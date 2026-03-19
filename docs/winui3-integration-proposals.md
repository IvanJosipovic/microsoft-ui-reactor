# Duct + WinUI3 Integration Proposals

> Analysis of how Duct's declarative reconciler could integrate more deeply with the WinUI3 native framework to unlock performance, correctness, and developer experience improvements.
>
> **Duct repo:** `C:\Users\andersonch\Code\patch`
> **WinUI3 repo:** `C:\Users\andersonch\Code\microsoft-ui-xaml`

---

## 1. Bypass DependencyProperty for Duct-Managed Properties (Tactical)

**Problem:** Every `SetElementTag()` call goes through WinUI's `FrameworkElement.Tag` DependencyProperty, which is a full COM round-trip through `CDependencyObject::SetValue` (`src/dxaml/xcp/core/core/elements/depends.cpp`). This is on the hot path — called on every interactive control during every reconciliation pass, even when the element hasn't changed.

**Proposal:** Introduce a lightweight attached storage mechanism that sidesteps the DP system entirely. Use `ConditionalWeakTable<UIElement, Element>` or a simple `Dictionary<nint, Element>` keyed on the control's native handle. This eliminates the COM marshaling overhead for Tag access during event dispatch.

**Impact:** The Tag property is read on every event handler invocation (button clicks, text changes, toggle switches) via `GetElementTag()` in `Reconciler.cs`. On a screen with 50 interactive controls, this could eliminate ~100 COM calls per reconciliation pass.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/depends.cpp` — SetValue hot path (line ~477)
- `src/dxaml/xcp/core/inc/CDependencyObject.h` — SetValueByKnownIndex overloads

**Duct files:**
- `Duct/Core/Reconciler.cs` — `SetElementTag()` / `GetElementTag()` helpers
- `Duct/Core/Reconciler.Update.cs` — SetElementTag called on every interactive control update

---

## 2. Native Layout Coalescing: Hook Into LayoutManager's Batch Queue (Tactical)

**Problem:** Duct's `DuctHost.RenderLoop()` reconciles the entire tree in one shot, which can trigger hundreds of `InvalidateMeasure()` / `InvalidateArrange()` calls as individual properties are patched. Each invalidation propagates up the ancestor chain via `PropagateOnMeasureDirtyPath()`.

**Proposal:** Bracket Duct's reconciliation pass with WinUI's layout suppression. Call `LayoutManager::EnterMeasure()` before patching and `ExitMeasure()` after, so all layout invalidations are batched into a single pass. Alternatively, expose a `BeginDeferUpdates()` / `EndDeferUpdates()` API on the WinUI side that suppresses layout until the batch completes.

**Impact:** A reconciliation that patches 200 properties currently triggers 200 individual invalidation propagations. Batching would reduce this to a single layout pass.

**WinUI3 files:**
- `src/dxaml/xcp/core/layout/LayoutManager.cpp` — Enter/ExitMeasure (line ~83-87 in header)
- `src/dxaml/xcp/core/core/elements/uielement.cpp` — `InvalidateMeasure()` (lines 3551-3586), `InvalidateArrange()` (lines 3611-3646)

**Duct files:**
- `Duct/Hosting/DuctHost.cs` — `RenderLoop()` / `Render()`
- `Duct/Core/Reconciler.Update.cs` — property patching triggers layout invalidation

---

## 3. Pool Interactive Controls by Resetting Event State (Tactical)

**Problem:** `ElementPool.cs` only pools 12 non-interactive control types (TextBlock, Grid, Border, etc.). Interactive controls like Button, TextBox, CheckBox, ToggleSwitch are created fresh on every mount and discarded on unmount — never reused.

**Proposal:** Extend pooling to interactive controls by adding a `ResetEventState()` step. Since Duct's Tag-based event pattern means handlers are generic (they read from Tag at invocation time), the actual event subscriptions don't need to change. The only work needed is: (1) clear the Tag, (2) reset visual state (IsPressed, IsChecked, etc.), (3) return to pool. This is safe because Duct never stores per-instance closures in event handlers.

**Impact:** In a virtualized list of 1000 buttons, scrolling currently creates and destroys Button instances. Pooling would amortize allocation to ~32 instances.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/Control.cpp` — Control state reset
- `src/controls/dev/Repeater/ViewManager.h` — WinUI's own element reuse patterns

**Duct files:**
- `Duct/Core/ElementPool.cs` — `PoolableTypes` set, `CleanElement()` method
- `Duct/Core/Reconciler.Mount.cs` — event handler wiring (generic Tag-based pattern)

---

## 4. Direct Composition Visuals for Layout-Only Elements (Wild)

**Problem:** Duct creates full WinUI FrameworkElement instances for layout-only elements like Border, StackPanel, and Grid. Each one carries the full UIElement allocation overhead: DComp render data (`PrimitiveCompositionPropertyData`), layout storage, automation peer infrastructure, managed peer linking, and property system participation.

**Proposal:** For elements that are purely structural (Border with just margin/padding, StackPanel with orientation/spacing), bypass UIElement creation entirely and create lightweight `Visual` objects directly via the Windows.UI.Composition API. These would participate in the DComp visual tree but skip the entire XAML framework overhead — no DependencyProperty storage, no layout manager participation, no event routing. Duct's reconciler would calculate layout positions itself (it already knows the constraints) and set `Visual.Offset` and `Visual.Size` directly.

**Impact:** Could reduce element creation cost by 10-50x for layout-only containers. A deeply nested component tree with 5 levels of VStack/HStack nesting would go from 5 FrameworkElement allocations to 5 lightweight Visual allocations.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/uielement.cpp` — UIElement construction overhead (lines 1-150)
- `src/dxaml/xcp/core/hw/hwwalk.cpp` — DComp visual creation
- `src/dxaml/xcp/core/hw/CompositorTreeHost.cpp` — Composition tree management

**Duct files:**
- `Duct/Core/Reconciler.Mount.cs` — MountStack, MountBorder, MountGrid create full FrameworkElements
- `Duct/Core/Element.cs` — StackElement, BorderElement, GridElement definitions

---

## 5. Rust Differ at the Composition Layer (Wild)

**Problem:** The Rust differ currently operates on serialized Element trees (ViewNode/ViewProp arrays) and produces patches that the C# reconciler applies to the WinUI control tree. This means: serialize → diff in Rust → deserialize patches → apply via COM interop → trigger layout → render. Three language boundaries per update.

**Proposal:** Move the Rust differ to operate directly on the DComp visual tree. The differ would read the current visual tree state from shared memory and emit DComp operations (visual inserts, property changes, offset updates) directly, bypassing the XAML layer entirely for layout-only subtrees. Interactive controls would still go through XAML, but pure layout subtrees could be updated in a single Rust→DComp pass.

**Impact:** Eliminates the C# ↔ Rust ↔ C# round-trip for structural updates. For a 500-node tree where 90% are layout-only, this could reduce update latency by 40-60%.

**WinUI3 files:**
- `src/dxaml/xcp/core/hw/CompositorTreeHost.cpp` — DComp tree access
- `src/dxaml/xcp/core/hw/hwcompnode.cpp` — Composition node management

**Duct files:**
- `Duct/Native/differ/src/diff.rs` — `diff_subtree()` algorithm
- `Duct/Native/differ/src/ffi.rs` — FFI boundary
- `Duct/Core/TreeSerializer.cs` — Serialization for Rust differ

---

## 6. Incremental Tree Serialization with Dirty Tracking (Medium)

**Problem:** `TreeSerializer.SerializeWithMapping()` does a full BFS traversal and serializes the entire Element tree on every reconciliation pass. For a 1000-node tree where only 3 nodes changed, this wastes ~99.7% of serialization work.

**Proposal:** Add dirty tracking to the Element tree. When `UseState` produces a new value, mark the owning component and its subtree as dirty. `TreeSerializer` would then only re-serialize dirty subtrees, reusing cached ViewNode/ViewProp arrays for clean subtrees. The Rust differ already handles subtree replacement via its `Replace` patch — it just needs the serializer to provide stable subtree references.

**Impact:** Reduces serialization cost from O(n) to O(changed) per render. For typical UI updates (user types in a text field, counter increments), this could be 100x faster serialization.

**Duct files:**
- `Duct/Core/TreeSerializer.cs` — `Serialize()` / `SerializeWithMapping()` BFS traversal
- `Duct/Core/RenderContext.cs` — `UseState` / state change triggers
- `Duct/Hosting/DuctHost.cs` — `RequestRender()` could carry dirty component info

---

## 7. Replace Remount-On-Update Controls with Incremental Patching (Tactical)

**Problem:** Several complex controls in `Reconciler.Update.cs` use a `RemountOnUpdate` pattern — they unmount and remount entirely instead of patching properties. This includes: `RadioButtonsElement`, `ComboBoxElement`, `SplitViewElement`, `TabViewElement`, `TreeViewElement`, `MenuBarElement`, `CommandBarElement`.

**Proposal:** Implement incremental update paths for these controls. The reason they remount is that their WinUI counterparts use `Items` collections that don't support efficient diffing. For ComboBox and RadioButtons, use `ChildReconciler` on their Items collection. For TabView, patch individual `TabViewItem` properties. For TreeView, use hierarchical reconciliation matching WinUI's `TreeViewNode` structure.

**Impact:** TabView with 10 tabs currently destroys and recreates all 10 TabViewItems when any tab label changes. Incremental patching would update only the changed label — a 10x improvement for tab-heavy UIs.

**WinUI3 files:**
- `src/controls/dev/TabView/TabView.h` — TabViewItem collection management
- `src/controls/dev/RadioButtons/RadioButtons.h` — Items collection
- `src/dxaml/xcp/core/core/elements/ItemsControl.cpp` — Items collection patterns

**Duct files:**
- `Duct/Core/Reconciler.Update.cs` — RemountOnUpdate controls
- `Duct/Core/ChildReconciler.cs` — keyed reconciliation (could be reused)

---

## 8. Unified Element Recycling with ItemsRepeater's ViewManager (Medium)

**Problem:** Duct has its own `ElementPool` and ItemsRepeater has its own `ViewManager` with separate recycling pools (PinnedPool, UniqueIdResetPool). These two systems don't know about each other. When Duct unmounts a virtualized item, it explicitly does NOT pool (comment in `DuctElementFactory.RecycleElementCore` explains why), losing the recycling opportunity.

**Proposal:** Integrate Duct's recycling with ItemsRepeater's ViewManager lifecycle. Register Duct's element pool as a custom recycling backend for ViewManager. When ItemsRepeater recycles an element, instead of clearing it to the factory, transition it to Duct's pool with element state preserved. When ItemsRepeater requests a new element, check Duct's pool first. This creates a single unified recycling pipeline.

**Impact:** Eliminates the "recycling gap" where ItemsRepeater recycles a control but Duct can't reuse it, forcing fresh allocation.

**WinUI3 files:**
- `src/controls/dev/Repeater/ViewManager.h` — `GetElement()` cascade, `ClearElement()` methods
- `src/controls/dev/Repeater/ViewManager.cpp` — Element lifecycle (lines 22-137)
- `src/controls/dev/Repeater/VirtualizationInfo.h` — Per-element state machine

**Duct files:**
- `Duct/Core/DuctElementFactory.cs` — `GetElementCore()` / `RecycleElementCore()`
- `Duct/Core/ElementPool.cs` — Current standalone pool

---

## 9. Fine-Grained Component Boundaries via WinUI's ContentPresenter (Medium)

**Problem:** Duct components are opaque to the Rust differ — they appear as "gap nodes" that require imperative C# reconciliation. This means the native diff path can't optimize across component boundaries, falling back to the slower C# path for every component in the tree.

**Proposal:** Map Duct components to WinUI `ContentPresenter` instances. ContentPresenter already manages content lifecycle and template instantiation natively. Each Duct component would own a ContentPresenter, and its rendered subtree would be the presenter's content. This gives WinUI native awareness of component boundaries — the presenter's content can be diffed independently, and WinUI's own content transition system provides free animation support.

**Impact:** Components would no longer be opaque to the differ. A tree with 20 components would go from 20 imperative reconciliation fallbacks to 20 independently diffable subtrees.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/ContentPresenter.cpp` — Content lifecycle (lines 76-145)
- `src/dxaml/xcp/core/core/elements/ContentControl.cpp` — Content hosting

**Duct files:**
- `Duct/Core/Reconciler.cs` — `ReconcileComponent()`, gap node handling
- `Duct/Core/TreeSerializer.cs` — Component serialization (treated as leaf/gap)

---

## 10. Frame-Budget-Aware Reconciliation (Tactical)

**Problem:** `DuctHost.RenderLoop()` reconciles the entire tree synchronously. If reconciliation takes longer than 16ms (one frame at 60fps), the UI stutters. There's no mechanism to yield mid-reconciliation and continue on the next frame.

**Proposal:** Implement time-sliced reconciliation inspired by React's Fiber architecture. Break reconciliation into units of work (one component = one unit). After each unit, check elapsed time. If approaching the frame deadline, yield to the dispatcher and resume on the next frame. Priority levels: user input > animations > data updates > off-screen content.

**Impact:** Prevents jank during large tree updates. A 2000-node tree update that takes 40ms would be split across 3 frames instead of causing a single 40ms stutter.

**WinUI3 files:**
- `src/dxaml/xcp/core/layout/LayoutManager.cpp` — MaxLayoutIterations=250, layout cycle management
- WinUI's own layout manager already has iteration limits; Duct could mirror this

**Duct files:**
- `Duct/Hosting/DuctHost.cs` — `RenderLoop()` / `Render()` — currently synchronous
- `Duct/Core/Reconciler.cs` — `ReconcileComponent()` — natural unit-of-work boundary

---

## 11. Native Property Diffing in Rust (Medium)

**Problem:** `TreeSerializer` serializes properties as `(dp_id, value_hash)` pairs, and the Rust differ compares hashes to detect changes. But the actual property application still happens in C# via per-control switch statements in `Reconciler.Update.cs`. The Rust differ knows *which* properties changed but can't *apply* them.

**Proposal:** Extend the Rust differ to emit typed property patches with actual values (not just hashes). For simple properties (strings, doubles, booleans, enums), the patch would carry the new value directly. A thin C interop layer would call WinUI's `SetValue` with the right `KnownPropertyIndex` and value, bypassing the C# switch dispatch entirely.

**Impact:** Eliminates the C# property dispatch overhead for simple properties. For a TextBlock content update, the path becomes: Rust diff → emit `UpdateProp(TextBlock, Content, "new text")` → native SetValue. No C# involved.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/depends.cpp` — `SetValueByKnownIndex()` for direct property access
- `src/dxaml/xcp/core/inc/KnownPropertyIndex.h` — Property index enum

**Duct files:**
- `Duct/Native/differ/src/types.rs` — DifferProp, DifferPatch definitions
- `Duct/Core/Reconciler.Update.cs` — Per-control property switch statements
- `Duct/Core/PropValueRegistry.cs` — Complex value storage

---

## 12. Leverage WinUI's Built-In Implicit Style Resolution for Theming (Tactical)

**Problem:** Duct elements specify visual properties (FontSize, Foreground, FontWeight) explicitly via modifiers. There's no participation in WinUI's implicit style system — a Duct TextBlock doesn't pick up the app's implicit TextBlock style. This means Duct apps can't inherit theme customizations.

**Proposal:** After mounting a control, allow WinUI's implicit style resolution to run before applying Duct's explicit properties. Duct properties would override implicit styles (specificity: explicit > implicit), but unset properties would inherit from the theme. This is already how WinUI works — the fix is to *not* reset properties that Duct hasn't explicitly set, rather than clearing everything in `CleanElement()`.

**Impact:** Duct apps would automatically respect system themes, accessibility settings (high contrast), and app-level style overrides without any Duct-side changes.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/Style.cpp` — Implicit style lookup, BasedOn chain (lines 28-100)
- `src/dxaml/xcp/core/core/elements/Control.cpp` — `OnApplyTemplate()` and style application

**Duct files:**
- `Duct/Core/ElementPool.cs` — `CleanElement()` resets all properties
- `Duct/Core/Reconciler.Mount.cs` — Property application during mount

---

## 13. Shared Memory Ring Buffer for Rust ↔ C# Communication (Wild)

**Problem:** Every Rust differ invocation involves marshaling flat arrays across the FFI boundary: `ViewNode[]`, `ViewProp[]` in, `ViewPatch[]` out. While the current zero-copy design (patches point into Rust heap) is efficient for reads, the serialization of the input tree still copies data.

**Proposal:** Use a shared memory ring buffer for bidirectional communication. C# writes serialized tree nodes directly into a memory-mapped region. Rust reads from the same region without copying. Patches are written to a separate output region. The ring buffer supports pipelining: C# can begin serializing the next frame while Rust is still diffing the current one.

**Impact:** Eliminates all FFI marshaling overhead. For a 1000-node tree, this removes ~40KB of array copying per reconciliation pass. The pipelining benefit is larger — it overlaps serialization and diffing.

**Duct files:**
- `Duct/Native/differ/src/ffi.rs` — Current FFI boundary
- `Duct/Core/ViewDiffer.cs` — P/Invoke wrappers, pointer management
- `Duct/Core/TreeSerializer.cs` — Could write directly to shared memory

---

## 14. Duct as WinUI's Official Declarative Layer (Wild)

**Problem:** WinUI's declarative story is XAML + data binding + MVVM. This requires: .xaml files, code-behind, INotifyPropertyChanged boilerplate, DataTemplate definitions, converter classes, and style resources. The cognitive overhead is enormous compared to Duct's `Text("hello").Bold()`.

**Proposal:** Ship Duct as a first-party WinUI package (`Microsoft.UI.Xaml.Declarative`). This would involve:
1. Adding Duct-aware APIs to WinUI controls (e.g., `IReconcilable` interface for incremental updates)
2. Exposing internal WinUI APIs to Duct (layout suppression, direct property access, composition visuals)
3. Making Duct's Element types part of the WinUI SDK
4. Providing migration tooling (XAML → Duct converter)

**Impact:** Every WinUI developer gets a modern, React-like development experience. Eliminates the XAML/MVVM learning curve. Microsoft ships a competitive declarative UI framework alongside SwiftUI and Jetpack Compose.

**WinUI3 files:**
- `src/controls/dev/` — Every control would get an `IReconcilable` implementation
- `src/dxaml/xcp/core/layout/LayoutManager.cpp` — Would expose batch APIs
- `src/dxaml/xcp/core/core/elements/depends.cpp` — Would expose fast property paths

---

## 15. Children.Move() Instead of Remove+Insert (Tactical)

**Problem:** `ChildReconciler.cs` reorders children by removing and re-inserting them. Each `Panel.Children.Remove()` and `Panel.Children.Insert()` triggers visual tree updates, DComp visual reparenting, and layout invalidation independently.

**Proposal:** Use `UIElementCollection.Move()` (or the equivalent native `MoveInternal()`) which reorders the child in-place without remove+insert overhead. This preserves the visual's composition state and avoids the double layout invalidation. If the public API doesn't expose Move, add it to WinUI.

**Impact:** A list reorder of 10 items currently does 10 removes + 10 inserts = 20 visual tree operations. With Move, it's 10 operations with no reparenting overhead.

**WinUI3 files:**
- `src/dxaml/xcp/core/inc/panel.h` — Children collection management
- `src/dxaml/xcp/core/core/elements/panel.cpp` — SetValue for children transitions

**Duct files:**
- `Duct/Core/ChildReconciler.cs` — `ReconcileKeyed()` uses Remove+Insert for moves
- `Duct/Core/ChildCollection.cs` — Abstraction over Panel.Children

---

## 16. Pre-Warm Element Pools During Idle Time (Tactical)

**Problem:** Element pools start empty. The first render of a complex component creates all controls from scratch — the "cold start" penalty. Subsequent renders benefit from pooling, but the initial render is the most performance-critical (it's what the user sees first).

**Proposal:** During `DispatcherQueue` idle time, pre-allocate common control types into the pool. Use heuristics from the component tree structure: if the root component contains a `LazyVStack<Item>` with a view builder that produces `HStack(Image, VStack(Text, Text))`, pre-warm the pool with 32 StackPanels, 32 Images, and 64 TextBlocks before the first scroll event.

**Impact:** Eliminates cold-start allocation stutter for the first screenful of virtualized items.

**Duct files:**
- `Duct/Core/ElementPool.cs` — Pool management, max 32 per type
- `Duct/Hosting/DuctHost.cs` — Could schedule pre-warming on idle
- `Duct/Core/DuctElementFactory.cs` — Could analyze view builder output types

---

## 17. Expose WinUI's Composition Animations for Duct Transitions (Medium)

**Problem:** Duct has no transition/animation system. When elements are inserted, removed, or reordered, changes are instant. WinUI has a full composition animation system (implicit animations, connected animations, layout transitions) that Duct can't access because it bypasses the template/style system.

**Proposal:** Add a `.Transition()` modifier to Duct elements that maps to WinUI's `UIElement.TransitionCollection`. For layout changes, use `RepositionThemeTransition`. For inserts/removes, use `AddDeleteThemeTransition`. For connected animations, expose `ConnectedAnimationService` via a `UseConnectedAnimation()` hook. The reconciler would set these during Mount and they'd animate automatically.

**Impact:** Duct apps get polished, native-feeling animations with zero custom code. List reorders would animate smoothly instead of snapping.

**WinUI3 files:**
- `src/dxaml/xcp/core/core/elements/panel.cpp` — Panel_ChildrenTransitions property
- `src/dxaml/xcp/core/hw/` — Composition animation infrastructure

**Duct files:**
- `Duct/Elements/ElementExtensions.cs` — Would add `.Transition()` modifier
- `Duct/Core/Element.cs` — ElementModifiers would gain transition fields
- `Duct/Core/Reconciler.Mount.cs` — Would apply TransitionCollection during mount

---

## 18. Parallel Subtree Reconciliation (Wild)

**Problem:** Reconciliation is single-threaded. For a tree with independent subtrees (e.g., a split-pane view with left navigation and right content), both sides are reconciled sequentially even though they have no data dependencies.

**Proposal:** Identify independent subtrees (components with no shared state) and reconcile them in parallel on separate threads. Each thread produces a list of patches. Patches are applied on the UI thread in a single batch. The Rust differ already supports this — `DiffContext` is per-thread, and patches are returned as arrays that can be concatenated.

**Impact:** A complex app with 4 independent panels could reconcile 4x faster on multi-core machines. The constraint is that WinUI controls can only be touched on the UI thread, so only the diff phase parallelizes — patch application remains single-threaded.

**Duct files:**
- `Duct/Core/Reconciler.cs` — `ReconcileComponent()` as parallelization boundary
- `Duct/Native/differ/src/arena.rs` — `DiffContext` is already per-instance (not global)
- `Duct/Hosting/DuctHost.cs` — Would coordinate parallel diff + sequential apply

---

## Summary Table

| # | Proposal | Ambition | Effort | Impact |
|---|----------|----------|--------|--------|
| 1 | Bypass DependencyProperty for Tag | Tactical | S | Medium |
| 2 | Layout coalescing via LayoutManager | Tactical | M | High |
| 3 | Pool interactive controls | Tactical | S | Medium |
| 4 | Direct Composition visuals for layout | Wild | XL | Very High |
| 5 | Rust differ at Composition layer | Wild | XL | Very High |
| 6 | Incremental tree serialization | Medium | L | High |
| 7 | Replace remount-on-update controls | Tactical | M | Medium |
| 8 | Unified recycling with ViewManager | Medium | L | Medium |
| 9 | Component → ContentPresenter mapping | Medium | L | High |
| 10 | Frame-budget-aware reconciliation | Tactical | M | High |
| 11 | Native property diffing in Rust | Medium | L | Medium |
| 12 | Implicit style participation | Tactical | S | Medium |
| 13 | Shared memory ring buffer | Wild | XL | Medium |
| 14 | Duct as WinUI's declarative layer | Wild | XXL | Transformative |
| 15 | Children.Move() for reorders | Tactical | S | Medium |
| 16 | Pre-warm element pools on idle | Tactical | S | Low-Medium |
| 17 | Composition animations for transitions | Medium | M | High |
| 18 | Parallel subtree reconciliation | Wild | XL | High |

# Duct Framework — Critical Review

A skeptic's component-by-component analysis of Duct against React, SwiftUI, and
Jetpack Compose. This document catalogs architectural weaknesses, API design
problems, missing capabilities, and places where Duct is a hack on top of WinUI
rather than a principled declarative UI framework.

---

## Executive Summary

Duct is an ambitious attempt to bring React-style declarative UI to WinUI 3.
It has impressive breadth of control coverage (94% of WinUI controls wrapped),
a now-solid component model foundation, and a faithful hooks system. The two
largest gaps from the previous review — navigation and commanding — have both
shipped as comprehensive systems. But significant problems remain, and some of
the new features have implementation concerns that temper the enthusiasm:

1. **The component model is now a real framework foundation** — context system,
   memoization, generic hook state, persisted state, and post-render effect
   cleanup have all landed, closing the most fundamental gaps
2. **Navigation has shipped and is architecturally strong** — type-safe routes,
   developer-owned back stack, composition-layer transitions, lifecycle guards,
   LRU caching, serialization, and deep linking. The design is competitive with
   Compose Navigation 3 and ahead of SwiftUI's type-erased NavigationPath. But
   connected transitions are a stub, E2E tests are unexecuted, and there's no
   adaptive multi-pane layout story
3. **Commanding is a genuine differentiator** — define-once commands with
   metadata bundling, 16 standard commands, async lifecycle, focus-scoped
   keyboard accelerators, and ICommand interop. No competing framework (React,
   SwiftUI, Compose) provides this out of the box. But accelerators rebuild on
   every render, standard command labels aren't localized, and there's no command
   routing to the focused view
4. **The DSL is constrained by C# language limitations** — verbose, repetitive,
   and leaky compared to JSX, SwiftUI's result builders, or Compose's Kotlin DSL
5. **Theming works for WinUI's built-in tokens but has real gaps** — custom
   branded colors still require workarounds, and the implementation has perf cost
6. **Accessibility has improved significantly but still has structural limits** —
   16 properties exposed with real UIA tests, but hooks, diagnostics, and custom
   automation peers remain unbuilt
7. **Animation has broadened but remains limited in scope** — layout animations,
   springs, and connected animations now exist, but still no value-driven
   animation, no keyframe DSL, and implicit transitions are capped at 5 properties
8. **The `.Set()` escape hatch is still load-bearing** — navigation and commanding
   reduce its surface area, but the majority of input handling, gestures, advanced
   styling, and composition-layer access still require it

**Verdict:** Duct has crossed a second important threshold. The first was the
component model foundation (context, memoization, hooks). The second is the
application architecture layer: navigation and commanding are the features that
separate "a component library" from "a framework you can build a real app with."
Both are now shipped, tested, and demonstrated in sample apps. Navigation in
particular is architecturally competitive with the industry's latest thinking —
Compose Navigation 3 (stable Nov 2025) made the same philosophical choices
(developer-owned typed stack, declarative destination mapping) that Duct
independently arrived at. Commanding fills a gap that literally no competitor
has filled — every serious app builds a custom command registry, and Duct
provides one as a first-class feature.

The distance from "impressive demo" to "ships to real users" has narrowed
substantially. The critical path now runs through the mid-tier features:
theming needs to be more than 3 properties, accessibility needs the semantic
and imperative layers, animation needs general-purpose value-driven support.
The `.Set()` escape hatch carries less weight than before — navigation and
commanding reclaimed meaningful surface area — but it's still the answer for
too many scenarios. The framework is no longer blocked on any single P0 gap,
but the sum of its P1/P2 gaps still adds up to a significant distance from
production-readiness for apps that need polish.

---

## Table of Contents

1. [The DSL: C# as a UI Language](#1-the-dsl-c-as-a-ui-language)
2. [Component Model](#2-component-model)
3. [State Management](#3-state-management)
4. [The Reconciler](#4-the-reconciler)
5. [Layout System](#5-layout-system)
6. [Styling and Theming](#6-styling-and-theming)
7. [Navigation](#7-navigation)
8. [Commanding](#8-commanding)
9. [Lists and Collections](#9-lists-and-collections)
10. [Animation](#10-animation)
11. [Accessibility](#11-accessibility)
12. [Input Handling and Events](#12-input-handling-and-events)
13. [Developer Experience](#13-developer-experience)
14. [The .Set() Problem](#14-the-set-problem)
15. [Component-by-Component Scorecard](#15-component-by-component-scorecard)
16. [Conclusion](#16-conclusion)

---

## 1. The DSL: C# as a UI Language

### The fundamental problem

React has JSX. SwiftUI has result builders. Compose has Kotlin's trailing lambdas
and compiler plugin. Duct has... C# method calls with `params` arrays.

This matters enormously. The quality of the UI DSL determines whether writing UI
code feels natural or feels like fighting the language.

### What Duct looks like

```csharp
VStack(
    Text("Hello").Bold().FontSize(24),
    Button("Click me", () => setCount(count + 1))
        .Background("#0078D4")
        .Padding(12, 8),
    count > 5 ? Text("Wow!") : null
)
```

### What the competition looks like

**React (JSX):**
```jsx
<VStack>
  <Text bold fontSize={24}>Hello</Text>
  <Button onClick={() => setCount(c => c + 1)}
          style={{ background: '#0078D4', padding: '12px 8px' }}>
    Click me
  </Button>
  {count > 5 && <Text>Wow!</Text>}
</VStack>
```

**SwiftUI:**
```swift
VStack {
    Text("Hello").bold().font(.title)
    Button("Click me") { count += 1 }
        .background(.blue)
        .padding(.horizontal, 12)
    if count > 5 { Text("Wow!") }
}
```

**Compose:**
```kotlin
Column {
    Text("Hello", fontWeight = FontWeight.Bold, fontSize = 24.sp)
    Button(onClick = { count++ }) {
        Text("Click me")
    }
    if (count > 5) { Text("Wow!") }
}
```

### Specific DSL weaknesses

**1. No block syntax for children.** SwiftUI and Compose use trailing closures
/ lambdas that visually separate children from the container. In Duct, children
are positional arguments mixed into the constructor call. For deeply nested UIs,
this becomes bracket-counting hell:

```csharp
VStack(
    HStack(
        VStack(
            Text("Label"),
            Text("Value")
        ).Width(200),
        VStack(
            Text("Label 2"),
            Text("Value 2")
        ).Width(200)
    ),
    HStack(
        Button("OK", onOk),
        Button("Cancel", onCancel)
    )
)  // Which closing paren belongs to which?
```

**2. Modifier chains allocate on every render.** Every `.Margin(10)` call does
`el with { Modifiers = ... }` creating a new record copy. A chain of 5 modifiers
creates 5 intermediate records. SwiftUI avoids this via opaque return types and
view builder inlining. Compose avoids it via `Modifier` composition that chains
without copying. Duct's approach generates measurable GC pressure in hot render
paths — every element with modifiers allocates multiple short-lived objects per
render cycle.

**3. The `with` expression problem.** Duct's element records use C# records
with `init` properties. The fluent modifier pattern uses either extension methods
that create `ElementModifiers` records and merge them, or `with` expressions on
the concrete record. This means *every modifier call allocates a new
ElementModifiers record*, even for a single property change:

```csharp
// This creates a new ElementModifiers for just one property,
// then merges it (another allocation) with the existing modifiers
public static T Margin<T>(this T el, double uniform) where T : Element =>
    Modify(el, new ElementModifiers { Margin = new Thickness(uniform) });
```

**4. String-typed APIs where enums or types should be used.** Grid columns are
`string[]`: `Grid(["*", "Auto", "200"], ...)`. There's no compile-time
validation that `"*"` or `"Auto"` are valid values. SwiftUI uses
`GridItem(.flexible())`, `GridItem(.fixed(200))` — type-safe and discoverable.
Compose uses `GridCells.Adaptive(minSize)`. Duct borrowed CSS grid syntax but
lost type safety in the process.

**5. No children as content blocks.** In React, JSX children are a natural part
of the markup. In SwiftUI, `@ViewBuilder` closures provide block syntax. In
Compose, `content: @Composable () -> Unit` trailing lambdas do the same. Duct
forces all children into `params Element?[]` constructor arguments — there's no
visual distinction between container properties and container children.

**6. Implicit string conversion is a code smell, not a feature.** `Element` has
`public static implicit operator Element(string text) => new TextElement(text)`.
This means `VStack("Hello", "World")` works. But it also means any string
accidentally passed where an Element is expected silently becomes text. This is
the kind of convenience that causes bugs in large codebases.

**7. Null-based conditional rendering is fragile.** Duct uses nullable elements
for conditional rendering: `condition ? Text("yes") : null`. This works but
requires `FilterChildren` to strip nulls on every render. SwiftUI uses `if`
directly in the view builder. Compose uses `if` naturally. React uses
`{condition && <Component/>}`. Duct's approach works but is less readable and
requires runtime filtering.

---

## 2. Component Model

### The component model story: from gaps to genuine framework foundation

The previous version of this review scored Component Model at C and Global State
at F — "no Context equivalent," "no memoization," "hooks box value types." A
significant component model diff has landed that addresses these systematically:
context system, default-on memoization, generic hook state, persisted state, and
post-render effect cleanup. The honest assessment now: **the component model
foundation is solid and competitive with the established frameworks on core
mechanics, but some secondary gaps remain.**

### What Duct has now

- **Class components** extending `Component` with `Render()` method
- **Props via generics**: `Component<TProps>` for typed props
- **Function components**: `Func(ctx => { ... })` inline lambdas
- **Memo function components**: `Memo(ctx => { ... }, deps)` with dependency tracking
- **Error boundaries**: `ErrorBoundary(child, fallback)`
- **DuctContext\<T\>** — tree-scoped ambient state (React Context equivalent)
- **Default-on memoization** — `ShouldUpdate()` on class components, dependency
  tracking on Memo elements
- **Generic hook state** — `ValueHookState<T>` eliminates boxing for value types
- **Persisted state** — `UsePersisted<T>(key, initial)` survives unmount/remount
- **Post-render effect cleanup** — cleanup runs in `FlushEffects`, not during render
- **No slots/named children** — no mechanism for multi-slot composition

### What shipped: Context system (DuctContext\<T\>)

```csharp
// Define: static, typed, named, with default
public static readonly DuctContext<ThemeConfig> ThemeContext =
    new(new ThemeConfig("light"));

// Provide: modifier on any element, scoped to subtree
VStack(children).Provide(ThemeContext, darkTheme)

// Consume: hook in any descendant component
var theme = UseContext(ThemeContext);
```

The design follows React's mental model with SwiftUI's fluent syntax. Contexts
are tree-scoped: inner providers shadow outer ones. The `ContextScope` class
maintains a stack during reconciler traversal — pushed on entering an element
with `ContextValues`, popped on leaving. `UseContext<T>` reads the nearest
ancestor's value by walking the stack backward.

The reconciler integrates context into the memo check: `HasConsumedContextChanged`
compares each `ContextHookState.LastValue` against the current scope value. A
component that consumes `ThemeContext` re-renders when the theme changes, even
if its props haven't changed. A component that doesn't consume any context skips
the check entirely — zero cost for the common case.

**LocaleProvider now uses DuctContext.** The localization system has been migrated
from the hand-rolled `LocaleContext.Current` thread-static hack to a proper
`DuctContext<IntlAccessor?>`. `UseIntl()` is now `UseContext(IntlContexts.Locale)`
under the hood. The legacy `LocaleContext.Current` is maintained for backward
compatibility but marked internal. This validates the context system with a
real use case — the framework's own localization depends on it.

### What shipped: Default-on memoization

```csharp
// Propless component: ShouldUpdate() defaults to false
// → only re-renders from own state changes or context changes
public class StatusBar : Component { ... }

// Props component: ShouldUpdate(old, new) defaults to Equals(old, new)
// → record props get structural comparison for free
public record DashboardProps(string Title, int Count);
public class Dashboard : Component<DashboardProps> { ... }

// Function component with memo: explicit dependency array
Memo(ctx => { ... }, count, theme)
```

Class components have `ShouldUpdate()` which defaults to `false` for propless
components (never re-render from parent) and `!Equals(oldProps, newProps)` for
`Component<TProps>` (re-render when props change structurally). This leverages
C# records — a `record` props type gets value equality for free, so the common
case requires zero developer effort. The reconciler also checks context changes
via `HasConsumedContextChanged`, so components re-render when consumed contexts
change regardless of the `ShouldUpdate` result.

The `ShouldUpdateWithProps` method in the reconciler uses reflection to call the
typed `ShouldUpdate(TProps?, TProps?)` through the untyped `Component` reference.
This is a one-time cost per component type (reflection calls are not cached), but
it runs on every parent re-render for every child component. For deep trees with
many components, this reflection overhead could add up.

### What shipped: Generic hook state (no boxing)

```csharp
// Before: HookState.Value was object — boxing for every int, bool, double
private class HookState { public object Value = default!; }

// After: ValueHookState<T> — no boxing, no allocation for value types
private class ValueHookState<T> : HookState { public T Value; }
```

`UseState<int>`, `UseReducer<bool>`, `UseMemo<double>` — none of these box
anymore. The generic `ValueHookState<T>` stores the value directly. The equality
check uses `EqualityComparer<T>.Default` instead of `object.Equals`, avoiding
both boxing for comparison and the allocation of intermediate `object` references.

This also means hook type mismatches (calling `UseState<int>` on one render and
`UseState<string>` on the next) produce clearer error messages — the exception
says exactly which generic types conflicted.

### What shipped: Persisted state

```csharp
var (scrollPos, setScrollPos) = UsePersisted("inbox-scroll", 0.0);
```

`UsePersisted<T>(key, initialValue)` works like `UseState<T>` but survives
unmount/remount. Values are stored in `PersistedStateCache` — a static
`Dictionary<string, object?>` — and saved to cache in `RunCleanups()` (on
unmount). On next mount, the cached value is used instead of `initialValue`.

### What shipped: Post-render effect cleanup

Previously, effect cleanup ran synchronously during `UseEffect()` — inside the
render phase. Now, cleanup is deferred: `UseEffect` sets `PendingCleanup` (the
old cleanup function) and `FlushEffects()` runs all pending cleanups *before*
running new effects, in a two-phase approach:

```
Phase 1: Run all PendingCleanup actions (from previous render)
Phase 2: Run all new pending effects
```

This matches React's behavior: cleanup from the previous render runs after the
new render completes, not during it. Expensive cleanup (network cancellation,
timer disposal) no longer blocks the render.

### What's actually good about this (credit where due)

The context system is well-designed. The `DuctContext<T>` type is simple — a
static field with a default value and a debug name (via `[CallerMemberName]`).
The provide mechanism uses the existing element `with` pattern via
`ContextExtensions.Provide()`, so it composes naturally with other modifiers.
The `ContextScope` stack is lightweight — a `List<(DuctContextBase, object?)>`
with version tracking. The consumer hook follows the existing hook pattern
exactly. This is the right API shape — it mirrors React Context closely enough
that the mental model transfers, while using Duct's fluent modifier convention
for providing.

The memoization default is the right call. Making propless components skip
re-renders by default (unless self-triggered or context-changed) is aggressive
but correct — it matches Compose's behavior where stable parameters cause
automatic skipping. Record-based props getting structural equality for free via
`Equals` is a clever use of C# language features that React and Compose can't
match (they need explicit `React.memo()` comparators or `@Stable` annotations).

The test coverage is thorough: 24 context unit tests, 12 context self-host tests,
19 memoization tests, 18 hook refactor tests, 17 persisted state tests, 15
integration tests exercising realistic multi-hook component patterns. The self-
host test pattern (manually driving BeginRender → Render → FlushEffects through
a ContextScope without WinUI controls) is a good strategy for testing framework
internals without platform dependencies.

### What's still concerning (skeptic's view)

**1. Context values are boxed in the scope stack.** `ContextScope` stores
`List<(DuctContextBase, object?)>` — the value is `object?`. A
`DuctContext<int>` provided with value `42` boxes that int. Every `UseContext<T>`
read casts back from `object?`. The hook state itself is now unboxed
(`ValueHookState<T>`), but the context delivery mechanism reintroduces boxing
for value types. For `DuctContext<string>` or `DuctContext<ThemeConfig>` (the
common cases), this doesn't matter. But the inconsistency is notable — hooks
went to the effort of eliminating boxing while the context system didn't.

**2. ShouldUpdateWithProps uses reflection on every memo check.** The reconciler
calls `ShouldUpdateWithProps` which does `compType.GetMethod("ShouldUpdate", ...)`
via reflection to find the typed `ShouldUpdate(TProps?, TProps?)` method. This
runs every time a parent re-renders and the child is a `Component<TProps>`. There's
no caching of the `MethodInfo` — each check does a fresh reflection lookup. For
a tree with 50 components and a root state change, that's 50 reflection calls per
render cycle. React's `React.memo()` stores the comparator as a direct function
reference. Compose's stability check is compile-time. Duct's is a runtime
reflection walk.

**3. PersistedStateCache is a static Dictionary with no eviction.** The cache
grows unboundedly — every `UsePersisted` key stays in memory for the process
lifetime. There's no LRU eviction, no size limit, no TTL. For a long-running
desktop app where users navigate through many views, the cache accumulates stale
state from components that will never remount. SwiftUI's `@SceneStorage`
serializes to disk with OS-managed lifecycle. Compose's `rememberSaveable` ties
to the `SaveableStateRegistry` which is scoped to the composition. Duct's cache
is a global singleton that only clears on process exit.

**4. PersistedStateCache keys are stringly-typed with no collision protection.**
`UsePersisted("scroll-pos", 0.0)` uses a bare string key. Two unrelated
components that happen to use the same key silently share state — a bug with no
diagnostic. The cache stores `object?`, so a type mismatch (one component persists
`int`, another reads `string` with the same key) produces a runtime
`InvalidCastException` on the next mount. There's no namespacing, no type
validation, no collision warning.

**5. No Suspense or lazy loading.** Still missing. React's `Suspense` +
`React.lazy()` for code-split components has no equivalent. SwiftUI and Compose
also lack this, so Duct is not uniquely deficient — but React has had it since
2018.

**6. Component props are still untyped at the element level.** `ComponentElement`
stores props as `object?`. Props are set via `IPropsReceiver.SetProps(object props)`
with a cast. The generic `Component<T, TProps>(props)` factory hides this, but the
underlying infrastructure is type-erased. A wrong props type produces a runtime
`InvalidCastException`, not a compile error.

**7. No slots/named children convention.** The design spec mentions documenting
a convention for named children using Element-typed props, with `Lazy<Element>`
as future work. This hasn't materialized. Multi-slot composition (header + body +
footer) requires ad-hoc props types. SwiftUI has `@ViewBuilder` parameters for
multiple content slots. Compose has multiple `@Composable` lambda parameters.
Duct has no convention, pattern, or framework support.

**8. FuncElement identity is still problematic.** `Func(ctx => ...)` creates a
`FuncElement` with a lambda. The lambda is recreated every render (it captures
outer state), so the function reference changes every time. `ShallowEquals` still
returns false for FuncElements, forcing an update. The new `Memo(ctx => ..., deps)`
provides a workaround — but `Func` remains the first thing developers reach for,
and it can't memo-skip.

**9. Context change detection compares boxed values with Equals.** The
`HasConsumedContextChanged` method compares `Equals(currentValue, ctxHook.LastValue)`
where both values are `object?`. For reference types that don't override `Equals`,
this is reference equality — meaning a new `ThemeConfig` record with the same
values still triggers re-renders unless the record type properly implements
`Equals`. C# records do implement value equality by default, so this works for
the common case. But a class-based context value type silently breaks the memo
optimization — every provide creates a new object, which is always "different"
by reference, defeating the point of memoization.

### Revised component model verdict

**Previously: Component Model C, Global State F, Local State B+.**
**Now: Component Model B+, Global State B+, Local State A-.**

The improvement is substantial and addresses the most critical gaps
identified in the previous review. The context system provides a real answer
to "how do I share state across the tree" — theming, auth, feature flags, and
localization all have a clean mechanism now. Default-on memoization means
components skip unnecessary re-renders without developer opt-in. Generic hook
state eliminates boxing. Post-render effect cleanup matches React's behavior.
Persisted state handles the unmount/remount scenario.

The grade is B+/B+ rather than A because of implementation concerns: reflection
for ShouldUpdate dispatch, boxing in the context scope stack, unbounded persisted
state cache, string-keyed persistence with no collision protection. These are
solvable problems — caching MethodInfo, using a generic scope entry, adding LRU
eviction — but they're real costs in the current implementation. The competition
doesn't have these particular issues: React's memo comparator is a direct
function reference, Compose's stability is compile-time, SwiftUI's environment
is type-safe throughout.

Local State moves to A- because the boxing is gone, effect cleanup timing is
correct, and persisted state exists. The remaining gap is the `object[]`
dependency array for `UseEffect`/`UseMemo` — dependencies are still boxed into
`object[]` and compared with `Equals`, which is the same issue React has (but
React has ESLint rules to catch common mistakes, and Duct has no tooling).

---

## 3. State Management

### What Duct has

- `UseState<T>` — React's useState equivalent, **now generic (no boxing)**
- `UseReducer<T>` — functional updater variant
- `UseReducer<TState, TAction>` — Redux-style reducer
- `UseEffect` — side effects with dependency tracking, **cleanup now post-render**
- `UseMemo<T>` — memoized computation
- `UseCallback` — stable callback reference
- `UseRef<T>` — mutable reference
- `UseObservable<T>` — INotifyPropertyChanged bridge
- `UseObservableTree<T>` — deep INotifyPropertyChanged bridge (recursive)
- `UseObservableProperty<T>` — single-property INotifyPropertyChanged bridge
- `UseCollection<T>` — ObservableCollection bridge
- `UseContext<T>` — **NEW** — reads nearest ancestor's context value
- `UsePersisted<T>` — **NEW** — state that survives unmount/remount
- `UseWindowSize` / `UseBreakpoint` — responsive hooks

### What's improved

**1. Hook state is now generic — no boxing.** Previously `HookState.Value` was
`object`, boxing every int/bool/double. Now `ValueHookState<T>` stores the value
directly. `EqualityComparer<T>.Default` handles comparison without boxing. This
is a clean fix for a real performance problem.

**2. Effect cleanup timing is now correct.** Previously, cleanup ran synchronously
during `UseEffect()` — inside the render phase. Now, `UseEffect` stores the old
cleanup as `PendingCleanup`, and `FlushEffects()` runs all pending cleanups in
Phase 1 before running new effects in Phase 2. This matches React's behavior:
cleanup from the previous render runs after the new render completes, not during
it. The fix is clean and the two-phase approach in FlushEffects is easy to reason
about.

**3. State persistence exists.** `UsePersisted<T>(key, initialValue)` stores
values in a static `PersistedStateCache` that survives unmount/remount. Values
are saved to cache during `RunCleanups()` (on unmount) and restored on next
mount. This closes the gap where navigating away and back lost all state.

**4. Global state exists via DuctContext.** Covered in detail in Section 2. The
`UseContext<T>` hook reads tree-scoped ambient state provided by any ancestor.
The old "no global state at all" critique is resolved.

### Remaining critiques

**1. Dependency comparison still uses `object.Equals` on `object[]`.** `UseEffect`,
`UseMemo`, and other dependency-tracking hooks compare deps with
`Equals(prev[i], next[i])` where deps are `params object[]`. This means:
- Value types are boxed into the `object[]` dependency array (allocation)
- Reference types use reference equality by default unless they override Equals
- Collections as dependencies compare by reference, not by content
- No warning or guidance about what makes a good dependency

The hook state itself is now unboxed, but the dependency arrays still box. React
has the same issue (shallow comparison) but has ESLint rules that catch common
mistakes. Duct has no tooling support.

**2. UseCallback is just UseMemo returning the same Action.** The implementation
is literally:

```csharp
public Action UseCallback(Action callback, params object[] dependencies)
{
    return UseMemo(() => callback, dependencies);
}
```

This doesn't actually stabilize the callback reference in the way React's
useCallback does. The `() => callback` lambda captures `callback`, so if
`callback` is a new closure each render (which it always is), the memoized
value is the closure captured at first render — meaning it has stale captures
unless the deps change. This is correct behavior but the documentation doesn't
explain this nuance.

**3. No batching control or transition API.** React 18 has `startTransition`
for marking non-urgent updates. Duct batches via DispatcherQueue, which is
all-or-nothing. There's no way to say "this state update is low priority" or
"this update should not show a loading state."

**4. PersistedStateCache limitations.** Covered in Section 2 — unbounded growth,
string keys with no collision protection, no disk persistence. For a desktop
framework where window/session restoration is expected, in-memory-only persistence
is a partial answer. SwiftUI's `@SceneStorage` and Compose's `rememberSaveable`
both tie into the platform's state restoration lifecycle. Duct's cache is process-
scoped only.

---

## 4. The Reconciler

### Architecture

The reconciler follows React's model: diff old and new element trees, apply
minimal patches. It's split across `Reconciler.cs` (orchestration + component
lifecycle + memo checks + context scope), `Reconciler.Mount.cs` (~40 mount
handlers), `Reconciler.Update.cs` (~30 update handlers), and `ChildReconciler.cs`
(keyed/unkeyed lists).

The reconciler now owns the `ContextScope` and drives context push/pop during
tree traversal — elements with `ContextValues` push on mount/update entry and
pop on exit. Components receive the scope via `BeginRender(requestRerender,
contextScope)` so their `UseContext` hooks read from the correct scope position.
The component update path includes a memo check that combines props comparison
(`ShouldUpdate`) with context change detection (`HasConsumedContextChanged`).

### Critiques

**1. Massive switch/dispatch for every element type.** The Mount and Update
methods are giant type-based dispatches. Adding a new element type requires
modifying both `Mount()` and `Update()` — a violation of the open/closed
principle. The `RegisterType<>` extensibility API exists but the built-in types
don't use it; they're hardcoded switch arms. This means the reconciler is a
monolithic class with ~70+ individual handler methods across its partial files.

React's reconciler is element-type-agnostic — it calls `createElement` and the
component decides how to render. SwiftUI's diffing is automatic via the View
protocol. Compose's compiler plugin handles recomposition transparently. Duct's
reconciler is essentially a hand-maintained mapping table from element types to
WinUI control operations.

**2. The Tag-based event dispatch is a fragile workaround.** WinUI controls
have a `Tag` property (type `object`) intended for user data. Duct repurposes it
to store the current element, so event handlers can read fresh state:

```csharp
// At mount time: wire event handler once
button.Click += (sender, _) => {
    var el = GetElementTag<ButtonElement>(sender);
    el?.OnClick?.Invoke();
};

// At update time: update the tag to point to the new element
SetElementTag(button, newElement);
```

This is clever but fragile:
- If anything else sets `Tag` (e.g., WinUI internals, user code via `.Set()`),
  event handlers silently break with no error
- It prevents users from using `Tag` for their own purposes
- It couples event dispatch to a WinUI implementation detail that could change
- There's a race condition: if an event fires between `Tag` being cleared and
  updated during reconciliation, the handler gets `null`

**3. Element Pool only handles non-interactive controls.** The element pool
recycles unmounted controls: TextBlock, StackPanel, Grid, Border, ScrollViewer,
Canvas, Image. But interactive controls (Button, TextBox, etc.) are NOT pooled
because "resetting their event state safely is more complex." In a real app,
interactive controls are the majority of the UI. This means the pool optimizes
the cheap case (layout containers) while leaving the expensive case (controls
with event subscriptions and visual state) unoptimized.

**4. ShallowEquals is conservative to the point of being useless.** The
`Element.ShallowEquals` optimization returns `false` for any element that has
Setters, event handlers, or is of an unknown type. Since virtually every
interactive element has at least one event handler, ShallowEquals will return
false for all of them, meaning the optimization only helps for static text and
images. The method explicitly says "Conservative: returns false for unknown
element types" — which means custom element types never benefit.

**5. Every component adds a hidden Border to the visual tree.** The reconciler
wraps every `Component` and `FuncElement` in a WinUI `Border` as an identity
anchor:

```csharp
var wrapper = new Border { Child = childControl };
_componentNodes[wrapper] = new ComponentNode { ... };
```

This means the WinUI visual tree is NOT 1:1 with the Duct element tree. A
component that renders a single `Text("Hello")` actually produces
`Border > TextBlock`. In a deeply nested component tree (which is how
well-structured Duct apps should look), you accumulate invisible Borders at
every component boundary. These are not free — each Border participates in
WinUI's measure/arrange layout cycle. React's Fiber architecture, SwiftUI's
view protocol, and Compose's slot table all avoid this overhead.

**6. The element pool uses a ForceDetach hack.** Returning a control to the pool
requires round-tripping it through a scratch `StackPanel`:

```csharp
_scratchPanel ??= new StackPanel();
_scratchPanel.Children.Add(element);    // Re-parent
_scratchPanel.Children.Remove(element);  // Detach
```

WinUI has internal parent tracking that doesn't fully release on logical removal.
Without this workaround, re-parenting a pooled control causes `COMException`.
This is the kind of hack that works until a WinUI update changes the internal
behavior, and it betrays how much the framework is fighting the platform rather
than working with it.

**7. No concurrent/interruptible rendering.** React 18's concurrent mode allows
rendering to be interrupted and resumed, preventing long renders from blocking
user interaction. Duct's render cycle is fully synchronous and runs to
completion. For complex UIs with many components, a state change triggers a full
synchronous re-render of the entire dirty subtree, which blocks the UI thread.
There is no mechanism to yield back to the event loop mid-render, prioritize
urgent updates, or time-slice work.

---

## 5. Layout System

### What Duct has

- `VStack` / `HStack` (StackPanel)
- `Grid` with string-based column/row definitions
- `Canvas` with absolute positioning
- `RelativePanel` with named references
- `FlexPanel` (CSS Flexbox via Yoga port) — **Duct-exclusive**
- `WrapGrid` (VariableSizedWrapGrid)
- `ScrollView`
- `Border`

### Critiques

**1. Grid definitions are stringly-typed.** `Grid(["*", "Auto", "200"], ["*"])`
uses string arrays. A typo like `"Atuo"` silently fails at runtime. SwiftUI uses
`GridItem(.flexible())`, `GridItem(.fixed(200))`. Even WinUI's own XAML
validates `ColumnDefinition` values at parse time. Duct's pure-string approach
is a regression in type safety.

**2. No Spacer equivalent.** SwiftUI's `Spacer()` is one of its most-used
layout tools — a flexible element that expands to fill available space. Duct has
no equivalent. You'd need to use `.HAlign(HorizontalAlignment.Stretch)` or Flex
layout with `grow`, neither of which is as intuitive as `Spacer()`.

**3. FlexPanel is impressive but duplicates WinUI's layout system.** Duct
ships a full CSS Flexbox implementation via a Yoga port. This is a significant
engineering effort that creates a parallel layout system alongside WinUI's
native layout. It means:
- Two layout systems with different mental models
- Flex children can't participate in Grid layout and vice versa
- Performance overhead of running Yoga's layout algorithm on top of WinUI's
- Debugging layout issues requires understanding which system is in play

SwiftUI and Compose don't have this problem because they own the entire layout
pipeline. Duct bolted Flexbox onto a platform that has its own (different) layout
model.

**4. No safe area or inset handling.** WinUI desktop apps have title bars,
task bars, and potentially custom chrome. Duct has no `SafeArea` concept. SwiftUI
has `.ignoresSafeArea()` and `.safeAreaInset()`. Compose has `WindowInsets`. Duct
developers must manually account for title bar height and other insets.

**5. Responsive layout is hook-based, which forces full re-renders.**
`UseWindowSize()` and `UseBreakpoint()` trigger a full component re-render
when the window resizes. SwiftUI's `@Environment(\.horizontalSizeClass)` only
invalidates views that read it. Compose's `BoxWithConstraints` only recomposes
the contained scope. Duct's approach means resizing a window re-renders the
entire component tree that uses any responsive hook.

---

## 6. Styling and Theming

### The theming story: from disaster to functional (with caveats)

The previous version of this review called theming "architecturally broken" — a
P0 blocker. That is no longer accurate. The `ThemeRef` system has landed and
works. The honest assessment now: **theming is functional for the common case
but has real implementation concerns and remaining gaps.**

### What shipped

**ThemeRef tokens and modifier overloads.** Duct now provides ~40 semantic theme
tokens (`Theme.PrimaryText`, `Theme.Accent`, `Theme.CardBackground`, etc.) and
`Theme.Ref("AnyResourceKey")` for custom WinUI resources. These plug into the
existing modifier API naturally:

```csharp
Text("Hello").Foreground(Theme.PrimaryText)
VStack(children).Background(Theme.CardBackground)
Button("Go").Background(Theme.Accent)
```

**Theme change detection.** `DuctHost` subscribes to `ActualThemeChanged` on the
root content element and triggers a full re-render. Theme switches propagate
automatically — no developer action needed.

**Reconciler integration via XAML Style injection.** The `ApplyThemeBindings`
method constructs a WinUI `Style` with `{ThemeResource}` setters and assigns
it to each element. This delegates theme resolution to WinUI's native machinery,
which correctly handles Light/Dark/HighContrast and per-element
`RequestedTheme` overrides.

**The three-tier model works.** Unstyled elements (Tier 3) theme automatically.
Theme token references (Tier 2) resolve and re-resolve on theme change. Local
concrete values (Tier 1) override everything, as documented. The "impossible
choice" from the previous review is resolved — developers can now use
`.Background(Theme.CardBackground)` for themed colors and
`.Background("#FF5733")` for explicit overrides, and both behave correctly.

### What's actually good about this

The architecture decision to generate `{ThemeResource}` styles rather than
manually resolving brushes is smart. It means WinUI handles all the theme
resolution complexity (theme dictionaries, merged dictionaries, HighContrast,
per-element RequestedTheme overrides) rather than Duct trying to replicate it.
This is the right call — let the platform do what it's good at.

The token catalog is well-chosen. It covers the main WinUI semantic brush
categories (text, accent, fill, stroke, background, signal) and `Theme.Ref()`
provides an escape hatch for any resource key. A developer who sticks to these
tokens gets correct dark mode and high contrast support for free.

### What's still concerning (skeptic's view)

**1. XamlReader.Load on every theme-bound element, every render.** The core of
`ApplyThemeBindings` is:

```csharp
var xaml = $"<Style ...><Setter Property='...' Value='{{ThemeResource ...}}'/></Style>";
var style = (Style)XamlReader.Load(xaml);
fe.Style = style;
```

This constructs a XAML string, parses it through `XamlReader.Load()` (which
invokes the XAML parser), creates a `Style` object, and assigns it to the
control — on every mount AND every update. For a screen with 50 theme-bound
elements, that's 50 XAML parse operations per render cycle. `XamlReader.Load`
is not cheap — it involves string allocation, XML parsing, and WinUI object
creation through the XAML type system.

React's CSS custom properties are resolved by the browser's style engine with
zero JavaScript cost. SwiftUI's semantic colors are resolved at draw time by
the rendering pipeline. Compose's `MaterialTheme.colorScheme` is a
`CompositionLocal` read with no parsing overhead. Duct's approach works but it's
the heaviest implementation possible — string building + XML parsing + object
allocation per element per render.

There is no caching — if the same `Theme.Accent` token is used on 20 buttons,
20 separate XAML strings are built and parsed, producing 20 separate Style
objects. A style cache keyed by (targetType, bindingSet) would eliminate most
of this cost.

**2. Style assignment clobbers existing styles.** `ApplyThemeBindings` does
`fe.Style = style` (with `BasedOn` chaining if a style already exists). But
this means every theme-bound element gets a dynamically generated style that
replaces (or chains on top of) whatever style WinUI would have naturally
applied. This interacts poorly with:

- `.ApplyStyle("AccentButtonStyle")` — the theme binding overwrites it (or
  chains, changing precedence)
- Implicit styles from WinUI's style system — the dynamically-applied style
  becomes the effective style, potentially blocking implicit style resolution
- Lightweight styling — when this is eventually implemented, it will need to
  coordinate with the generated styles

React and Compose don't have this problem because their theming operates at a
different layer (CSS variables / composition locals) that doesn't conflict with
the component styling system.

**3. Hard-coded color values are still a trap with no guardrails.**
`.Background("#FF5733")` still does a local-value set that silently breaks
theming. The framework provides no warning, no lint, no diagnostic. A developer
who writes `.Background("#FF5733")` instead of `.Background(Theme.Accent)` gets
a working app in light mode and a broken app in dark mode with no indication of
the mistake.

SwiftUI solves this by making semantic colors the default — `Color.primary`,
`Color.accentColor`. You have to go out of your way to use a hard-coded color.
In Duct, hard-coded strings are the *first* thing developers learn (every
tutorial, every sample until the Color Gallery), and theme tokens are the
advanced feature. This is backwards — the pit of success should lead to themed
colors, not hard-coded ones.

**4. No custom theme resource definitions.** `Theme.Ref("key")` can reference
any *existing* WinUI resource, but there's no way to define new theme resources
from Duct. The `DuctThemeResources` class from the theming design spec is not
implemented. This means:

- No branded colors that adapt to light/dark (e.g., "Brand.Primary" that's blue
  in light mode and light-blue in dark mode)
- No app-specific semantic tokens (e.g., "PricingPositive" / "PricingNegative")
- Developers must define custom resources by manually creating
  `ResourceDictionary` entries via Application.Resources — the same escape
  hatch that Duct is supposed to abstract away

React's Material UI `createTheme()`, SwiftUI's asset catalogs with named colors,
and Compose's `lightColorScheme()`/`darkColorScheme()` all provide this. Duct
only lets you reference the platform's built-in palette.

**5. No UseTheme / UseHighContrast hooks.** There's no way for a component to
read the current theme or react to theme changes in its render logic. The
`DuctHost` re-renders on theme change, but a component can't do:

```csharp
var isDark = UseTheme();  // ← doesn't exist
return Text(isDark ? "🌙" : "☀");
```

The workaround is `.Set(fe => fe.ActualTheme)` in an effect, which is clunky
and breaks the declarative model. SwiftUI has `@Environment(\.colorScheme)`.
Compose has `isSystemInDarkTheme()`. React has `prefers-color-scheme` media
queries.

**6. No .RequestedTheme() modifier.** Setting per-element theme (e.g., a dark
panel in a light app) requires `.Set(b => b.RequestedTheme = ElementTheme.Dark)`
— the theming design spec proposes `.RequestedTheme(ElementTheme.Dark)` but
it's not implemented. The gallery sample itself uses the `.Set()` workaround.

**7. Only three properties support ThemeRef bindings.** `ApplyThemeBindings`
maps "Background", "Foreground", and "BorderBrush" — that's it. There's no
theme binding support for:
- `Fill` / `Stroke` on shapes (Rectangle, Ellipse, Path)
- `Foreground` on TextBlock (handled separately from Control.Foreground)
- `PlaceholderForeground` on TextBox
- `SelectionHighlightColor`, `CaretBrush` on text controls
- Any other brush property

SwiftUI's semantic colors work on any color property. Compose's
`MaterialTheme.colorScheme` works anywhere. Duct's ThemeRef only works on the
three most common brush properties.

### Revised theming verdict

**Previously: F (architecturally broken). Now: C+.**

The ThemeRef system closes the P0 gap. A developer who uses `Theme.*` tokens
gets correct dark mode and high contrast for background, foreground, and border.
This is a significant improvement — the "impossible choice" is gone for the
common case.

But the implementation has performance concerns (XamlReader.Load per element per
render), the API surface is narrow (3 properties, no custom resources, no hooks),
there are no guardrails against hard-coded colors, and the style injection
approach may cause conflicts with WinUI's own styling system. The competition's
theming is built into the rendering pipeline; Duct's is bolted on via XAML
string generation.

### Other styling issues (unchanged)

**No style composition or reuse.** SwiftUI has `ViewModifier` for creating
reusable style bundles. Compose has `Modifier` which is composable. Duct has no
mechanism to group modifiers into reusable units. You can create extension methods
as a workaround, but there's no framework-level concept of a "style."

**No lightweight styling (theme resource key overrides).** WinUI's lightweight
styling lets you override specific theme resource keys per-control to customize
appearance while preserving theme reactivity. Duct has no DSL for this. It's
listed as a P1 gap.

**ApplyStyle is a string-based runtime lookup.** `.ApplyStyle("AccentButtonStyle")`
does a dictionary lookup in `Application.Current.Resources` at runtime. A typo
in the style name silently fails (returns null, sets Style to null). No
compile-time validation.

---

## 7. Navigation

### The navigation story: from "architecturally blocked" to competitive

The previous version of this review scored Navigation at F — "Roll your own
string-based switch statement." That was the single most damning critique in the
document. A comprehensive navigation system has now shipped: type-safe routes,
developer-owned back stack, composition-layer GPU transitions, lifecycle hooks
with cancellation guards, LRU page caching, JSON state serialization, deep
linking, and NavigationView integration. 117 unit tests across 6 test files
validate the implementation. A feature-complete sample app demonstrates all
capabilities. The honest assessment now: **navigation is architecturally strong
and competitive with the industry's latest thinking, but has implementation gaps
and missing E2E validation that temper confidence.**

### What Duct has now

```csharp
// Define routes as C# records — type-safe, serializable, pattern-matchable
record HomeRoute;
record DetailRoute(int Id);
record SettingsRoute;
record ProfileRoute(string UserId, string? Tab = null);

// Root: create navigation stack with initial route
var nav = UseNavigation<AppRoute>(initial: new HomeRoute());

// Child: retrieve ancestor's handle via DuctContext
var nav = UseNavigation<AppRoute>();

// Navigate with full type safety
nav.Navigate(new DetailRoute(42));
nav.GoBack();
nav.GoForward();
nav.Replace(new SettingsRoute());
nav.Reset(new HomeRoute());       // clear stack
nav.PopTo(r => r is HomeRoute);   // pop until match

// Render current route
NavigationHost(nav, route => route switch {
    HomeRoute => Component<HomePage>(),
    DetailRoute d => Component<DetailPage, int>(d.Id),
    SettingsRoute => Component<SettingsPage>(),
    _ => Text("Unknown route")
}) with {
    Transition = NavigationTransition.Slide(),
    CacheMode = NavigationCacheMode.Enabled,
    CacheSize = 10
}

// Lifecycle hooks with navigation guards
UseNavigationLifecycle(
    onNavigatedTo: ctx => LoadData(ctx.Route),
    onNavigatingFrom: ctx => { if (hasUnsavedChanges) ctx.Cancel(); },
    onNavigatedFrom: ctx => Cleanup()
);

// State serialization
var json = nav.GetState();   // full stack to JSON
nav.SetState(json);          // restore from JSON

// Deep linking
var deepLinks = new DeepLinkMap<AppRoute>()
    .Map("/detail/{id:int}", args => new DetailRoute(args.Get<int>("id")))
    .Map("/settings", _ => new SettingsRoute());

// NavigationView integration
NavigationView(menuItems,
    NavigationHost(nav, routeMap)
).WithNavigation(nav, routeToTag, tagToRoute)
```

### What the competition provides now (2026 update)

**React Router v7** (stable, Nov 2024+): Three operating modes (declarative,
data, framework). Type safety via code generation (`npx react-router typegen`).
View Transitions API integration. Data loaders/actions for SSR. **But**: the
back stack is opaque — developers interact via `useNavigate()`, not by owning
the stack. Type safety is bolt-on (code-generated `.d.ts`), not intrinsic.

**TanStack Router** (v1.0 via TanStack Start, March 2026): 100% type-safe
routing with full inference — no code generation. Search params as typed state.
This is now the type-safety gold standard in the React ecosystem.

**SwiftUI NavigationStack** (iOS 16+, refreshed WWDC 2025): `NavigationPath`
is developer-owned and `Codable` (serializable). `navigationDestination(for:)`
maps types to views. WWDC 2025 added "Liquid Glass" visual refresh and improved
deep-linking. **But**: `NavigationPath` is type-erased — you can append and
count, but can't inspect or modify entries by type without workarounds.

**Compose Navigation 3** (stable Nov 2025 — **the biggest competitive shift
since the last review**): Ground-up rewrite that mirrors Duct's philosophy
almost exactly. Developer-owned back stack (`SnapshotStateList<T>`). Type-safe
routes via `@Serializable` data classes. `NavDisplay` observes the list and
renders content. `Scene`/`SceneStrategy` for adaptive multi-pane layouts
(list-detail on wide screens). **This is now the direct competitor to Duct's
navigation model.**

| Capability | Duct | React Router v7 | SwiftUI | Compose Nav3 |
|---|---|---|---|---|
| Type-safe routes | Native (C# records) | Codegen | Native (Swift types) | Native (@Serializable) |
| Developer-owned back stack | Yes (IReadOnlyList) | No (opaque) | Partial (type-erased) | Yes (SnapshotStateList) |
| GPU-accelerated transitions | Yes (composition layer) | View Transitions API | System-managed | Basic |
| Lifecycle guards (cancel nav) | Yes | useBlocker() | beforeRemove | Developer-managed |
| Page caching (LRU) | Yes | N/A | N/A | Developer-managed |
| State serialization | Yes (JSON) | N/A | Codable | Developer-managed |
| Deep linking | Yes (pattern matching) | Built-in (URL) | Built-in (URI) | Developer-managed |
| Adaptive multi-pane | No | N/A (web) | Limited | Yes (SceneStrategy) |
| Nested navigation | Yes (independent stacks) | Yes | Yes | Yes |
| NavigationView chrome | Yes (auto-sync) | N/A | Built-in | N/A |

### What's actually good about this (credit where due)

**The architectural decision to bypass WinUI Frame is correct and
well-justified.** The design spec documents exactly why Frame doesn't work:
XAML type metadata requirements, IPage interface hard-casts, parameterless
constructor constraints, no extension points. These are hard constraints in
WinUI's C++ code. Rather than fighting the platform, Duct built its own
navigation on a `ContentPresenter`/`Grid` host with the reconciler managing
content swap. This is the same architectural choice Compose Nav3 made —
own the stack, own the rendering, let the framework manage it.

**The developer-owned back stack is the right call.** Both Compose Nav3 and
SwiftUI converged on "navigation state is a list the developer controls." Duct
arrived at this independently. `NavigationStack<TRoute>` with `Navigate`,
`GoBack`, `GoForward`, `Replace`, `Reset`, `PopTo` is a clean, complete API
surface. The stack is an `IReadOnlyList<TRoute>` — inspectable, serializable,
testable. SwiftUI's `NavigationPath` is type-erased (you can't pattern-match on
entries), which is arguably worse.

**C# records as routes is clever.** Records give you structural equality,
immutability, `with` expressions, pattern matching in `switch`, and JSON
serialization for free. A `DetailRoute(int Id)` is more type-safe and more
ergonomic than Compose Nav3's `@Serializable data class Detail(val id: Int)`
(roughly equivalent) and strictly better than React Router's string-template
params (`"/detail/:id"`).

**Composition-layer transitions are a genuine differentiator.** The transition
engine uses `ElementCompositionPreview.GetElementVisual()` and runs slide/fade/
drill-in/spring animations on the compositor thread — zero managed-code
callbacks during animation, zero UI thread blocking. The automatic direction
reversal on GoBack (slide-from-right becomes slide-to-right) is a nice touch.
Per-navigation transition overrides (`nav.Navigate(route, new NavigateOptions {
Transition = NavigationTransition.DrillIn() })`) provide fine-grained control.
No competitor has GPU-accelerated transitions as a first-class navigation
feature.

**The lifecycle hook system is well-designed.** `UseNavigationLifecycle` with
`onNavigatedTo`, `onNavigatingFrom` (with cancellation), and `onNavigatedFrom`
follows the established navigation lifecycle pattern. The ordering is correct:
guard fires before stack mutation, mount and `onNavigatedTo` happen on the new
page, `onNavigatedFrom` fires after the old page is swapped out. The
cancellation mechanism (via `NavigatingFromContext.Cancel()`) is clean and
the guard runs synchronously before the stack mutates — no race conditions.

**The NavigationView integration is thoughtful.** `.WithNavigation(nav,
routeToTag, tagToRoute)` auto-syncs selected item, wires selection change to
navigate, and manages back button visibility/state. This is the kind of
framework integration that eliminates 30+ lines of boilerplate in every app.

**117 unit tests is substantial coverage.** The test suite covers stack
operations, host rendering, lifecycle ordering, caching, serialization/deep
linking, and NavigationView sync. The self-host test pattern (driving navigation
without WinUI controls) enables fast, reliable CI.

### What's still concerning (skeptic's view)

**1. ConnectedTransition is a stub.** The `NavigationTransition.Connected()`
factory exists and is documented, but `TransitionEngine.RunTransition` logs a
warning and falls back to `SlideTransition`. This is a spec-driven API that
doesn't work. A developer who writes `Transition = NavigationTransition.Connected()`
gets a slide instead of a connected animation, with the only indication being a
debug log message. Shipping a named API that doesn't do what the name says is
worse than not shipping it — it's a trap. SwiftUI's `matchedGeometryEffect` and
Compose's `SharedTransitionLayout` both work. Duct's doesn't.

**2. E2E Appium tests are fixtures only — not executed.** The navigation
implementation tasks show 6 E2E test scenarios marked as incomplete: NavigationView
click, back button, navigation guards, multi-level deep navigation, selection
sync, and tab state preservation. The 117 unit tests validate internal logic, but
nobody has verified that pushing a route actually shows the right page on screen,
that transitions are visible, or that NavigationView selection syncs visually.
For a feature this central, the gap between "unit tests pass" and "it works in a
real window" is not trivial.

**3. No adaptive multi-pane layout.** Compose Nav3's `Scene`/`SceneStrategy`
abstraction handles the list-detail pattern: on a wide screen, show list and
detail side-by-side; on a narrow screen, push detail onto the stack. This is
increasingly important even for desktop apps (think adaptive panels in Outlook,
Teams, VS Code). Duct's navigation is strictly single-pane — one route renders
one page. Building a master-detail layout requires manual composition outside the
navigation system. For a desktop-first framework, this is an odd omission.

**4. Deep link patterns are stringly-typed at the boundary.** `DeepLinkMap.Map(
"/detail/{id:int}", args => ...)` uses string patterns with string parameter
names. A typo in `"{id:int}"` silently fails to match. The `RouteArgs.Get<T>(
"id")` call is a string-keyed dictionary lookup with a runtime cast. The irony
is thick — Duct chose C# records for routes specifically for type safety, then
introduced string-based pattern matching at the deep link boundary. The type
safety is only skin deep: the deep link system connects untyped URI strings to
typed routes through a stringly-typed middle layer.

**5. The Grid wrapper adds another invisible element.** `MountNavigationHost`
creates a Grid as the container for navigation content. This is the same
problem as the Border wrapper for components (Section 4, critique #5) — the
WinUI visual tree accumulates framework-internal containers that participate in
layout but serve no visual purpose. A NavigationHost inside a component inside
a NavigationView produces: NavigationView > ... > Border (component) >
Grid (nav host) > content. Every extra layout container costs measure/arrange
time.

**6. Cache restore may skip transitions.** When a cached page is restored, the
reconciler skips remounting (the cached `UIElement` is reattached directly).
It's unclear whether transitions run during cache restore. If they don't, the
user sees an instant snap when navigating back to a cached page but a smooth
slide when navigating to an uncached one — an inconsistency that would feel
broken.

**7. No navigation middleware or route guards beyond `onNavigatingFrom`.**
React Router has loaders that fetch data before navigation completes. SwiftUI
has `navigationDestination` modifiers that can be conditional. Duct's guard
system is limited to the outgoing page — there's no mechanism for the
*destination* to reject or defer navigation (e.g., "load data first, show
loading state, then transition"). The `onNavigatingFrom` guard is useful for
"are you sure?" dialogs but not for data-dependent navigation.

**8. UseSystemBackButton is a separate opt-in hook.** The developer must
explicitly call `UseSystemBackButton(nav, window)` to wire Alt+Left and the
system back button. For a desktop framework where back-navigation is a core
interaction, this should be default behavior that you opt *out* of, not opt
*in* to. Every NavigationHost should respond to Alt+Left unless told not to.

**9. The sample app is the only integration test.** The NavigationDemo sample
app is comprehensive (routes, guards, transitions, caching, deep linking,
nested navigation). But it's a demo, not a test. There's no automated
verification that it works. The existing showcase apps (Outlook clone, file
manager) haven't been updated to use the new navigation system — meaning the
framework's most complex apps still use the hand-rolled `UseState` switch
pattern.

### Revised navigation verdict

**Previously: F (blocked). Now: B+.**

The improvement is extraordinary — from "architecturally blocked" to a
comprehensive system that's competitive with the industry's latest and best.
The type-safe route model, developer-owned back stack, and composition-layer
transitions are genuinely strong. The design philosophy aligns with the modern
consensus (Compose Nav3, SwiftUI NavigationStack) and the implementation is
substantial (117 tests, 1200+ lines of core code, full sample app).

The grade is B+ rather than A because of real gaps: ConnectedTransition is a
stub, E2E tests are unexecuted, no adaptive multi-pane layout, deep linking
reintroduces string-typing, and the showcase apps haven't adopted it. The
navigation system is architecturally sound but not yet battle-tested. A B+ for
a feature that was an F three months ago is a remarkable trajectory — but the
last 20% (adaptive layouts, connected transitions, E2E validation, showcase
adoption) is where production confidence comes from.

---

## 8. Commanding

### The commanding story: a novel framework feature with real gaps

The previous version of this review scored Commands at F — "No ICommand
equivalent." A comprehensive commanding system has now shipped: immutable
`DuctCommand` records that bundle execute + canExecute + label + icon +
description + keyboard accelerator, 16 standard commands, a `UseCommand` hook
for async lifecycle, focus-scoped keyboard accelerators via `CommandHost`,
command-aware DSL overloads for Button/AppBarButton/MenuItem, and ICommand
interop. The honest assessment: **commanding is a genuine framework
differentiator — no competing declarative framework provides this — but it
has performance concerns and missing capabilities that limit its claim to
being a complete solution.**

### Why this matters (the competitive context)

The commanding design spec's research is correct and worth repeating:

- **React** has no command abstraction. Third-party command palette libraries
  (`cmdk`, `kbar`) are UI components, not command registries.
- **SwiftUI** has `CommandMenu`/`CommandGroup` for macOS menu bars and (as of
  iPadOS 26) iPad menu bars. But there's no bundling — each menu item repeats
  its label, icon, shortcut, and action. No "define once, use everywhere."
- **Compose** has nothing. No commanding abstraction whatsoever.
- **Every serious app builds a custom command registry.** VS Code, Files App,
  Windows Terminal all rolled their own. This is a real, unsolved gap.

Duct filling this gap is the single most novel feature in the framework. It's
the one area where a skeptic has to concede: Duct provides something the
competition genuinely doesn't.

### What Duct has now

```csharp
// Define once — immutable record with all metadata
var saveCmd = new DuctCommand {
    Label = "Save",
    ExecuteAsync = async () => await SaveDocumentAsync(),
    Icon = new SymbolIconData("Save"),
    Accelerator = new KeyboardAcceleratorData(VirtualKey.S, VirtualKeyModifiers.Control),
    CanExecute = isDirty,
    Description = "Save the current document"
};

// Or use standard commands (16 built-in)
var cutCmd = StandardCommand.Cut(() => CutSelection(), canExecute: hasSelection);
var copyCmd = StandardCommand.Copy(() => CopySelection(), canExecute: hasSelection);
var pasteCmd = StandardCommand.Paste(() => PasteFromClipboard());

// UseCommand hook for async lifecycle (auto-debounce, IsExecuting tracking)
var save = UseCommand(saveCmd);
// save.IsExecuting is true during async operation
// save.IsEnabled is false while executing — buttons auto-disable

// Use in N surfaces from one definition
CommandBar(
    AppBarButton(cutCmd),    // auto-maps Label, Icon, Accelerator, IsEnabled
    AppBarButton(copyCmd),
    AppBarButton(pasteCmd)
)
MenuBar(
    MenuBarItem("Edit",
        MenuItem(cutCmd),    // same command, same metadata, different surface
        MenuItem(copyCmd),
        MenuItem(pasteCmd)
    )
)

// Per-site overrides via record with-expressions
MenuItem(deleteCmd with { Label = "Remove from list" })

// Focus-scoped keyboard accelerators
CommandHost([saveCmd, undoCmd, redoCmd],
    VStack(editorContent)    // Ctrl+S/Z/Y only work inside this region
)

// Parameterized commands
var deleteItem = new DuctCommand<Item> {
    Label = "Delete",
    Execute = item => RemoveItem(item),
    Icon = new SymbolIconData("Delete"),
    CanExecute = canDelete
};
MenuItem(deleteItem, selectedItem)  // binds parameter at use site

// ICommand interop for migration
var legacyCmd = CommandInterop.FromCommand(viewModel.SaveCommand, "Save");
```

### What's actually good about this (credit where due)

**The "CanExecute is a bool" design is brilliant in context.** Traditional
`ICommand` uses `CanExecuteChanged` events to notify the UI when enablement
changes. This is an event-based mechanism fighting a reactive framework. Duct's
approach: commands are created during `Render()`, which runs on every state
change. `CanExecute` is just a bool that naturally reflects current state
because `isDirty` or `hasSelection` are already reactive. No events needed.
The framework's re-render cycle IS the notification mechanism. This is the
insight that makes the whole design work — it eliminates the impedance mismatch
between ICommand's event model and Duct's declarative model.

**The "define once, use everywhere" pattern works.** One `DuctCommand` drives
`AppBarButton`, `MenuItem`, `Button`, keyboard accelerator, and tooltip from a
single definition. The DSL overloads (`Button(cmd)`, `AppBarButton(cmd)`,
`MenuItem(cmd)`) auto-map all metadata fields to the appropriate WinUI
properties. Per-site overrides via `cmd with { Label = "..." }` let you
customize without duplicating. This is exactly the capability VS Code, Files,
and Windows Terminal all built custom registries to achieve.

**StandardCommand is a nice convenience.** `StandardCommand.Cut(action)` gives
you the correct icon (SymbolIcon("Cut")), the correct keyboard accelerator
(Ctrl+X), and a label — ready to use. All 16 standard commands (Cut, Copy,
Paste, Undo, Redo, Delete, SelectAll, Save, Open, Close, Share, Play, Pause,
Stop, Forward, Backward) are implemented. This eliminates the "look up the
right icon and accelerator" dance that wastes time in every WinUI app.

**UseCommand for async is well-designed.** The hook wraps async commands with
`IsExecuting` tracking and re-entrance prevention. During `ExecuteAsync`, the
command auto-disables (buttons go gray). When it completes, it re-enables.
The implementation is clean: `UseState(false)` for the executing flag,
`UseMemo` for the wrapped execute with the guard. Sync-only commands skip hook
state entirely (no slot waste), which is a thoughtful optimization.

**Focus-scoped accelerators are the right model for desktop.** `CommandHost`
creates a region where keyboard accelerators are active only when focus is
within the host. This solves the "Ctrl+S means different things in different
panels" problem that desktop apps face. The implementation checks
`IsDescendantOf(focused, host)` before invoking — simple and correct.

### What's still concerning (skeptic's view)

**1. Accelerators are rebuilt on every render.** `UpdateCommandHost` clears and
recreates all `KeyboardAccelerator` objects on the WinUI `UIElement` every
render cycle. This is O(n) COM interop calls per render per CommandHost, where
n is the number of commands with accelerators. The reason is that accelerator
event handlers capture command closures that reference current state — so they
need fresh closures each render. This is functionally correct but
architecturally expensive. React avoids this with event delegation. Compose
avoids it by not having keyboard accelerators. Duct's approach is the most
direct and the most wasteful.

For a CommandHost with 20 commands (not unusual for a document editor's main
region), that's 20 accelerator creates + 20 accelerator destroys per render.
Each involves COM interop to the WinUI layer. If the component re-renders
frequently (e.g., on every keystroke in a text field), this compounds quickly.

**2. StandardCommand labels are English-only.** `StandardCommand.Cut` has
`Label = "Cut"`. Not `Label = Intl("Cut")`. Not `Label = loc.GetString("Cut")`.
Just a hard-coded English string. This is surprising given that Duct has a full
ICU-based localization system with `DuctContext<IntlAccessor?>` integration.
The framework's own standard commands don't use the framework's own
localization system. A developer localizing their app will discover that
toolbar buttons say "Cut"/"Copy"/"Paste" in English regardless of locale, and
the fix is to create their own command definitions instead of using
`StandardCommand`. This defeats the purpose of standard commands.

For comparison, WinUI's `StandardUICommand` has localized labels for all
supported Windows languages. Duct's `StandardCommand` is a regression from the
platform it's built on.

**3. No command routing to the focused view.** The design spec lists "command
routing to focused view" as a non-goal ("future work — needs focus management
first"). But this is the core use case for Cut/Copy/Paste in a multi-document
or multi-panel app. When the user presses Ctrl+C, which panel's selection gets
copied? `CommandHost` scopes accelerators to a region, but it doesn't solve
the routing problem — if two editors are both inside the same CommandHost, the
command fires on whatever closure was captured at render time, not on the
focused editor.

SwiftUI solves this with `FocusedValue`/`FocusedObject` — the focused view
publishes its cut/copy/paste handlers, and the menu bar reads them. WPF solved
this with `RoutedUICommand` and command routing through the visual tree. Duct
has neither. For a desktop framework, this is a conspicuous gap.

**4. No command palette UI.** The commanding design spec lists this as future
work. The registry data model (commands with labels, icons, accelerators) is
the perfect foundation for a VS Code-style command palette. But the framework
doesn't provide the palette itself. Given that Duct aims to be an opinionated
framework (not just a component library), shipping the registry without the
palette is like shipping a search index without a search box.

**5. CommandHost creates another invisible Grid wrapper.** `MountCommandHost`
creates a Grid to host the accelerators. This is the third invisible wrapper
(component Border, NavigationHost Grid, CommandHost Grid) that accumulates in
the visual tree. A component with a navigation host and a command host produces
Border > Grid > Grid > content — three extra layout containers.

**6. IsDescendantOf visual tree walk on every key press.** When any keyboard
accelerator fires inside a CommandHost, the handler walks the WinUI visual tree
upward from the focused element to check if it's a descendant of the host. For
deep visual trees (which Duct creates via its wrapper elements), this is O(d)
per key press where d is tree depth. Most key presses in a document editor fire
a KeyDown → accelerator check chain. This isn't catastrophic, but it's another
tax on a hot path.

**7. No batch command registration.** Each `CommandHost` independently manages
its accelerators. If multiple `CommandHost` elements exist (e.g., one for
document commands, one for panel commands), accelerators are registered on
separate WinUI elements. There's no global registry, no deduplication, no
priority system. If two CommandHosts register Ctrl+S, both fire (or one
shadows the other depending on focus). The design spec mentions a global
command registry as future work, but the current system is purely local.

**8. DuctCommand equality may defeat memoization.** `DuctCommand` is a record,
so its `Equals` compares all fields structurally — including `Execute` (an
`Action`) and `ExecuteAsync` (a `Func<Task>`). Delegate equality in C# compares
target + method — two lambdas that capture different closure state are never
equal, even if they do the same thing. This means a `DuctCommand` created in
`Render()` is *always* unequal to the one from the previous render (because the
lambda captures fresh state). Components that receive commands as props will
always fail the memo check, defeating the default-on memoization from Section 2.
The `UseCommand` hook doesn't address this — it wraps the command but returns a
new record each time.

### Revised commanding verdict

**Previously: F (no ICommand equivalent). Now: B+.**

The improvement is a leap, and the feature is genuinely novel. No competing
declarative framework provides define-once commands with metadata bundling,
standard commands, async lifecycle, and focus-scoped accelerators. Duct is
ahead of the entire competition here — not catching up, actually leading. The
API design is clean, the integration with the DSL is natural, and the
`UseCommand` hook for async is well-considered.

The grade is B+ rather than A because of implementation concerns: accelerator
rebuild on every render, un-localized standard commands (ironic given the
localization system exists), no command routing to the focused view, delegate
equality defeating memoization, and the missing command palette UI. The
foundation is strong — a command palette, localization, and routing can all be
built on top of what exists. But "the foundation enables it" and "it's shipped"
are different claims, and this review scores what's shipped.

---

## 9. Lists and Collections

### What Duct has

- `ListView(items)` / `GridView(items)` — WinUI controls
- `LazyVStack<T>` / `LazyHStack<T>` — virtualized via ItemsRepeater
- `TemplatedListView<T>` / `TemplatedGridView<T>` — typed templates
- `ForEach` — non-virtualized iteration
- `TreeView` with drag support
- `FlipView`, `SemanticZoom`

### Critiques

**1. ForEach is not virtualized.** `ForEach(items, item => ...)` produces a
`GroupElement` with ALL items rendered. For 1000 items, this creates 1000
elements in memory, diffs all 1000, and mounts all 1000 WinUI controls. React
has the same problem (it needs react-window), but SwiftUI's `ForEach` inside
`List` is virtualized, and Compose's `items()` inside `LazyColumn` is virtualized.
Duct requires explicitly choosing `LazyVStack<T>` for virtualization — a common
footgun for developers who reach for the simpler `ForEach` API first.

**2. LazyVStack requires a key selector.** `LazyVStack<T>(items, keySelector,
viewBuilder)` forces you to provide a key extraction function. React, SwiftUI,
and Compose all have default behaviors for keyless lists (positional matching).
Duct's LazyVStack won't compile without a key selector. While keys are a best
practice, making them mandatory adds friction for prototyping.

**3. No sections or grouping in lists.** SwiftUI has `Section(header:)` inside
`List`. Compose has `stickyHeader {}` in `LazyColumn`. Duct has nothing — to
create a sectioned list, you'd need to manually interleave header elements with
content items and handle all the layout yourself.

**4. No pull-to-refresh.** `RefreshContainer` exists but it's a separate
element, not integrated with ListView. SwiftUI has `.refreshable { }` on List.
Compose has `PullToRefreshBox`. Duct requires manually wrapping a list in a
RefreshContainer and wiring the callback.

**5. No drag-and-drop reordering for lists.** TreeView has drag support, but
ListView and GridView have no built-in reordering. SwiftUI has `.onMove` on
`ForEach`. This must be manually implemented in Duct.

**6. No empty state handling.** All frameworks require manual `if list.isEmpty`
checks, so this isn't a competitive disadvantage — but a production framework
could provide a convenience API like `ListView(items, template, emptyState)`.

---

## 10. Animation

### The animation story: from 5 transitions to a real (if narrow) system

The previous version of this review said Duct had "5 implicit transitions and
nothing else" — everything beyond opacity/scale/rotation/translation/background
required `.Set()`. That's no longer the full picture. A significant animation
diff has landed that adds composition-layer layout animations with spring
physics, connected animations for cross-container transitions, and proper
reconciler lifecycle integration. The honest assessment now: **animation has
genuine new capabilities that go beyond what most new frameworks ship with, but
the scope is still narrow compared to the competition, and the fundamental
"animate any value" problem remains unsolved.**

### What Duct has now

**Tier 1: Implicit transitions (5 properties, unchanged).**

- `.OpacityTransition(duration?)` — ScalarTransition on UIElement.Opacity
- `.RotationTransition(duration?)` — ScalarTransition on UIElement.Rotation
- `.ScaleTransition(transition?)` — Vector3Transition on UIElement.Scale
- `.TranslationTransition(transition?)` — Vector3Transition on UIElement.Translation
- `.BackgroundTransition(duration?)` — BrushTransition on Grid/StackPanel only

These are thin wrappers over WinUI's built-in implicit transition properties.
When you change a value (e.g., `.Opacity(isVisible ? 1.0 : 0.0)`), the
transition animates the change smoothly. They run on the composition thread —
zero managed-code involvement.

**Tier 2: Theme transitions (structural enter/exit).**

- `.WithTransitions(params Transition[])` — sets ChildrenTransitions on
  panels/borders/content controls
- `.ItemContainerTransitions(params Transition[])` — sets ItemContainerTransitions
  on ListView/GridView

These use WinUI's `EntranceThemeTransition`, `AddDeleteThemeTransition`,
`RepositionThemeTransition`, etc. directly — no shadow types. When items are
added to or removed from a container, WinUI animates them automatically.

**Tier 3: Layout animations (NEW — composition-layer position/size).**

```csharp
// Linear offset animation (300ms default)
Border(child).LayoutAnimation()

// Custom duration
Border(child).LayoutAnimation(TimeSpan.FromMilliseconds(500))

// Spring physics
Border(child).SpringLayoutAnimation(dampingRatio: 0.8f, period: 0.1f)

// Full config: spring + size animation
Border(child).LayoutAnimation(new LayoutAnimationConfig
{
    UseSpring = true,
    DampingRatio = 0.6f,
    Period = 0.08f,
    AnimateOffset = true,
    AnimateSize = true
})
```

This is the most substantial new capability. `LayoutAnimationConfig` is a
declarative record that the reconciler applies via
`ElementCompositionPreview.GetElementVisual()` — it sets up
`ImplicitAnimationCollection` entries for "Offset" and optionally "Size" on the
element's composition Visual. When WinUI's layout engine repositions or resizes
the element (list reorder, grid reflow, responsive layout change), the visual
animates smoothly from old to new position. It runs entirely on the composition
thread with zero managed callbacks during animation.

The spring option uses `CreateSpringVector3Animation()` with configurable
damping ratio and period. The linear option uses `CreateVector3KeyFrameAnimation()`
with `InsertExpressionKeyFrame(1.0f, "this.FinalValue")`. Both are correct
composition patterns.

**Tier 4: Connected animations (NEW — cross-container transitions).**

```csharp
// Source (before navigation/switch)
Border(avatar).ConnectedAnimation("hero-image")

// Destination (after navigation/switch — same key)
Border(largeAvatar).ConnectedAnimation("hero-image")
```

The reconciler coordinates with `ConnectedAnimationService`:
- **On unmount:** captures a visual snapshot via
  `service.PrepareToAnimate(key, control)` while the element is still in the
  visual tree
- **On mount:** queues the animation start via `service.GetAnimation(key)`
- **After tree attach:** `DuctHost` calls `FlushConnectedAnimations()` to
  start all queued animations once the new tree is in the visual tree

This two-phase approach handles the timing problem correctly — you can't start
a connected animation until the destination element is laid out, and the
reconciler can't know that during mount. The deferred flush solves it.

### What's actually good about this (credit where due)

The layout animation system is well-engineered. Using composition-layer implicit
animations is the right technical approach for WinUI — it's the same mechanism
WinUI's own XAML controls use internally for layout transitions. The spring
physics option provides natural motion that feels native. And the
`LayoutAnimationConfig` record is cleanly declarative — you set it once on the
element, the reconciler wires up the composition plumbing, and layout changes
animate automatically with no imperative code.

The connected animation integration is thoughtful. The two-phase
prepare-on-unmount / start-after-mount pattern correctly handles the lifecycle
timing that makes connected animations tricky. Other frameworks either don't
support this (React has no built-in equivalent) or require significant
boilerplate (Compose's `SharedTransitionLayout`). Duct's
`.ConnectedAnimation("key")` modifier is genuinely simple.

The reconciler lifecycle ordering is correct and important: transitions are
applied AFTER modifiers and theme bindings but the code is structured so they're
in place before property values change. On update, layout animations are
properly cleared when the config is removed (`ClearLayoutAnimation()`). This
attention to lifecycle detail prevents subtle animation bugs.

The `LayoutAnimationConfig` documentation is honest about limitations: "Hit-
testing uses the final layout position, not the animated visual position" and
"Size animation is cosmetic: content does not re-layout during the Size
animation." Documenting known limitations is better than hiding them.

### What's still concerning (skeptic's view)

**1. Implicit transitions are still limited to 5 properties.** This is
unchanged and it's the single biggest animation gap. You can't implicitly
animate width, height, corner radius, margin, padding, font size, color (on
non-background elements), or any other property. SwiftUI animates *any* state
change that produces a different view body with `.animation(.spring, value: x)`.
Compose's `animateXAsState` works for any type with a `TwoWayConverter`. Duct
can only implicitly animate the 5 properties that WinUI exposes transition
properties for on UIElement.

This isn't a Duct design limitation — it's a WinUI platform constraint. WinUI
only provides `OpacityTransition`, `RotationTransition`, `ScaleTransition`, and
`TranslationTransition` on UIElement, and `BackgroundTransition` on a few
panel types. There's no general-purpose "animate this dependency property"
mechanism. But the result is the same: developers who want to animate anything
outside these 5 properties must drop to the composition layer via `.Set()`.

**2. No declarative value-driven animation API.** The layout animation and
implicit transitions handle *reactive* animation (the value changes, the
animation follows). But there's no way to declaratively say "animate this value
from A to B with this curve over this duration":

```csharp
// None of these exist in Duct:
var opacity = UseAnimatedValue(1.0, target: isVisible ? 1.0 : 0.0);
var offset = UseSpring(targetX, stiffness: 200, damping: 20);
withAnimation(.easeInOut(0.3)) { setExpanded(true); }
```

React has Framer Motion's `motion.div` with `animate` prop. SwiftUI has
`withAnimation { }` that wraps any state change in an animation context.
Compose has `animateAsState`, `AnimatedContent`, and `Transition`. Duct has no
equivalent — animation is tied to specific properties with specific transition
objects, not to arbitrary state changes.

**3. No enter/exit animations for individual elements.** Theme transitions
provide container-level enter/exit (items appearing in a list animate in). But
conditional rendering of a single element — `isVisible ? Text("Hello") : null`
— still pops in and out with no animation. SwiftUI's `.transition(.slide)` +
`withAnimation` and Compose's `AnimatedVisibility` both solve this elegantly
at the individual element level. Duct's theme transitions only work at the
*container* level — when items are added to or removed from a panel.

You can fake it with `.Opacity(isVisible ? 1 : 0).OpacityTransition()` +
keeping the element mounted, but this means the element is always in the tree
consuming layout space and resources. True enter/exit animation needs the
reconciler to delay unmounting until an exit animation completes — a feature
that doesn't exist.

**4. No keyframe or sequenced animation DSL.** Complex animations often need
keyframes (value at 0%, 30%, 100%) or sequences (fade in, then slide, then
scale). Duct has no declarative syntax for this. React's Framer Motion supports
keyframe arrays. SwiftUI has `KeyframeAnimator` (iOS 17+). Compose has
`keyframes {}` blocks. Duct requires dropping to WinUI's `Storyboard` /
`DoubleAnimation` / `DoubleAnimationUsingKeyFrames` via `.Set()`.

**5. No easing function DSL.** Implicit transitions accept `Duration` but not
custom easing curves. The WinUI `ScalarTransition` and `Vector3Transition`
types support basic duration configuration but not cubic bezier or spring curves
(except through layout animations, which only affect Offset/Size). SwiftUI's
`.easeInOut`, `.easeIn`, `.spring(duration:bounce:)` and Compose's
`FastOutSlowInEasing`, `tween(easing = CubicBezierEasing(...))` provide rich
easing control on any animation. Duct's implicit transitions are linear-ish by
default with no way to customize the curve.

**6. Layout animation has honest but real limitations.** The documentation
correctly notes:
- Hit-testing uses the final layout position, not the animated visual — clicking
  "where the element visually is" during animation may miss
- Size animation is cosmetic — content doesn't re-layout during the Size
  animation, so text may clip or overflow during the transition
- Elements need stable keys (`.WithKey()`) for the reconciler to match them
  across reorders — forgetting keys silently breaks layout animations

These are inherent to composition-layer visual animations (the layout is
committed immediately, only the visual representation animates). But they mean
layout animations work well for "list items reorder" and less well for
"sidebar expands from collapsed to full width."

**7. Connected animations require string-key coordination.** Source and
destination must use the same string key. A typo in the key silently produces
no animation (the `try/catch` in `QueueConnectedAnimationStart` swallows the
failure). There's no compile-time validation that source and destination keys
match. SwiftUI's `matchedGeometryEffect(id:in:)` uses typed Namespace objects;
Compose's `SharedTransitionScope` uses typed keys. Duct uses bare strings.

**8. The VSM replacement is still expensive.** This is unchanged. Duct replaces
WinUI's Visual State Manager with state + conditional rendering. A hover effect
that changes background color triggers a full reconciliation cycle. In WinUI
XAML, VSM transitions run entirely on the composition thread. The new layout
animations help with position changes but don't address the VSM gap for visual
state changes (hover, pressed, disabled, focused states).

**9. No UseAnimation hook.** There's no hook that drives an animation from
component state:

```csharp
// Doesn't exist:
var progress = UseAnimation(0.0, 1.0, duration: 300ms, easing: EaseOut);
// progress smoothly animates from 0 to 1, re-rendering at each frame
```

React Spring's `useSpring`, Framer Motion's `useAnimation`, and Compose's
`Animatable` all provide imperative animation control from component code. Duct
has no bridge between the hooks system and the animation system — they're
completely separate mechanisms.

### Revised animation verdict

**Previously: D (5 implicit transitions, nothing else). Now: C.**

The improvement is meaningful. Layout animations with spring physics are a
genuinely useful capability — list reordering, grid reflows, and responsive
layout changes now animate smoothly with a single modifier. Connected animations
provide cross-container transitions that most declarative frameworks don't have
built-in. The reconciler lifecycle integration is correct and well-considered.

But the grade is C, not B, because the animation system only covers *layout*
motion and 5 specific property transitions. The broader problem — "animate any
state change" — is unsolved. No value-driven animation, no enter/exit at the
element level, no keyframes, no easing control, no animation hooks. A developer
who wants to animate a sidebar opening, a color shifting, a badge counting up,
or a card flipping still needs `.Set()` and WinUI's imperative composition API.
The animation system handles what the composition layer gives you for free and
nothing more.

SwiftUI's `withAnimation { state = newValue }` makes *any* state change
animatable with one line. Compose's `animateAsState` does the same. Duct can't
match this because WinUI doesn't provide a general-purpose "animate this
dependency property" mechanism — but that's a reason, not an excuse. The
framework could provide a `UseAnimation` hook that drives re-renders with
interpolated values, bridging the gap between WinUI's limited implicit
transitions and the rich animation expectations of modern UI development.

---

## 11. Accessibility

### The accessibility story: from checkbox to credible foundation (with limits)

The previous version of this review called accessibility "an afterthought, not a
design principle" — 2 out of 12+ properties exposed, zero tests, everything
else behind `.Set()`. That's no longer accurate. A significant accessibility
diff has landed that implements 16 first-class modifiers, a tiered storage
architecture, and 12 end-to-end UIA tests mapped to specific WCAG 2.1 success
criteria. The honest assessment now: **accessibility has a solid modifier layer
and real validation, but the harder problems — hooks, diagnostics, custom
automation peers, and focus management — remain unbuilt.**

### What shipped

**16 first-class accessibility modifiers across two storage tiers:**

| Property | Modifier | Tier |
|---|---|---|
| AutomationProperties.Name | `.AutomationName()` | Tier 1 (inline) |
| AutomationProperties.AutomationId | `.AutomationId()` | Tier 1 (inline) |
| AutomationProperties.HeadingLevel | `.HeadingLevel()` | Tier 1 (inline) |
| Control.IsTabStop | `.IsTabStop()` | Tier 1 (inline) |
| Control.TabIndex | `.TabIndex()` | Tier 1 (inline) |
| UIElement.AccessKey | `.AccessKey()` | Tier 1 (inline) |
| AutomationProperties.HelpText | `.HelpText()` | Tier 2 (lazy) |
| AutomationProperties.FullDescription | `.FullDescription()` | Tier 2 (lazy) |
| AutomationProperties.LandmarkType | `.Landmark()` | Tier 2 (lazy) |
| AutomationProperties.AccessibilityView | `.AccessibilityView()` | Tier 2 (lazy) |
| Shorthand: hide from AT | `.AccessibilityHidden()` | Tier 2 (lazy) |
| AutomationProperties.IsRequiredForForm | `.Required()` | Tier 2 (lazy) |
| AutomationProperties.LiveSetting | `.LiveRegion()` | Tier 2 (lazy) |
| AutomationProperties.PositionInSet/SizeOfSet | `.PositionInSet()` | Tier 2 (lazy) |
| AutomationProperties.Level | `.HierarchyLevel()` | Tier 2 (lazy) |
| AutomationProperties.ItemStatus | `.ItemStatus()` | Tier 2 (lazy) |
| AutomationProperties.LabeledBy | `.LabeledBy()` | Tier 2 (lazy) — **defined but not reconciler-applied** |
| UIElement.TabFocusNavigation | `.TabNavigation()` | Tier 2 (lazy) |
| Custom AutomationPeer | N/A | **Blocked** — components aren't Controls |

**Lazy sub-record architecture.** Tier 1 properties (HeadingLevel, IsTabStop,
TabIndex, AccessKey) are stored inline on `ElementModifiers` — zero allocation
overhead for elements that don't use them. Tier 2/3 properties live in a
separate `AccessibilityModifiers` record that is only allocated when an advanced
modifier is first applied. A `ModifyA11y()` helper merges sub-records
automatically, and the developer sees a completely flat API surface — all
modifiers look identical at the call site:

```csharp
Button("Search", doSearch)
    .AutomationName("Search documents")
    .AccessKey("S")               // Tier 1 — inline
    .HelpText("Search all files") // Tier 2 — lazy sub-record
    .LiveRegion()                 // Tier 2 — same flat API
```

**12 end-to-end UIA tests via Appium/WinAppDriver.** These are real
out-of-process tests that read properties through the Windows UI Automation
client API — the same pipeline used by Narrator, NVDA, and automated testing
tools. Each test maps to a specific WCAG 2.1 success criterion:

| Test | WCAG | Validates |
|---|---|---|
| `A11y_1_1_1_IconButtonHasAccessibleName` | 1.1.1 | Name on icon-only buttons |
| `A11y_1_1_1_DecorativeImageHiddenFromUIA` | 1.1.1 | AccessibilityView.Raw hides decorative elements |
| `A11y_1_3_1_HeadingLevelsExposed` | 1.3.1 | HeadingLevel (Level1, Level2) |
| `A11y_1_3_1_LandmarksExposed` | 1.3.1 | Navigation & Main landmarks |
| `A11y_1_3_1_FormFieldRequired` | 1.3.1 | IsRequiredForForm |
| `A11y_1_3_1_HierarchyLevels` | 1.3.1 | Level property for tree structures |
| `A11y_2_1_1_AccessKeysExposed` | 2.1.1 | Access key shortcuts (Alt+F, Alt+E) |
| `A11y_3_3_2_FormFieldHasNameAndHelpText` | 3.3.2 | Name + HelpText on form fields |
| `A11y_3_3_2_FullDescriptionExposed` | 3.3.2 | FullDescription for complex elements |
| `A11y_4_1_2_ItemStatusExposed` | 4.1.2 | ItemStatus announcements |
| `A11y_4_1_2_PositionInSetExposed` | 4.1.2 | PositionInSet / SizeOfSet |
| `A11y_4_1_3_LiveRegionPolite` | 4.1.3 | Live region (Polite mode) |
| `A11y_4_1_3_LiveRegionAssertive` | 4.1.3 | Live region (Assertive mode) |

**Reconciler integration with change detection.** `ApplyAccessibilityModifiers()`
in the reconciler compares each property against the previous value before
calling the WinUI `AutomationProperties.Set*()` methods. This avoids redundant
COM interop calls on re-render, following the same pattern used for other
modifiers.

### What's actually good about this (credit where due)

The tiered storage design is smart engineering. Most elements in a typical UI
need zero accessibility annotations (WinUI's built-in automation peers handle
the basics). The few that need annotations usually need only Tier 1 (a heading
level, a tab stop). The rare elements that need advanced annotations (landmarks,
live regions, position-in-set) get a lazy sub-record. This means the common
case (no a11y modifiers) pays zero cost, the typical case (one or two modifiers)
pays minimal cost, and only the advanced case allocates the sub-record. This is
better than a flat struct with 16 nullable fields on every element.

The E2E test approach is genuinely rigorous. Testing through the real UIA
pipeline (out-of-process via WinAppDriver) validates what assistive technology
actually sees, not what the framework thinks it set. This is a higher bar than
React's `eslint-plugin-jsx-a11y` (which checks markup, not runtime behavior) or
SwiftUI's accessibility inspector (which is a developer tool, not a CI test).
If these tests pass, Narrator will actually read the right values. That matters.

The WCAG criterion mapping in the tests is good practice. Each test says
*which* accessibility requirement it validates. This makes it possible to answer
"do we cover WCAG 1.3.1?" by grepping the test file rather than reading
implementation code.

### What's still concerning (skeptic's view)

**1. LabeledBy is defined but not wired.** The `AccessibilityModifiers` record
has a `LabeledBy` property, the `.LabeledBy("EmailLabel")` fluent method exists,
but `ApplyAccessibilityModifiers()` in the reconciler has no code to apply it.
The property is accepted silently and does nothing. This is worse than not
having the API at all — a developer who writes `.LabeledBy("EmailLabel")`
believes they've associated a label with a field, but screen readers see nothing.
No test covers it (because it can't pass). The implementation note suggests it
requires a post-mount tree walk to resolve AutomationId references to elements,
which is non-trivial in a declarative framework. But shipping a no-op API
without a warning is a trap.

**2. Custom AutomationPeer remains architecturally blocked.** This is unchanged
and it's the hardest problem in the accessibility story. WinUI controls provide
screen reader semantics by overriding `OnCreateAutomationPeer()` on `Control`.
Duct components are pure C# classes that emit element trees — they can't override
anything on `Control`. This means:

- A custom "StarRating" component built from Image and Text primitives can't tell
  screen readers "I am a slider with value 3 of 5"
- A custom "DatePicker" built from TextBox and Popup can't announce its role
- Screen readers see the primitive controls, not the semantic composite

This is a fundamental architectural limitation, not a missing feature. React
solves it with ARIA roles on DOM elements. SwiftUI solves it with
`.accessibilityRepresentation {}`. Compose solves it with `Modifier.semantics {
role = Role.Slider }`. Duct has no mechanism at all. The 16 modifiers help with
annotating individual controls, but they can't describe what a *composite
component* is.

**3. No accessibility hooks — the imperative side is missing.** The modifier
system covers declarative annotations (setting static properties). But
production accessibility also needs imperative operations:

- `UseAnnounce()` — triggering a live-region announcement from code (e.g., "3
  items deleted") without needing a visible element
- `UseFocusTrap()` — trapping focus within a modal dialog
- `UseHighContrast()` — detecting high contrast mode in render logic
- `UseReducedMotion()` — respecting user's motion preferences
- `UseScreenReaderActive()` — adapting UI when a screen reader is running

These are all specified in the 7-layer accessibility design doc but none are
implemented. The modifier system is Layer 1. Layers 2–7 (hooks, diagnostics,
convenience DSL) are spec only. A developer who needs to announce a toast
message to screen readers today has no option except `.Set()` on a hidden
live-region element — which is exactly the kind of workaround the framework
should eliminate.

**4. No accessibility diagnostics or linting.** Still unimplemented. There's no
way to detect at build time or runtime that an Image lacks an accessible name,
that a Button has no label, or that a live region is missing. The design spec
describes a diagnostic system with JSON export and even Roslyn analyzers, but
none of it exists. React has `eslint-plugin-jsx-a11y` catching problems at edit
time. SwiftUI has the Accessibility Inspector. Duct has nothing — you discover
accessibility bugs when a screen reader user reports them or when you manually
run the test suite.

**5. Focus management is limited to basic Tab properties.** `.IsTabStop()`,
`.TabIndex()`, and `.TabNavigation()` now exist, which is a significant
improvement over "completely missing." But there's still no:

- Programmatic focus control (`FocusRequester` / `@FocusState` equivalent)
- Focus trapping for modal dialogs
- `XYFocusUp/Down/Left/Right` for directional D-pad/gamepad navigation
- Focus restoration on back-navigation

SwiftUI's `@FocusState` + `.focused()` and Compose's `FocusRequester` +
`Modifier.focusable()` both provide programmatic focus management. Duct covers
the Tab order basics but not the programmatic side. For a framework that
doesn't have a navigation system (see Section 7), the inability to manage focus
programmatically compounds the problem — you can't even build your own
navigation with proper focus restoration.

**6. The test suite validates modifiers but not interaction patterns.** The 12
E2E tests are good at verifying that UIA properties are set correctly. But they
don't test:

- Keyboard navigation flow (can you Tab through a form in order?)
- Live region announcements after state changes (does Narrator actually speak
  when content updates?)
- Focus behavior (does focus move to a dialog when it opens?)
- High contrast rendering (are all elements visible in HC mode?)

These are the accessibility behaviors that break in real apps. Property
annotations are necessary but not sufficient — a button can have a perfect
accessible name and still be unreachable by keyboard. The test suite validates
Layer 1 (annotations) but not Layers 2+ (behavior).

**7. No sample apps demonstrate accessibility.** The Outlook clone, file
manager, registry editor, and word puzzle game don't use any of the new
accessibility modifiers. None of the showcase apps demonstrate heading
structure, landmark regions, live regions, or accessible forms. When the
framework's own demo apps don't dogfood accessibility, it sends a clear signal
about maturity.

### Revised accessibility verdict

**Previously: D- (afterthought). Now: C+.**

The jump is real. Going from 2 properties and zero tests to 16 modifiers, a
tiered architecture, and 12 WCAG-mapped E2E tests is a genuine investment. The
modifier API is well-designed — the lazy sub-record avoids overhead, the flat
fluent surface is discoverable, and the reconciler integration follows the
framework's established patterns. The UIA test approach is rigorous and the
right call for a Windows-native framework.

But the grade is C+, not B, because the modifier system is the *easy* part of
accessibility. Setting `HeadingLevel` on a TextBlock is straightforward WinUI
plumbing. The hard problems — custom automation peers for composite components,
imperative announcements, focus management, diagnostics, high contrast
adaptation — are all unbuilt. Layers 2–7 of the accessibility spec are still
spec-only. And fundamentally, the custom automation peer gap means Duct
components can't describe their own semantics to screen readers, which limits
accessibility to annotating individual primitives rather than building
accessible composites.

The competition's gap has narrowed but not closed. SwiftUI's accessibility
story includes `.accessibilityRepresentation {}` for composite semantics,
`@FocusState` for focus management, and the Accessibility Inspector for
diagnostics. Compose has `Modifier.semantics {}` with full role/state/action
descriptions. React has ARIA on DOM elements plus `eslint-plugin-jsx-a11y`.
Duct now has the annotation layer but lacks the semantic, imperative, and
diagnostic layers that those frameworks provide.

---

## 12. Input Handling and Events

### What Duct has

- **Semantic events on controls:** `OnClick`, `OnChanged`, `OnSelectionChanged`
  — well-covered for all wrapped controls
- **Declarative event modifiers:** `.OnPointerPressed()`, `.OnPointerMoved()`,
  `.OnPointerReleased()`, `.OnTapped()`, `.OnKeyDown()`, `.OnSizeChanged()`
- **Keyboard accelerators:** `Accelerator(key, modifiers)` data records
- **Everything else:** `.Set()` passthrough

### Critiques

**1. No gesture system.** SwiftUI has `.gesture()` with `DragGesture`,
`TapGesture`, `LongPressGesture`, and gesture composition (`.simultaneously`,
`.sequenced`). Compose has `Modifier.pointerInput { detectDragGestures {} }`.
Duct has individual pointer events with no abstraction — you're back to manual
hit testing and state tracking for any gesture more complex than a tap.

**2. Event handler re-attachment is wasteful.** Declarative event handlers
(`.OnPointerPressed()` etc.) "re-attach on every update" per the documentation.
The reconciler detaches the previous handler and attaches the new one on every
render cycle. This is O(n) COM interop calls per render per element with event
handlers. React avoids this with event delegation (one handler on the document).
SwiftUI and Compose handle it at the framework level.

**3. Commanding exists but doesn't cover all input surfaces.** The new
commanding system (Section 8) closes the P0 gap — `DuctCommand` bundles
execute + canExecute + metadata, and `UseCommand` handles async lifecycle. But
the commanding system only integrates with `Button`, `AppBarButton`, and
`MenuItem`. Other command-capable controls (`SplitButton`, `SwipeItem`,
`ContentDialog` actions) still use bare `Action` callbacks. And the absence of
command routing to the focused view (Section 8, critique #3) means Cut/Copy/
Paste in multi-panel apps still requires manual wiring.

**4. Six pointer events but no PointerEntered/Exited modifiers.** The
declarative event handlers include pressed/moved/released but not entered/exited.
Hover effects — one of the most common interaction patterns — require `.Set()`
to wire `PointerEntered`/`PointerExited`. This is an odd omission given that
hover is more common than pointer-move tracking.

**5. No RightTapped, DoubleTapped, or Holding modifiers.** These common
interactions are passthrough only, requiring `.Set()`. Context menus need
right-tap. Double-click is common in desktop apps. Long-press is common in
touch apps.

---

## 13. Developer Experience

### What's good

- **Full IntelliSense and refactoring** — the C# DSL gets IDE support for free
- **Type safety** — mismatched types are caught at compile time
- **No XAML parsing errors** — a common WinUI pain point eliminated
- **No DataContext confusion** — data flows explicitly through props and state

### What's bad

**1. Hot reload exists but with .NET's inherent limitations.** Duct hooks into
.NET's `MetadataUpdateHandler` via `HotReloadService.cs` — when code changes,
`DuctApp.ActiveHost?.RequestRender()` fires and the UI updates while preserving
hook state (UseState values survive because the RenderContext stays in memory).
This works with both Visual Studio's hot reload and `dotnet watch`.

This is genuinely good — and better than "no hot reload." However, .NET hot
reload has well-known limitations: adding new fields, changing type hierarchies,
adding new classes, and lambda changes often require a full restart. These are
exactly the kinds of changes you make during UI development (adding a new
component, restructuring a layout). React's Fast Refresh and Compose's Live Edit
are purpose-built for UI changes and handle a wider range of edits.

**2. Preview system is functional but not a visual designer.** Duct has a
`--preview [ComponentName]` CLI flag that launches a component in isolation, a
`--preview-list` to discover all components, and a `PreviewCaptureServer` that
streams JPEG frames over HTTP. A VS Code extension (`vscode-duct`) consumes this
to show a live preview panel beside the editor, with a component dropdown
selector and automatic switching when the active editor changes.

This is a thoughtful system — the HTTP capture server, frame streaming, and
dynamic component switching without process restart are well-engineered. It's
more than many frameworks provide.

However, it's fundamentally a **screenshot stream** of a real WinUI window, not
an interactive preview. You can't click or interact with the preview in VS Code.
SwiftUI's Xcode Previews are interactive — you can click buttons, type in fields,
and see state changes in the canvas. Compose's Preview renders actual composables
inline in the IDE. Duct's approach is closer to a live screenshot — useful for
visual iteration but not for testing interactions.

The bigger gap is the lack of a **visual designer**. None of the competition
really has this either (SwiftUI's canvas is previews, not a designer), but for
WinUI developers coming from XAML's Visual Studio designer, this is a step
backward. A property inspector, element picker, or layout visualizer would
significantly close this gap.

**3. Error messages from the reconciler are runtime-only.** If you violate hook
rules (calling hooks conditionally), you get a runtime `InvalidOperationException`:
"Hook at index N is X, expected Y. Hooks must be called in the same order every
render." This is a runtime crash, not a compile-time warning. React has the
`eslint-plugin-react-hooks` that catches this at edit time. Duct has no static
analysis for hook rule violations.

**4. Debugging the reconciler requires deep framework knowledge.** When a UI
update doesn't look right, there's no equivalent of React DevTools (component
tree inspector, state viewer, profiler) or Compose's Layout Inspector. You're
left with `System.Diagnostics.Debug.WriteLine` sprinkled through the reconciler
code and the `IDuctLogger` interface.

**5. No performance profiling.** There's no built-in way to measure:
- How long a render takes
- How many elements were diffed
- How many WinUI controls were touched
- Which components are re-rendering unnecessarily
- Memory allocation per render cycle

React has the React Profiler. Compose has recomposition counts in Layout
Inspector. Duct has nothing.

---

## 14. The .Set() Problem

### The escape hatch that carries the framework

`.Set()` is Duct's escape hatch for accessing any WinUI property that doesn't
have a first-class modifier. It takes a lambda that receives the underlying WinUI
control:

```csharp
Button("Click", onClick)
    .Set(b => b.FlowDirection = FlowDirection.RightToLeft)
    .Set(b => {
        b.PointerEntered += (_, _) => setHovered(true);
        b.PointerExited += (_, _) => setHovered(false);
    })
```

### Why this is a problem

**1. It breaks the declarative model.** The entire point of Duct is declarative
UI. `.Set()` is imperative mutation. When you use `.Set()`, you're bypassing the
reconciler — the framework doesn't know what you changed and can't diff it. If
a control is recycled from the pool, your `.Set()` mutations from the previous
owner are still there (the pool only resets properties the reconciler knows
about).

**2. Event handlers wired in .Set() leak.** If you do
`.Set(b => b.PointerEntered += handler)`, that handler is wired on every update
(Set runs during mount AND update). You'll accumulate duplicate event handlers
unless you manually track and remove them. The framework provides no lifecycle
hook for this — no "cleanup on unmount" for Set-based side effects.

**3. A huge fraction of WinUI is .Set()-only.** From the gap analysis:
- All pointer events (entered, exited, pressed, released, moved)
- All gesture events (tapped, double-tapped, right-tapped, holding)
- All manipulation events
- All keyboard events (except OnKeyDown modifier)
- Drag and drop
- Custom storyboard animations
- Composition layer access
- Materials and effects
- Most windowing APIs

When this much of the platform requires the escape hatch, the abstraction is
too thin.

**4. .Set() runs on every render.** The documentation for `OnMount` says it runs
once at mount time, but `.Set()` Setters are `Action<TControl>[]` arrays stored
on the element. These arrays are compared by length in `ShallowEquals`, not by
content — meaning any element with Setters always updates, and the Set callbacks
run on every render. This is both wasteful and surprising.

**5. There's no .Get() — no way to read control state.** `.Set()` lets you
mutate the control, but there's no way to read from it back into the component's
render logic. `.OnMount(control => ...)` captures a reference, but by the time
you use it in a render, you might get stale data. There's no reactive bridge
from control properties back to component state.

---

## 15. Component-by-Component Scorecard

How Duct's individual feature areas compare to the mature frameworks (React,
SwiftUI, Compose).

| Feature Area | Duct | React | SwiftUI | Compose | Notes |
|---|---|---|---|---|---|
| **Component Model** | B+ | A | A | A | Context, memoization, generic hooks; reflection in memo check, no slots |
| **Local State** | A- | A | A | A | Generic hooks, post-render cleanup, persisted state; dep arrays still box |
| **Global State** | B+ | A | A | A | DuctContext + UseContext + .Provide(); boxing in scope stack, no selector |
| **Reconciler** | B- | A | A- | A | Works but monolithic, no concurrent mode |
| **Layout** | B+ | B+ | A | A | Flex is good; Grid is stringly-typed |
| **Theming** | C+ | B+ | A | A | ThemeRef works; XamlReader.Load perf concern; 3 props only; no custom resources |
| **Navigation** | B+ | A | A | A | Type-safe routes, dev-owned stack, GPU transitions, lifecycle guards, caching, serialization, deep linking; ConnectedTransition is stub, no adaptive multi-pane, E2E tests unexecuted |
| **Commanding** | B+ | N/A | C+ | N/A | Define-once commands, 16 standard, async lifecycle, focus-scoped accelerators; no competitor has this. Accelerator rebuild per render, labels not localized, no command routing, no palette UI |
| **Lists/Collections** | B | B+ | A | A | Virtualization exists, no sections |
| **Animation** | C | B | A | A | Layout animations + springs + connected; still no value-driven, no enter/exit, 5-property limit |
| **Accessibility** | C+ | B | A | A | 16 modifiers, UIA E2E tests; no custom peers, no hooks, no diagnostics |
| **Input/Events** | C | B | A | A | Semantic events good; commanding helps but no gesture system, no pointer enter/exit, rest is .Set() |
| **Styling** | C- | B+ | A | A | No style composition; no lightweight styling; ApplyStyle is stringly-typed |
| **Developer Experience** | C+ | A | B+ | B+ | Hot reload works; preview is screenshot-only; no devtools |
| **Control Coverage** | A | N/A | A | A | 94% of WinUI wrapped |
| **Error Handling** | B | B+ | D | D | ErrorBoundary exists (rare feature) |
| **Localization** | B+ | B | B | B | ICU-based, full system, now using DuctContext |
| **Responsive Layout** | B | B+ | A | A | Hooks work but force full re-render |

---

## 16. Conclusion

### The sample apps are telling — and the story has shifted

Duct now has six sample apps: the original four (Outlook clone, file manager,
registry editor, word puzzle game) plus NavigationDemo and CommandingDemo. The
new samples demonstrate their respective features comprehensively:
NavigationDemo covers routes, guards, transitions, caching, deep linking, and
nested navigation. CommandingDemo covers standard commands, async lifecycle,
parameterized commands, focus-scoped accelerators, per-site overrides, and
context-based command sharing.

But the original showcase apps remain frozen in time:
- **Outlook clone** still uses string-based view switching (`currentPage switch
  { ... }`) — it hasn't adopted the navigation system that was built to solve
  exactly this problem
- **DuctFiles** still requires manual `SynchronizationContext` capture for
  off-thread state updates — no async state management
- **Samples that use hard-coded colors** are still broken in dark mode
- **None of the original samples** use the context system, memoization,
  persisted state, commanding, navigation, or the new accessibility modifiers

This is a pattern: every new feature ships with its own isolated demo app, but
the showcase apps — the ones that prove the framework works for *real* UIs —
don't adopt the new features. The Outlook clone is the most telling case: it's
the framework's most complex app, it has the navigation problem, and the
navigation system was explicitly designed to solve it. It's still using
`UseState<string>`.

The gap between "feature works in isolation" and "feature works in a real app"
is where production confidence lives. Duct keeps building features and demo
apps without going back to prove they compose in the existing showcase apps.
This is a red flag for a framework that wants to be production-ready.

### What Duct gets right

1. **The component model foundation is now solid.** Context, memoization, generic
   hooks, persisted state, post-render effect cleanup — these are the core
   mechanics that every declarative UI framework needs, and they're now
   implemented with correct semantics.

2. **Navigation is architecturally competitive.** Type-safe routes via C#
   records, developer-owned back stack, composition-layer GPU transitions,
   lifecycle guards with cancellation, LRU caching, state serialization, deep
   linking. The design independently converged with Compose Nav3's philosophy
   and is arguably stronger on type safety (C# records vs Kotlin data classes)
   and transitions (composition layer vs basic animation).

3. **Commanding is a genuine differentiator.** No competing declarative framework
   provides define-once commands with metadata bundling, standard commands, async
   lifecycle, and focus-scoped accelerators. Duct is ahead of the field here.

4. **Control coverage is impressive.** 94% of WinUI controls wrapped with
   clean factory APIs. This is a huge amount of tedious work done well.

5. **The hooks system is faithful to React and now correctly implemented.**
   UseState, UseReducer, UseEffect, UseMemo, UseRef, UseContext, UseNavigation,
   UseCommand, UsePersisted — the hook surface area has grown meaningfully and
   the React mental model transfers cleanly.

6. **ErrorBoundary exists.** Neither SwiftUI nor Compose has this. Duct's error
   boundary is a genuine differentiator for resilient UIs.

7. **FlexPanel is ambitious and useful.** A full Flexbox implementation on WinUI
   provides layout capabilities that WinUI itself doesn't have.

8. **Type safety over XAML.** No more binding errors, DataContext confusion, or
   resource-not-found runtime failures. The C# compiler catches real mistakes.
   Navigation and commanding extend this further — routes are types, commands
   are records, everything is compiler-checked.

9. **Observable interop.** UseObservable, UseObservableTree, UseObservableProperty,
   and UseCollection bridge cleanly to MVVM. Essential for incremental adoption
   in existing WinUI codebases.

10. **The localization system validates the framework's own abstractions.** The
    migration of LocaleProvider to `DuctContext<IntlAccessor?>` proves the context
    system works for real cross-cutting concerns. Navigation uses DuctContext for
    sharing handles. Commanding can use DuctContext for sharing commands. When
    multiple framework features build on the same primitive, that's good
    architecture.

### What prevents Duct from being production-ready

1. **The showcase apps don't use the framework's own features.** This is the
   most damning critique I can level. Navigation, commanding, context, memoization,
   persisted state — none of these are used in the Outlook clone, file manager,
   registry editor, or word puzzle game. Each feature has its own isolated demo
   app, but nobody has proven they compose in a complex real-world UI. Until the
   Outlook clone navigates with `UseNavigation`, uses `DuctContext` for session
   state, and surfaces Cut/Copy/Paste through `StandardCommand`, the framework's
   production-readiness is theoretical.

2. **Theming is still too thin.** XamlReader.Load per element per render, only 3
   brush properties, no custom branded theme resources, no guardrails against
   hard-coded colors. The ThemeRef system closed the P0 blocker but hasn't been
   deepened. A real app with brand colors and custom token sets will hit the
   ceiling quickly.

3. **Accessibility lacks the hard layers.** 16 modifiers and 12 UIA tests are
   solid annotations, but custom automation peers are blocked (components can't
   describe their own semantics), accessibility hooks are unbuilt, and there's no
   diagnostics or linting. The annotation layer is the easy part.

4. **Animation is still narrow.** Layout animations + springs + connected
   animations cover motion. But value-driven animation, enter/exit for individual
   elements, keyframes, and easing control are all missing. The 5-property
   implicit transition limit is a WinUI constraint the framework hasn't worked
   around.

5. **.Set() still carries too much weight.** Navigation and commanding reclaimed
   meaningful surface area. But gestures, pointer enter/exit, right-tap,
   double-tap, drag-and-drop, composition-layer effects, materials, and most
   windowing APIs still require `.Set()`. The abstraction is thicker than before
   but still not thick enough for a "you don't need to know WinUI" claim.

6. **Performance concerns accumulate.** Reflection-based ShouldUpdateWithProps
   (Section 2), boxing in context scope (Section 2), unbounded persisted state
   cache (Section 2), XamlReader.Load per themed element (Section 6),
   accelerator rebuild per render (Section 8), invisible Border/Grid wrappers
   adding layout cost (Sections 4, 7, 8). Each is individually minor. Together,
   they paint a picture of a framework that hasn't done a performance pass. No
   profiling tools exist to even measure these costs (Section 13).

7. **E2E test gaps for the newest features.** Navigation's E2E Appium tests are
   fixtures-only (not executed). Commanding has no integration tests. The unit
   test coverage is strong (117 navigation tests, comprehensive commanding
   tests), but nobody has verified these features work in a real WinUI window.
   For features this central, the gap between "unit tests pass" and "it works
   end-to-end" matters.

### The fundamental question (revisited)

Is Duct a *framework* or a *wrapper*?

The previous version of this review posed this question and concluded "moving
toward framework." That's still true, and the movement has accelerated. The
component model (context, hooks, memoization), navigation (type-safe routing,
developer-owned stack, transitions), and commanding (define-once commands,
standard commands, async lifecycle) are genuine framework-level abstractions.
A developer can now build a multi-page app with shared state, keyboard
shortcuts, and navigation transitions thinking entirely in Duct's model.

But the framework-vs-wrapper question has refined. Duct is no longer a wrapper
for the parts it covers. The problem is coverage: too many common scenarios
still require `.Set()`, and the features that exist haven't been stress-tested
in the framework's own showcase apps. The framework's ambition exceeds its
integration testing.

The competitive landscape has also shifted. When this review was first written,
Compose Navigation used strings and an opaque controller. Now Compose Navigation
3 (stable Nov 2025) uses developer-owned typed stacks — the same model Duct
chose. SwiftUI's NavigationStack is mature. React Router v7 has view transitions
and TanStack Router has full type inference. The window where "declarative
navigation for WinUI" was a novelty has closed; now it needs to be competitive
in quality, not just existence.

Duct's strongest position is commanding — it's genuinely ahead of the entire
industry. Its navigation is competitive. Its component model is solid. The
mid-tier features (theming, accessibility, animation) are functional with scope
limitations. The `.Set()` surface area is smaller but still too large.

### To become production-ready, Duct needs to:

1. **Adopt its own features in the showcase apps.** The Outlook clone should
   use UseNavigation, DuctContext, StandardCommand, and UsePersisted. This is
   the highest-leverage work — it proves composition and finds real bugs.
2. **Execute E2E tests for navigation and commanding.** The Appium fixtures
   exist. Run them. Automate them in CI.
3. **Do a performance pass.** Profile a real render cycle. Measure the cost of
   reflection-based ShouldUpdate, XamlReader.Load theming, accelerator rebuild,
   and wrapper elements. Fix what's expensive.
4. **Localize StandardCommand labels.** The framework's own commanding system
   should use the framework's own localization system. This is embarrassing in
   its absence.
5. **Finish the theming story.** Style caching for XamlReader.Load, more than 3
   brush properties, custom theme resource definitions.
6. **Add command routing to the focused view.** This is the missing piece that
   makes Cut/Copy/Paste work in multi-panel apps.

The trajectory is right. The foundation is solid. Navigation and commanding have
moved Duct from "component library with hooks" to "framework with application
architecture." But the gap between "features exist" and "features are production-
quality and compose in real apps" is where the remaining work lives. That gap is
narrower than before — and for the first time, there's a feature (commanding)
where Duct is genuinely ahead of the competition, not just catching up.

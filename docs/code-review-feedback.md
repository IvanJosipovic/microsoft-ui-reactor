# Duct / DuctD3 Code Review Feedback

**Reviewer:** Senior Platform Engineering Review  
**Date:** 2026-04-01  
**Scope:** All source files, tests, build configuration, and dependencies

---

## Status Field Instructions

Each feedback item has a **Status** field. The workflow is:

1. Reviewer sets all items to **`draft`** (or **`draft/optional`** if borderline)
2. Engineering manager reviews and updates each to one of:
   - **`approved`** -- engineer should complete this item
   - **`optional`** -- engineer may complete at their discretion
   - **`skip`** -- not worth doing
3. Engineer works through approved items, checking the box when done

---

## Table of Contents

1. [Critical Issues](#1-critical-issues)
2. [Core Framework (Duct/Core)](#2-core-framework-ductcore)
3. [Hosting & Elements (Duct/Hosting, Duct/Elements, Duct/Flex)](#3-hosting--elements)
4. [Monaco Editor (Duct/Monaco)](#4-monaco-editor)
5. [Markdown Parser (Duct/Markdown)](#5-markdown-parser)
6. [DuctD3 Library](#6-ductd3-library)
7. [Test Suite](#7-test-suite)
8. [Build Configuration & Dependencies](#8-build-configuration--dependencies)
9. [Sample Applications](#9-sample-applications)

---

## 1. Critical Issues

These should be addressed before any release.

### CR-001: Hardcoded debug log file path in production code
- [x] **Status:** `approved`
- **File:** `Duct/Hosting/DuctHost.cs:192`
- **Severity:** CRITICAL
- `File.AppendAllText(@"C:\temp\duct_perf_phases.log", ...)` writes to a hardcoded path on every perf report interval. This (a) fails on machines without `C:\temp`, (b) leaks perf data to disk in production, (c) adds unnecessary I/O. The `catch { }` silently swallows the failure.
- **Fix:** Remove entirely or gate behind `#if DEBUG`. Perf data should go through `IDuctLogger`. Put under #if DEBUG and go through the logger API.

### CR-002: Reconciler.Dispose() is empty -- component cleanups leak
- [x] **Status:** `approved`
- **File:** `Duct/Core/Reconciler.cs:701-703`
- **Severity:** CRITICAL
- When a `Reconciler` is disposed, component contexts with pending effect cleanups (event unsubscriptions, timer disposals) are never cleaned up. `_componentNodes` holds `ComponentNode` instances with pending cleanup delegates that will leak.
- **Fix:** Iterate `_componentNodes.Values`, call `RunCleanups()` on each node's context, clear the dictionary and pool.

### CR-003: No exception handling around Component.Render()
- [x] **Status:** `approved`
- **File:** `Duct/Core/Reconciler.cs:175-177` and `Reconciler.Mount.cs:1527-1528`
- **Severity:** CRITICAL
- If a component's `Render()` throws, it propagates uncaught through the reconciler, potentially leaving the tree in a partially-updated state. React has error boundaries for this. At minimum, catch around `Render()`, log the error, and return a fallback element so the rest of the tree survives. Create an error handler element (ErrorBoundary) which can be used to catch and isolate failures.

### CR-004: No exception handling around effect/cleanup execution
- [x] **Status:** `approved`
- **File:** `Duct/Core/RenderContext.cs:301-328`
- **Severity:** CRITICAL
- `FlushEffects` and `RunCleanups` call user-supplied functions without try/catch. A throwing effect prevents subsequent effects from running; a throwing cleanup prevents the new effect from starting. This leaves subscriptions dangling.
- **Fix:** Wrap each invocation in try/catch, log errors, continue processing remaining effects.

### CR-005: ForceSimulation uses static mutable Random -- thread-safety bug
- [x] **Status:** `approved`
- **File:** `DuctD3/Layout/ForceSimulation.cs:246-247`
- **Severity:** CRITICAL
- `private static readonly Random _rng = new(42)` is shared across all instances without synchronization. `System.Random` is not thread-safe. Two concurrent `ForceSimulation` instances will corrupt state.
- **Fix:** Use `Random.Shared` (.NET 6+) or a per-instance `Random`.

---

## 2. Core Framework (Duct/Core)

### CR-006: ItemsControlChildCollection.Replace lacks the RemoveAt+Insert safety fix
- [x] **Status:** `approved`
- **File:** `Duct/Core/ChildCollection.cs:94-96`
- **Severity:** HIGH
- `PanelChildCollection.Replace` uses `RemoveAt+Insert` to avoid a known WinUI COMException (documented in comment at line 54-58). But `ItemsControlChildCollection.Replace` uses direct indexer assignment `_items[index] = element`, which may suffer the same bug.
- **Fix:** Apply the same RemoveAt+Insert pattern.

### CR-007: Logging abstraction lacks IsEnabled and structured logging
- [x] **Status:** `approved`
- **File:** `Duct/Core/IDuctLogger.cs`
- **Severity:** HIGH
- **Questions for the engineer:**
  - There is no `bool IsEnabled(DuctLogLevel level)` method. Without this, callers always format the log message string (including string interpolation) even when the logger discards it. For hot paths in the reconciler, this adds measurable allocation overhead.
  - There is no structured logging overload. This makes it harder to integrate with backends like Application Insights or OpenTelemetry.
  - The `DuctLogLevel` enum duplicates `Microsoft.Extensions.Logging.LogLevel`. Have you considered accepting `ILogger` directly and providing adapters? This would make the OSS-to-internal switch trivial.
- **Fix:** Add `bool IsEnabled(DuctLogLevel level)` to the interface. Use it in hot-path callers. Consider a `Microsoft.Extensions.Logging` bridge.

### CR-008: UpdateListView doesn't refresh visible items when content changes with same count
- [x] **Status:** `approved`
- **File:** `Duct/Core/Reconciler.Update.cs:886-901`
- **Severity:** HIGH
- `UpdateListView` only sets a new `ItemsSource` when item count changes. If the list has the same count but different content, currently-visible items show stale content until scrolled out and back. Compare with `UpdateTemplatedListView` (lines 959-977) which correctly calls `RefreshRealizedContainers`.
- **Fix:** When items have changed (even with same count), force a refresh of realized containers.

### CR-009: ReconcileComponent silently returns if component node not found
- [x] **Status:** `approved`
- **File:** `Duct/Core/Reconciler.cs:163`
- **Severity:** MEDIUM
- `if (!_componentNodes.TryGetValue(control, out var node)) return;` -- if the node is missing due to a bug, the component silently stops updating with no indication.
- **Fix:** Log a warning so developers can diagnose "frozen" components.

### CR-010: MountImage does not handle invalid URIs
- [x] **Status:** `approved`
- **File:** `Duct/Core/Reconciler.Mount.cs:523`
- **Severity:** MEDIUM
- `new Uri(img.Source, UriKind.RelativeOrAbsolute)` throws `UriFormatException` on malformed input. The Hyperlink mount (line 168) handles this with try/catch, but Image does not.
- **Fix:** Wrap in try/catch, return placeholder or empty image.

### CR-011: FindItemByOldIndex is O(n) per moved item -- O(n^2) worst case
- [x] **Status:** `approved`
- **File:** `Duct/Core/ChildReconciler.cs:303-322`
- **Severity:** MEDIUM
- For each moved item in keyed reconciliation, `FindItemByOldIndex` does a linear scan. For large lists (hundreds of items), this is O(n^2).
- **Fix:** Build a temporary lookup map from key to current panel index.

### CR-012: ForceDetach catches all exceptions including OOM/SOE
- [x] **Status:** `approved`
- **File:** `Duct/Core/ElementPool.cs:58-62`
- **Severity:** MEDIUM
- The bare `catch` block catches ALL exceptions and returns `true`, allowing pooling. This swallows `OutOfMemoryException`, `StackOverflowException`, etc.
- **Fix:** Narrow to `catch (InvalidOperationException)` or `catch (Exception) when (e is not OutOfMemoryException)`.

### CR-013: Component mount creates a Border wrapper per component
- [ ] **Status:** `draft/optional`
- **File:** `Duct/Core/Reconciler.Mount.cs:1531-1538`
- **Severity:** MEDIUM (performance)
- Every component mount creates a `Border` wrapper as an identity anchor in `_componentNodes`. For deeply nested trees, this adds a WinUI element per component with layout cost and COM overhead.
- **Question:** Is there a lighter-weight identity mechanism?

---

## 3. Hosting & Elements

### CR-014: DuctApp.ActiveHost is a mutable public field
- [x] **Status:** `approved`
- **File:** `Duct/Hosting/DuctApp.cs:28`
- **Severity:** HIGH
- `public static DuctHost? ActiveHost` is a bare public field, not a property. Any consumer can overwrite it. If multiple hosts are created, the last one wins silently.
- **Fix:** `public static DuctHost? ActiveHost { get; internal set; }`

### CR-015: DuctApp.Options written on caller thread, read on UI thread -- no memory barrier
- [x] **Status:** `approved`
- **File:** `Duct/Hosting/DuctApp.cs:27, 44-57`
- **Severity:** HIGH
- `Options` is written in `Run()` on the calling thread and read inside `Application.Start` on the UI thread. No `volatile`/lock guarantees the write is visible.
- **Fix:** Use `Volatile.Write`/`Volatile.Read` or pass options through a thread-safe mechanism.

### CR-016: DuctHostControl.Render() lacks error fallback
- [x] **Status:** `approved`
- **File:** `Duct/Hosting/DuctHostControl.cs:142-188`
- **Severity:** HIGH
- `DuctHost.Render()` catches exceptions and shows error fallback UI. `DuctHostControl.Render()` catches, logs, then re-throws, crashing the app. No error UI fallback exists. --> see the comment around an ErrorBoundary element
- **Fix:** Add try/catch with error fallback matching `DuctHost.ShowErrorFallback()`.

### CR-017: BrushHelper caches SolidColorBrush in ConcurrentDictionary -- thread affinity risk
- [x] **Status:** `approved`
- **File:** `Duct/Elements/BrushHelper.cs:13, 23`
- **Severity:** HIGH
- `SolidColorBrush` is a `DependencyObject` with thread affinity. The `ConcurrentDictionary` signals thread-safety intent but a brush created on one thread will throw `COMException` if used on another.
- **Fix:** Cache `Windows.UI.Color` values instead and create new `SolidColorBrush` per use (they're cheap), or switch to plain `Dictionary` with documented UI-thread constraint.

### CR-018: P/Invoke SetProcessDpiAwarenessContext return value ignored
- [x] **Status:** `approved`
- **File:** `Duct/Hosting/DuctApp.cs:42, 62`
- **Severity:** MEDIUM
- Declared with `SetLastError = true` but return value never checked. If DPI awareness was already set by manifest, the call fails silently.
- **Fix:** Check return value, log warning on failure.

### CR-019: FlexPanel._nodeCache never cleaned up on disposal
- [x] **Status:** `approved`
- **File:** `Duct/Flex/FlexPanel.cs:19`
- **Severity:** MEDIUM
- `_nodeCache` and `_rootNode` are never cleared when the FlexPanel is removed from the visual tree. If `YogaNode` holds resources, they leak.
- **Fix:** Override `OnDisconnectedFromVisualTree` or implement cleanup.

### CR-020: ForEach wraps results in VStack unconditionally
- [x] **Status:** `approved`
- **File:** `Duct/Elements/Dsl.cs:332, 339`
- **Severity:** MEDIUM (API design)
- `ForEach` always wraps mapped elements in a `VStack`. Callers wanting horizontal or no container must avoid `ForEach`. In React, `map()` returns a flat array.
- **Question:** Should `ForEach` return a fragment/group that doesn't introduce layout? -> yes, should not force a layout

### CR-021: FilterChildren allocates on every DSL call
- [x] **Status:** `approved`
- **File:** `Duct/Elements/Dsl.cs:548-549`
- **Severity:** LOW (performance)
- `.Where().Select().ToArray()` runs on every `VStack`/`HStack`/`Canvas` even when there are no nulls (common case).
- **Fix:** Fast-path: return input array directly when no nulls exist.

---

## 4. Monaco Editor

### CR-022: UpdateOptions double-serializes JSON
- [x] **Status:** `approved`
- **File:** `Duct/Monaco/MonacoEditor.cs:341`
- **Severity:** HIGH (functional bug)
- `UpdateOptions(string optionsJson)` wraps input with `JsonSerializer.Serialize(optionsJson)`, producing double-escaped JSON. The JS function receives a string literal instead of an object.
- **Fix:** Inject `optionsJson` directly (with validation that it's valid JSON).

### CR-023: WebView2 DevTools enabled unconditionally
- [x] **Status:** `approved`
- **File:** `Duct/Monaco/MonacoEditor.cs:161`
- **Severity:** HIGH (security)
- `AreDevToolsEnabled = true` is hardcoded. End users can open F12 and inspect/modify content.
- **Fix:** Gate behind `#if DEBUG` or make configurable.

### CR-024: Script injection risk via double/string interpolation into ExecuteScriptAsync
- [x] **Status:** `approved`
- **File:** `Duct/Monaco/MonacoEditor.cs:277` and multiple `On*Changed` callbacks
- **Severity:** MEDIUM
- Values are interpolated directly into JS strings. While `double` is generally safe, `NaN`/`Infinity` format as non-numeric strings in some cultures. The pattern is fragile and sets a dangerous precedent.
- **Fix:** Use `JsonSerializer.Serialize` for all injected values, or use `PostWebMessageAsJson`.

### CR-025: OnWebMessageReceived does not validate message structure
- [x] **Status:** `approved`
- **File:** `Duct/Monaco/MonacoEditor.cs:192-225`
- **Severity:** MEDIUM
- Assumes JSON has `type`, `value`, `isFlush` properties. Malformed data throws `KeyNotFoundException` and crashes.
- **Fix:** Use `TryGetProperty` with graceful fallback.

### CR-026: Fire-and-forget async throughout MonacoEditor
- [x] **Status:** `approved`
- **File:** `Duct/Monaco/MonacoEditor.cs:120, 207, 298-299`
- **Severity:** MEDIUM
- Multiple `_ = command()` patterns silently swallow async exceptions.
- **Fix:** Add exception handling inside async lambdas.

---

## 5. Markdown Parser

### CR-027: Numeric entity overflow can produce invalid codepoints
- [x] **Status:** `approved`
- **File:** `Duct/Markdown/Md4cHtml.cs:196-207`
- **Severity:** HIGH (security)
- `codepoint = 10 * codepoint + ...` in a `uint` loop without overflow checking. A long digit string (`&#99999999999999;`) silently wraps around, bypassing the `> 0x10ffff` guard and producing an arbitrary codepoint.
- **Fix:** Add `if (codepoint > 0x10FFFF) { codepoint = 0xFFFD; break; }` inside the parsing loops.

### CR-028: stackalloc inside loop (confirmed build warning)
- [x] **Status:** `approved`
- **File:** `Duct/Markdown/Md4cHtml.cs:132`
- **Severity:** HIGH
- `stackalloc byte[4]` inside a `for` loop. For long CJK URLs, this accumulates stack allocations.
- **Fix:** Move `Span<byte> utf8 = stackalloc byte[4]` before the loop.

### CR-029: MarkdownBuilder bold/italic uses boolean flags instead of depth counters
- [x] **Status:** `approved`
- **File:** `Duct/Markdown/MarkdownBuilder.cs:79-81, 586-631`
- **Severity:** MEDIUM
- Nested bold (`**outer **inner** outer**`) incorrectly turns off bold on the first `LeaveSpan(Strong)`.
- **Fix:** Replace `bool _isBold` with `int _boldDepth`, increment/decrement on enter/leave.

### CR-030: No URI scheme validation in MarkdownBuilder -- javascript: XSS risk
- [x] **Status:** `approved`
- **File:** `Duct/Markdown/MarkdownBuilder.cs:601, 610`
- **Severity:** MEDIUM (security)
- `Uri.TryCreate(href, UriKind.RelativeOrAbsolute)` accepts `javascript:alert(1)`. If rendered in a WebView2 context, this is an XSS vector.
- **Fix:** Validate URI scheme is `http`, `https`, or `mailto`. Reject `javascript:`, `data:`, `vbscript:`.

### CR-031: No nesting depth limit for block quotes/list items
- [x] **Status:** `approved`
- **File:** `Duct/Markdown/Md4cParser.Block.cs`, `PushContainer`
- **Severity:** MEDIUM (DoS)
- Thousands of nested `>` blockquotes allocate unbounded memory and create deep call stacks.
- **Fix:** Add `const int MAX_NESTING_DEPTH = 100` and reject deeper nesting.

### CR-032: schemeMap allocated on every colon character in CollectMarks
- [x] **Status:** `approved`
- **File:** `Duct/Markdown/Md4cParser.Inline.cs:1662-1667`
- **Severity:** LOW (performance)
- **Fix:** Hoist to `private static readonly` field.

---

## 6. DuctD3 Library

### CR-033: D3Color.Parse missing rgb()/hsl() support despite doc comment
- [x] **Status:** `approved`
- **File:** `DuctD3/Color/D3Color.cs:50-75`
- **Severity:** HIGH
- Doc comment says "Parses hex, rgb(), hsl(), and named colors" but only hex and named are implemented. `"rgb(255,0,0)"` silently returns black.
- **Fix:** Implement rgb()/hsl() parsing or correct the documentation.

### CR-034: Polygon methods crash on empty input
- [x] **Status:** `approved`
- **File:** `DuctD3/Polygon/Polygon.cs:20, 38, 60, 81`
- **Severity:** HIGH
- `Area()`, `Centroid()`, `Contains()`, `Length()` access `polygon[n-1]` without checking `n == 0`.
- **Fix:** Add `if (n == 0)` guards returning 0 / (0,0) / false / 0.

### CR-035: Polygon.Centroid division by zero when area is zero
- [x] **Status:** `approved`
- **File:** `DuctD3/Polygon/Polygon.cs:49`
- **Severity:** HIGH
- When all points are collinear, `k == 0`, producing NaN centroid.
- **Fix:** Return average of points when `k == 0`.

### CR-036: CultureInfo missing in Fmt() label formatting
- [x] **Status:** `approved`
- **File:** `DuctD3/Charts/ChartDsl.cs:159` and `DuctD3/Charts/D3Dsl.cs:43-46`
- **Severity:** HIGH
- `Fmt()` uses default `ToString()` without `CultureInfo.InvariantCulture`. In German/French locales, axis labels produce `"1,5k"` instead of `"1.5k"`.
- **Fix:** Pass `CultureInfo.InvariantCulture` to all numeric formatting.

### CR-037: Delaunay triangulation is not a real Delaunay implementation
- [ ] **Status:** `skip`
- **File:** `DuctD3/Voronoi/Delaunay.cs:97-121`
- **Severity:** HIGH (correctness)
- The comment says "Simple fan triangulation" and "for production use, Bowyer-Watson would be better." The current code produces overlapping/invalid triangles. Any consumer of Delaunay/Voronoi gets wrong results for non-trivial point sets.
- **Question:** Is Voronoi functionality advertised as production-ready? If so, Bowyer-Watson must be implemented.

### CR-038: BisectCenter crashes on empty array
- [x] **Status:** `approved`
- **File:** `DuctD3/Array/Bisect.cs:53-54`
- **Severity:** HIGH
- Empty array causes `array[0]` access after `BisectLeft` returns 0.
- **Fix:** Guard `if (array.Length == 0) return 0;`.

### CR-039: Treemap squarify is not actually implemented
- [ ] **Status:** `skip`
- **File:** `DuctD3/Layout/Treemap.cs:142-174`
- **Severity:** MEDIUM (correctness)
- `TileSquarify` computes aspect ratios but never lays out squarified strips. Falls back to slice/dice. Users selecting `TreemapTiling.Squarify` don't get squarified rectangles.
- **Fix:** Implement the actual squarify algorithm.

### CR-040: Voronoi ClipToBounds is point-clamping, not polygon clipping
- [ ] **Status:** `skip`
- **File:** `DuctD3/Voronoi/Delaunay.cs:320-329`
- **Severity:** MEDIUM (correctness)
- True cell clipping requires Sutherland-Hodgman. Current code clamps each vertex independently, distorting edges.
- **Fix:** Implement proper polygon clipping.

### CR-041: Range.Range infinite loop when step=0
- [x] **Status:** `approved`
- **File:** `DuctD3/Array/Range.cs:17`
- **Severity:** MEDIUM
- `step = 0` causes undefined behavior in `(int)double.PositiveInfinity`.
- **Fix:** Add `if (step == 0) return [];`.

### CR-042: Contour LerpX division by zero
- [x] **Status:** `approved`
- **File:** `DuctD3/Contour/Contour.cs:79`
- **Severity:** MEDIUM
- `(threshold - v0) / (v1 - v0)` divides by zero when `v0 == v1`.
- **Fix:** Guard against `v0 == v1`, returning 0.5.

### CR-043: ForceSimulation.ApplyCenterForce divide by zero with empty nodes
- [x] **Status:** `approved`
- **File:** `DuctD3/Layout/ForceSimulation.cs:215-216`
- **Severity:** MEDIUM
- `sx / _nodes.Count` throws when `Count == 0`. The public API allows direct calls.
- **Fix:** Add `if (_nodes.Count == 0) return;`.

### CR-044: BandScale.Rescale reverse formula appears incorrect
- [x] **Status:** `approved`
- **File:** `DuctD3/Scale/BandScale.cs:132-136`
- **Severity:** MEDIUM
- The reverse branch has `+ r0` that appears to be a bug vs D3's `ordinal.js`. Formula `_start = r0 + r1 - _start - _bandwidth` would make more sense for mirroring.
- **Question:** Please verify this against the D3 source.

### CR-045: AreaGenerator/RadialAreaGenerator ignores _curve field
- [x] **Status:** `approved`
- **File:** `DuctD3/Shape/Area.cs:16` and `DuctD3/Shape/Radial.cs:76`
- **Severity:** LOW
- `SetCurve()` accepts a value but `Generate()` never uses it.
- **Fix:** Either implement curve support or remove the field/setter.

### CR-046: OrdinalScale.Map has implicit domain growth side effect
- [x] **Status:** `approved`
- **File:** `DuctD3/Scale/OrdinalScale.cs:26-34`
- **Severity:** LOW (API surprise)
- Mapping an unknown key silently adds it to the domain. Matches D3 but surprising in C#.
- **Fix:** Document this behavior prominently.

---

## 7. Test Suite

### CR-047: Three buggy tests that always pass
- [x] **Status:** `approved`
- **Files:**
  - `tests/Duct.Tests/TypeRegistryUnmountTests.cs:132` -- `Assert.True(unmountInvoked || true)` always passes
  - `tests/DuctD3.Tests/ContourTests.cs:47` -- `Assert.True(result[0].Coordinates.Count >= 0)` always passes
  - `tests/Duct.Tests/ElementPoolTests.cs:100-114` -- test body has no assertions
- **Fix:** Fix the assertion logic. The TypeRegistryUnmount test should be `Assert.True(unmountInvoked)`.

### CR-048: ElementPool has zero functional test coverage
- [ ] **Status:** `approved`
- **File:** `tests/Duct.Tests/ElementPoolTests.cs`
- **Severity:** HIGH (gap)
- No test verifies rent/return behavior, capacity limits, type filtering, or exhaustion. All tests just assert `TryRent` returns null on empty pool.
- **Fix:** Add functional tests using WinUI thread (or mock the pool internals).

### CR-049: No reconciler mount/update behavior tests
- [ ] **Status:** `approved`
- **File:** `tests/Duct.Tests/Reconciler*Tests.cs`
- **Severity:** HIGH (gap)
- No unit test mounts a TextElement and verifies a TextBlock is created, updates text and verifies it changed, or unmounts and verifies removal. All tests verify preconditions (CanUpdate, record equality) but not actual DOM mutations.
- **Fix:** Add targeted mount/update/unmount tests via SelfTestRunner pattern for TextElement, ButtonElement, StackElement, ComponentElement.

### CR-050: DuctD3 tests lack D3.js reference values
- [ ] **Status:** `approved`
- **Files:** `tests/DuctD3.Tests/CurveTests.cs`, `ChordTests.cs`, `SymbolTests.cs`, `RadialTests.cs`
- **Severity:** HIGH (gap)
- Most tests only assert `Assert.NotNull(path)` or `Assert.Contains("C", path)`. No actual coordinate verification against D3.js output. LinearScaleTests and PathBuilderTests show the correct pattern with exact value comparisons.
- **Fix:** Run equivalent D3.js code in Node.js, capture output, assert exact match.

### CR-051: No error handling tests
- [ ] **Status:** `approved`
- **Severity:** MEDIUM (gap)
- Missing: component Render() throwing, unregistered element type mount, MaxRenderIterations guard behavior, DuctHost/DuctHostControl error paths, XamlInterop with invalid page types.
- **Fix:** Add targeted error path tests.

### CR-052: No logging tests
- [ ] **Status:** `approved`
- **Severity:** MEDIUM (gap)
- `IDuctLogger`, `NullDuctLogger`, `DebugDuctLogger` have zero coverage. No test verifies reconciler operations are logged or that log output appears during errors.

### CR-053: Missing boundary/negative input tests for DuctD3
- [ ] **Status:** `approved`
- **Severity:** MEDIUM (gap)
- Missing: `LinearScale` with Infinity domain, `PieGenerator` with single point, `Delaunay.From` with collinear points, `TreemapLayout` with zero-value nodes, `SankeyLayout` with cycles, `ContourGenerator` with NaN grid, `BinGenerator` with identical values.

---

## 8. Build Configuration & Dependencies

### CR-054: WindowsAppSDK experimental version
- [ ] **Status:** `skip`
- **File:** `Directory.Build.props:9`
- **Severity:** HIGH
- Using `2.0.0-experimental6`. Experimental packages have breaking changes, are unsupported for production, and caused build errors already.
- **Question:** Are specific 2.0 preview features required? If not, pin to stable 1.6.x.

### CR-055: Duct.Cli scaffolding generates stale versions
- [x] **Status:** `approved`
- **File:** `Duct.Cli/Program.cs:150-164`
- **Severity:** MEDIUM
- Template hardcodes `net8.0` (solution uses `net9.0`) and `experimental4` (solution uses `experimental6`).
- **Fix:** Read versions from Directory.Build.props or emit a props import.

### CR-056: No Central Package Management
- [ ] **Status:** `approved`
- **Files:** All `.csproj` files
- **Severity:** MEDIUM
- Package versions specified individually. Test framework version skew already observable (xunit 2.7 vs 2.9, MSTest mixed in).
- **Fix:** Create `Directory.Packages.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.

### CR-057: No TreatWarningsAsErrors or code analysis
- [ ] **Status:** `approved`
- **File:** `Directory.Build.props`
- **Severity:** MEDIUM
- No `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` or `<AnalysisLevel>latest-recommended</AnalysisLevel>`.
- **Fix:** Add to Directory.Build.props.

### CR-058: No global.json, nuget.config, or .editorconfig
- [ ] **Status:** `approved`
- **Severity:** LOW
- Without `global.json`, builds use whatever SDK is installed. Without `.editorconfig`, code style diverges.
- **Fix:** Add `global.json` pinning .NET 9.x SDK. Add `.editorconfig` for style consistency.

### CR-059: AOT compatibility inconsistency
- [x] **Status:** `approved`
- **File:** `Duct/Duct.csproj:11` vs `DuctD3/DuctD3.csproj`
- **Severity:** LOW
- `Duct.csproj` declares `IsAotCompatible=false` but `DuctD3.csproj` declares `IsAotCompatible=true` despite depending on Duct. `Duct.MiniTest` sets `PublishAot=true` while referencing non-AOT Duct.
- **Fix:** Reconcile the AOT story. --> we can remove AOT support, breaks too many things

---

## 9. Sample Applications

### CR-060: RegistryService swallows all exceptions silently
- [x] **Status:** `approved`
- **File:** `samples/apps/regedit/Services/RegistryService.cs`, multiple methods
- **Severity:** MEDIUM
- Nearly every method has `catch { return false/null/[]; }`. Users get no feedback when operations fail due to `SecurityException` or `UnauthorizedAccessException`.
- **Fix:** Catch specific types, surface error messages to UI.

### CR-061: DeleteKeyAsync calls DeleteSubKeyTree without confirmation
- [ ] **Status:** `approved`
- **File:** `samples/apps/regedit/Services/RegistryService.cs:173`
- **Severity:** MEDIUM
- Recursively deletes entire registry subtree. Single accidental click could be destructive.
- **Fix:** Add confirmation dialog in the UI layer.

### CR-062: FileWatcherService CancellationTokenSource leak
- [x] **Status:** `approved`
- **File:** `samples/apps/ductfiles/Services/FileWatcherService.cs:35-37`
- **Severity:** LOW
- Previous CTS is never disposed when creating new one on each filesystem change.
- **Fix:** `_debounceCts?.Cancel(); _debounceCts?.Dispose(); _debounceCts = new CTS();`

---

## Summary

| Priority | Count | Key Themes |
|----------|-------|------------|
| CRITICAL | 5 | Leaked cleanups, unhandled Render() exceptions, debug file I/O, thread-unsafe static Random |
| HIGH | 12 | Missing error fallbacks, thread-affinity bugs, broken D3 APIs, missing rgb() parser, empty-input crashes |
| MEDIUM | 20 | Correctness gaps (squarify, Voronoi, ListView refresh), security (DevTools, XSS), logging, performance |
| LOW/Optional | 12 | API design, performance micro-opts, documentation, build config cleanup |
| Test Gaps | 7 | Pool coverage, reconciler behavior, D3 reference values, error paths, logging |

**Recommended priority order:**
1. Fix the 5 critical items (CR-001 through CR-005)
2. Address test bugs (CR-047) and high-severity gaps (CR-048, CR-049, CR-050)
3. Work through HIGH items focusing on security (CR-023) and correctness (CR-033, CR-034, CR-037)
4. Adopt Central Package Management (CR-056) and TreatWarningsAsErrors (CR-057)
5. Address MEDIUM items by module

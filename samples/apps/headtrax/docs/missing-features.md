# HeadTrax – Missing Framework Features

Features needed by this sample app that don't yet exist in the Duct DSL.

---

## `Spacer(double pixels)`

**What it does:** A zero-content element that occupies a fixed amount of space along the parent's main axis. Used to insert explicit gaps between siblings without resorting to margin on either neighbor.

**Expected API:**
```csharp
FlexRow(
    Text("Left"),
    Spacer(16),
    Text("Right")
)
```

**Behavior:** Renders as an empty element with a fixed width (in a row) or height (in a column) equal to the given pixel value. Does not grow or shrink. Equivalent to a `<div>` with only a `flex-basis` and no `flex-grow`.

**Workaround (used):** `FlexColumn().Width(16)` — an empty flex child with a fixed width. Used in `Toolbar.cs` for non-uniform gaps between elements.

**Note:** The `FlexElement` already supports `ColumnGap`/`RowGap` on the container level via Yoga, which handles uniform spacing. `Spacer` is for one-off non-uniform gaps.

---

## `FlexSpacer()`

**What it does:** A flexible empty element that absorbs all remaining space along the parent's main axis. The flex layout equivalent of "push everything after me to the end."

**Expected API:**
```csharp
FlexRow(
    Text("Left-aligned"),
    FlexSpacer(),
    Text("Right-aligned")
)
```

**Behavior:** Renders as an empty element with `flex-grow: 1`. When placed between items in a `FlexRow` or `FlexColumn`, it pushes subsequent siblings to the opposite edge. Multiple `FlexSpacer`s divide remaining space equally.

**Workaround (used):** `FlexColumn().Flex(grow: 1)` — an empty flex child that absorbs remaining space. Used in `Toolbar.cs` and `EmployeeGrid.cs`.

---

## `SegmentedControl(segments, selectedValue, onChanged)`

**What it does:** A horizontal group of mutually exclusive toggle buttons — like a radio group rendered as a connected button strip. Common for switching between 2–5 modes (e.g., "SQLite Direct" vs "GraphQL API").

**Expected API:**
```csharp
// Segment is a simple record: Segment(string value, string label)
SegmentedControl(
    [
        Segment("sqlite", "SQLite Direct"),
        Segment("graphql", "GraphQL API"),
    ],
    selectedValue,    // current value (string)
    onChanged         // Action<string> callback with the new value
)
```

**Behavior:** Renders as a horizontal strip of buttons with connected borders (first button has left radius, last has right radius, middle buttons have no radius). Exactly one segment is selected at a time. Clicking an unselected segment calls `onChanged` with its value. The selected segment gets a visually distinct style (filled background or accent color).

**Workaround (used):** Two `ToggleButton`s with manual state in `Toolbar.cs`. Each button checks `mode == "sqlite"` or `mode == "graphql"` for its `isChecked` prop and calls `OnModeChanged` on toggle.

**Design notes:** WinUI doesn't have a native segmented control. Implementation options:
1. A custom `Component` that renders styled `ToggleButton`s in a `FlexRow` with connected corner radii via resource overrides.
2. Map to a horizontal `RadioButtons` with custom item template.
3. Build on `ItemsRepeater` with a custom layout.

---

## ~~DataGrid: incremental / demand-paged data fetching~~ (FIXED)

**Fixed:** The DataGrid now uses `DataPageCache<T>` for block-based incremental loading. When the data source supports server-side sort/filter/search (declared via `DataSourceCapabilities`), `LoadDataAsync` fetches only the first block (~50 items) to get `TotalCount`, and additional blocks are loaded on demand as the user scrolls into unloaded regions. For sources that don't support server-side operations, the legacy eager path is retained as a fallback.

**What changed:**
- `DataGridState<T>` now creates a `DataPageCache<T>` internally and uses `GetItemAt(index)` / `GetRowKeyAt(index)` for cache-aware item access
- `DataGridComponent` uses `state.ItemCount` (total from cache) for VirtualList itemCount and renders placeholder rows for unloaded blocks
- `BlockLoaded` events trigger `StateChanged` → component re-render → visible rows update
- Edit mutations go through a local overlay (`_mutations` dictionary) that sits on top of the cache

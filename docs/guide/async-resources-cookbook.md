# Async Resources — Cookbook

Task-oriented recipes for Reactor's async hooks. For the full contract and state
machines see [`docs/reference/async-system.md`](../reference/async-system.md);
for the design rationale see [`docs/specs/020-async-resources-design.md`](../specs/020-async-resources-design.md).

- [Porting `UseEffect + UseState` → `UseResource`](#porting-useeffect--usestate--useresource)
- [Infinite scroll with `UseInfiniteResource` + `LazyVStack`](#infinite-scroll)
- [Migrating from a `DataPageCache`-style cache](#migrating-from-a-datapagecache-style-cache)
- [Optimistic writes with `UseMutation`](#optimistic-writes)
- [Pending fallbacks with `Pending`](#pending-fallbacks)

---

## Porting `UseEffect + UseState` → `UseResource`

The old pattern: one `UseState` for the data, one for a loading flag, one for the
error, and a `UseEffect` with deps that kicks off the fetch and sets all three.
It's correct in the happy path but pushes lifecycle concerns onto every caller:
cancelling stale fetches on deps-change, dropping late results after unmount,
suppressing `OperationCanceledException` noise, and picking a sensible key for
sharing the result across siblings.

```csharp
// Before — 4 hooks, manual cancellation plumbing, no cache
public override Element Render()
{
    var (data, setData) = UseState<User?>(null);
    var (error, setError) = UseState<Exception?>(null);
    var (loading, setLoading) = UseState(true);

    UseEffect(() =>
    {
        var cts = new CancellationTokenSource();
        setLoading(true);
        _ = Task.Run(async () =>
        {
            try
            {
                var u = await _api.GetUserAsync(UserId, cts.Token);
                if (!cts.IsCancellationRequested)
                {
                    setData(u);
                    setError(null);
                    setLoading(false);
                }
            }
            catch (OperationCanceledException) { /* swallow */ }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                {
                    setError(ex);
                    setLoading(false);
                }
            }
        });
        return () => cts.Cancel();
    }, UserId);

    if (loading) return Text("Loading…");
    if (error is not null) return Text($"Error: {error.Message}");
    return Text(data?.Name ?? "(none)");
}
```

```csharp
// After — one hook, cancellation + caching are free
public override Element Render()
{
    var user = UseResource(
        ct => _api.GetUserAsync(UserId, ct),
        deps: new object[] { UserId });

    return user.Match(
        loading: () => Text("Loading…"),
        data:    u  => Text(u.Name),
        error:   ex => Text($"Error: {ex.Message}"));
}
```

What you get:

- **Cancellation.** Deps change and unmount fire `ct` automatically. The hook
  drops stale results — you don't have to check `cts.IsCancellationRequested`.
- **Caching.** Siblings with the same `deps` share one in-flight fetch and one
  cache entry (use explicit `CacheKey` in `ResourceOptions` for cross-tree
  sharing). See §9 of the reference.
- **Stale-while-revalidate.** Past `StaleTime` the hook returns
  `AsyncValue.Reloading(previous)` — the `Match` `reloading` arm falls back to
  `data` unless you override it, so the UI keeps the old value visible during
  the refetch.
- **No Loading flash for sync completions.** `Task.FromResult` lands in `Data`
  on the same render — no `Loading → Data` flicker.

Common pitfalls:

- **Non-idempotent fetchers.** `UseResource` may retry and refetch; only use it
  for reads. For writes see [`UseMutation`](#optimistic-writes).
- **`deps` must be a value-comparable array.** A new `List<T>` or lambda every
  render will thrash — either memoize, use a scalar key, or let the analyzer
  (`REACTOR_HOOKS_004`, WIP) flag it.
- **Don't capture mutable state in the fetcher without listing it in `deps`.**
  The fetcher closes over `UserId` — if `UserId` changes, `deps` must change.

---

## Infinite scroll

`UseInfiniteResource` models a cursor-paginated read. Combine it with a
`LazyVStack` or `VirtualList` and drive fetches from `ItemAt(i)` — that's the
pull-model: the virtualizer asks for item *i*, and the hook schedules whichever
page covers it.

```csharp
var commits = UseInfiniteResource<Commit, string>(
    fetchPage: async (cursor, ct) =>
    {
        var page = await _api.GetCommitsAsync(RepoId, afterCursor: cursor, ct);
        return new Page<Commit, string>(page.Items, page.NextCursor, page.Total);
    },
    deps: new object[] { RepoId });

return VirtualList(
    itemCount:    commits.TotalCount ?? commits.Items.Count,
    renderItem:   i =>
    {
        // ItemAt schedules the covering page on demand; returns null for
        // in-flight / not-yet-requested slots. Render a shimmer in that case.
        var commit = commits.ItemAt(i);
        return commit is null
            ? SkeletonRow()
            : CommitRow(commit);
    },
    getItemKey: i => commits.ItemAt(i)?.Sha ?? i.ToString());
```

When the user scrolls ahead, the `ItemAt` call for a new slot triggers a
coalesced fetch. `commits.LoadState` exposes `Loading` / `Idle` / `EndOfList` /
`Error` for footers (e.g. a spinner row or retry button).

- **Prefetching a range.** If you know a visible-range (from `VirtualList`'s
  `onVisibleRangeChanged`), call `commits.EnsureRange(first, last)` once per
  scroll tick — faster than relying on `ItemAt` to fault in pages row-by-row.
- **Refresh on pull-to-refresh.** `commits.Refresh()` cancels in-flight, clears
  the page table, and refetches page 0 — returns to `Loading` from `Items.Count == 0`.
- **Retry on failure.** `commits.Retry()` re-requests the failed page when
  `LoadState is Error`.
- **LRU cap.** Pass `InfiniteResourceOptions.MaxLoadedPages = 20` to bound the
  working set on very long lists.

---

## Migrating from a `DataPageCache`-style cache

If you've rolled your own block-cache over `IDataSource<T>` — the pre-Phase-3
`DataPageCache<T>` is the canonical example — the replacement path is:

1. **Swap the cache for a hook call.** Replace the ambient cache field with
   `var resource = ctx.UseDataSource(source, request, options);`. The
   `DataSourceResourceExtensions.UseDataSource` adapter bridges `IDataSource<T>`
   directly onto `UseInfiniteResource`, using `request.ContinuationToken` as the
   cursor.
2. **Read from `resource.Items[i]`.** The sparse `IReadOnlyList<T?>` is the
   flat view — null slots are placeholders. This replaces `cache.PeekItem(i)`
   plus the `LoadingBlock` sentinel.
3. **Drop `BlockLoaded` subscriptions.** The hook's own `UseReducer(threadSafe: true)`
   drives a re-render whenever the resource state changes, so a component
   consuming the resource re-renders automatically — no event plumbing.
4. **Route prefetch through `EnsureRange`.** If you had a `RequestBlock(i)`
   call inside an `onVisibleRangeChanged` callback, swap it for
   `resource.EnsureRange(firstRow, lastRow)`. The hook dedups against
   in-flight fetches for you.
5. **Request changes = deps change.** Sort/filter/search updates flow through
   `request` into the hook's `deps`. The hook cancels in-flight fetches,
   unsubscribes the old cache keys, and restarts on page 0.

For a worked example, see `src/Reactor/Controls/DataGrid/DataGridComponent.cs`
under `ReactorFeatureFlags.UseHookBasedPaging = true` — that's the DataGrid
running over the hook rather than its legacy `DataPageCache` path.

Gotchas worth calling out:

- **Mutation overlay still lives on the caller.** The hook's page table is
  server-sourced and immutable per-page. Optimistic edits should be kept in a
  caller-owned overlay (Dictionary<int, T>) that takes precedence over
  `resource.Items[i]` at read time — `DataGridState<T>` uses this pattern.
- **Cursor paging is inherently serial.** `UseInfiniteResource` requires page
  *N-1* to be loaded before requesting page *N*, because the cursor lives in
  the previous page's payload. If your source supports offset-based paging and
  parallel fetches, you can pass an offset as the cursor and
  `InfiniteResourceOptions.PageSize` to size each batch — the hook won't
  parallelize for you.

---

## Optimistic writes

`UseMutation` separates reads from writes. Register it once per component; call
`mutation.RunAsync(input)` from click handlers or effects. The optimistic
callback fires synchronously so the UI never flashes a stale value waiting for
the server:

```csharp
var mutation = UseMutation<TodoInput, Todo>(
    mutator: (input, ct) => _api.AddTodoAsync(input, ct),
    options: new MutationOptions<TodoInput, Todo>(
        OnOptimistic: input => _store.InsertOptimistic(new Todo(input.Title, tempId: true)),
        OnSuccess:    (todo, _) => _store.ReplaceTemp(todo),
        OnError:      (ex, input) => _store.RemoveOptimistic(input.Title),
        InvalidateKeys: ["todos/list"]));

return Button("Add", () => mutation.RunAsync(new TodoInput("Buy milk")));
```

- `InvalidateKeys` triggers `cache.Invalidate(key)` on success (not on error)
  — any sibling `UseResource` subscribed to that key observes the
  invalidation and refetches.
- Concurrent calls each get their own cancellation token; `IsPending` is true
  while any call is in-flight; `LastResult` is whichever finishes last.
- Unmount during pending cancels the mutator token. `OnError` does **not**
  fire for the cancellation — it's silent, matching `UseResource`'s
  cancellation semantics.

---

## Pending fallbacks

Wrap a subtree that depends on multiple resources in `Pending(fallback, child)`.
The fallback is visible until every `UseResource` / `UseInfiniteResource` inside
the subtree has left the `Loading` state:

```csharp
return PendingFactory.Pending(
    fallback: Text("Loading dashboard…").Opacity(0.5),
    child:    FlexColumn(
        UserHeader(),     // UseResource(userId)
        RecentActivity(), // UseInfiniteResource(feed)
        Stats()));        // UseResource(statsEndpoint)
```

Both trees are mounted — the child renders in the background so it's ready when
all three resolve. `Reloading` (stale-while-revalidate refetches) does **not**
re-trigger the fallback; only the initial `Loading` does. This matches
TanStack's `Suspense` semantics.

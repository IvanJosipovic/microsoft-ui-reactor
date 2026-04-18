using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Data;
using Microsoft.UI.Reactor.Hooks;
using Xunit;

namespace Microsoft.UI.Reactor.Tests.Core;

public class DataSourceResourceTests
{
    private sealed class InlineDispatcher : IHookDispatcher
    {
        public void Post(Action action) => action();
    }

    // Minimal in-memory IDataSource<int> for test purposes.
    private sealed class InMemorySource : IDataSource<int>
    {
        public int CallCount;
        private readonly int _totalItems;
        private readonly int _pageSize;

        public InMemorySource(int total, int pageSize)
        {
            _totalItems = total;
            _pageSize = pageSize;
        }

        public DataSourceCapabilities Capabilities => DataSourceCapabilities.ServerCount;

        public RowKey GetRowKey(int item) => item;

        public Task<DataPage<int>> GetPageAsync(DataRequest request, CancellationToken ct = default)
        {
            Interlocked.Increment(ref CallCount);
            int start = request.ContinuationToken is null ? 0 : int.Parse(request.ContinuationToken);
            int count = Math.Min(_pageSize, Math.Max(0, _totalItems - start));
            var items = Enumerable.Range(start, count).ToList();
            string? next = start + count >= _totalItems ? null : (start + count).ToString();
            return Task.FromResult(new DataPage<int>(items, next, _totalItems));
        }
    }

    [Fact]
    public void UseDataSource_Projects_IDataSource_To_InfiniteResource()
    {
        var cache = new QueryCache();
        var source = new InMemorySource(total: 50, pageSize: 10);
        var ctx = new RenderContext();
        ctx.BeginRender(() => { });

        var resource = ctx.UseDataSource(source, new DataRequest { PageSize = 10 }, cache,
            new InfiniteResourceOptions(PageSize: 10), new InlineDispatcher());

        Assert.Equal(50, resource.TotalCount);
        Assert.Equal(1, source.CallCount); // first page

        resource.FetchNext();
        Assert.Equal(2, source.CallCount);
        // With a known TotalCount, Items.Count reports the full length with placeholder
        // slots for unloaded pages. The first 20 are loaded; slot 25 is still a placeholder.
        Assert.Equal(50, resource.Items.Count);
        Assert.Equal(0, resource.Items[0]);   // item 0
        Assert.Equal(19, resource.Items[19]); // last loaded item
    }

    [Fact]
    public void Deps_Derived_From_Request_Change_Restart_Pagination()
    {
        var cache = new QueryCache();
        var source = new InMemorySource(total: 50, pageSize: 10);
        var ctx = new RenderContext();
        ctx.BeginRender(() => { });

        ctx.UseDataSource(source, new DataRequest { PageSize = 10, SearchQuery = "a" }, cache,
            new InfiniteResourceOptions(PageSize: 10), new InlineDispatcher());
        Assert.Equal(1, source.CallCount);

        ctx.BeginRender(() => { });
        ctx.UseDataSource(source, new DataRequest { PageSize = 10, SearchQuery = "b" }, cache,
            new InfiniteResourceOptions(PageSize: 10), new InlineDispatcher());

        // Different search query → deps change → new first page fetch.
        Assert.Equal(2, source.CallCount);
    }
}

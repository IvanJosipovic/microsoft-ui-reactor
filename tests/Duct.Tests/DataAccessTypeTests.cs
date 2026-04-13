using Duct.Data;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for core data access types: RowKey, DataPage, SortDescriptor,
/// FilterDescriptor, DataRequest, and DataSourceCapabilities.
/// </summary>
public class DataAccessTypeTests
{
    // ── RowKey ───────────────────────────────────────────────────

    [Fact]
    public void RowKey_From_String()
    {
        RowKey key = "abc-123";
        Assert.Equal("abc-123", key.Value);
    }

    [Fact]
    public void RowKey_From_Int()
    {
        RowKey key = 42;
        Assert.Equal("42", key.Value);
    }

    [Fact]
    public void RowKey_From_Guid()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        RowKey key = guid;
        Assert.Equal("12345678-1234-1234-1234-123456789abc", key.Value);
    }

    [Fact]
    public void RowKey_Equality()
    {
        RowKey a = "key1";
        RowKey b = "key1";
        RowKey c = "key2";

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void RowKey_ToString()
    {
        RowKey key = "my-key";
        Assert.Equal("my-key", key.ToString());
    }

    [Fact]
    public void RowKey_Implicit_Conversions()
    {
        RowKey fromString = "str";
        RowKey fromInt = 99;
        RowKey fromGuid = Guid.Empty;

        Assert.Equal("str", fromString.Value);
        Assert.Equal("99", fromInt.Value);
        Assert.Equal(Guid.Empty.ToString(), fromGuid.Value);
    }

    // ── DataPage ────────────────────────────────────────────────

    [Fact]
    public void DataPage_Construction()
    {
        var items = new[] { "a", "b", "c" };
        var page = new DataPage<string>(items, "token-2", 10);

        Assert.Equal(3, page.Items.Count);
        Assert.Equal("token-2", page.ContinuationToken);
        Assert.Equal(10, page.TotalCount);
    }

    [Fact]
    public void DataPage_WithExpression()
    {
        var page = new DataPage<int>(new[] { 1, 2, 3 }, "t1", 100);
        var next = page with { ContinuationToken = "t2" };

        Assert.Equal("t2", next.ContinuationToken);
        Assert.Equal(100, next.TotalCount);
    }

    [Fact]
    public void DataPage_Null_ContinuationToken_Means_LastPage()
    {
        var page = new DataPage<int>(new[] { 1, 2 });
        Assert.Null(page.ContinuationToken);
    }

    // ── SortDescriptor ──────────────────────────────────────────

    [Fact]
    public void SortDescriptor_Construction()
    {
        var sort = new SortDescriptor("Name", SortDirection.Descending);
        Assert.Equal("Name", sort.Field);
        Assert.Equal(SortDirection.Descending, sort.Direction);
    }

    [Fact]
    public void SortDescriptor_Default_Direction_Is_Ascending()
    {
        var sort = new SortDescriptor("Age");
        Assert.Equal(SortDirection.Ascending, sort.Direction);
    }

    [Fact]
    public void SortDescriptor_Equality()
    {
        var a = new SortDescriptor("X", SortDirection.Ascending);
        var b = new SortDescriptor("X", SortDirection.Ascending);
        var c = new SortDescriptor("X", SortDirection.Descending);

        Assert.Equal(a, b);
        Assert.NotEqual(a, c);
    }

    // ── FilterDescriptor ────────────────────────────────────────

    [Fact]
    public void FilterDescriptor_Construction()
    {
        var filter = new FilterDescriptor("Age", FilterOperator.GreaterThan, 18);
        Assert.Equal("Age", filter.Field);
        Assert.Equal(FilterOperator.GreaterThan, filter.Operator);
        Assert.Equal(18, filter.Value);
    }

    [Fact]
    public void FilterDescriptor_Between_Uses_ValueTo()
    {
        var filter = new FilterDescriptor("Price", FilterOperator.Between, 10.0, 99.99);
        Assert.Equal(10.0, filter.Value);
        Assert.Equal(99.99, filter.ValueTo);
    }

    [Fact]
    public void FilterDescriptor_Equality()
    {
        var a = new FilterDescriptor("X", FilterOperator.Equals, "hello");
        var b = new FilterDescriptor("X", FilterOperator.Equals, "hello");
        Assert.Equal(a, b);
    }

    // ── DataRequest ─────────────────────────────────────────────

    [Fact]
    public void DataRequest_Default_Values()
    {
        var req = new DataRequest();
        Assert.Equal(50, req.PageSize);
        Assert.Null(req.ContinuationToken);
        Assert.Null(req.Sort);
        Assert.Null(req.Filters);
        Assert.Null(req.SearchQuery);
        Assert.Null(req.Select);
    }

    [Fact]
    public void DataRequest_WithExpression()
    {
        var req = new DataRequest { PageSize = 20 };
        var next = req with
        {
            ContinuationToken = "page2",
            Sort = new[] { new SortDescriptor("Name") },
        };

        Assert.Equal(20, next.PageSize);
        Assert.Equal("page2", next.ContinuationToken);
        Assert.Single(next.Sort!);
    }

    // ── DataSourceCapabilities ──────────────────────────────────

    [Fact]
    public void Capabilities_Flags_Composition()
    {
        var caps = DataSourceCapabilities.ServerSort | DataSourceCapabilities.ServerFilter | DataSourceCapabilities.Mutate;
        Assert.True(caps.HasFlag(DataSourceCapabilities.ServerSort));
        Assert.True(caps.HasFlag(DataSourceCapabilities.ServerFilter));
        Assert.True(caps.HasFlag(DataSourceCapabilities.Mutate));
        Assert.False(caps.HasFlag(DataSourceCapabilities.ServerSearch));
        Assert.False(caps.HasFlag(DataSourceCapabilities.Refresh));
    }
}

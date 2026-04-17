using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for WrapGridElement — DSL factory, record properties, defaults,
/// child handling, reconciler dispatch, and Set() extension.
/// </summary>
public class WrapGridElementTests
{
    // ════════════════════════════════════════════════════════════════
    //  DSL factory and record defaults
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void WrapGrid_Creates_With_Children()
    {
        var el = WrapGrid(Factories.Text("A"), Factories.Text("B"), Factories.Text("C"));
        Assert.IsType<WrapGridElement>(el);
        Assert.Equal(3, el.Children.Length);
    }

    [Fact]
    public void WrapGrid_Filters_Null_Children()
    {
        var el = WrapGrid(Factories.Text("A"), null, Factories.Text("B"));
        Assert.Equal(2, el.Children.Length);
    }

    [Fact]
    public void WrapGrid_Default_Properties()
    {
        var el = WrapGrid(Factories.Text("A"));
        Assert.Equal(-1, el.MaximumRowsOrColumns);
        Assert.Equal(Orientation.Horizontal, el.Orientation);
        Assert.True(double.IsNaN(el.ItemWidth));
        Assert.True(double.IsNaN(el.ItemHeight));
    }

    [Fact]
    public void WrapGrid_With_MaxRowsOrColumns()
    {
        var el = WrapGrid(3, Factories.Text("A"), Factories.Text("B"), Factories.Text("C"));
        Assert.Equal(3, el.MaximumRowsOrColumns);
        Assert.Equal(3, el.Children.Length);
    }

    [Fact]
    public void WrapGrid_Orientation_Via_Init()
    {
        var el = WrapGrid(Factories.Text("A")) with { Orientation = Orientation.Vertical };
        Assert.Equal(Orientation.Vertical, el.Orientation);
    }

    [Fact]
    public void WrapGrid_ItemSize_Via_Init()
    {
        var el = WrapGrid(Factories.Text("A")) with { ItemWidth = 50, ItemHeight = 50 };
        Assert.Equal(50, el.ItemWidth);
        Assert.Equal(50, el.ItemHeight);
    }

    [Fact]
    public void WrapGrid_Is_Element()
    {
        Element el = WrapGrid(Factories.Text("A"));
        Assert.IsAssignableFrom<Element>(el);
    }

    [Fact]
    public void WrapGrid_Record_Equality_Same_Array()
    {
        var children = new Element[] { Factories.Text("A") };
        var a = new WrapGridElement(children) { MaximumRowsOrColumns = 3 };
        var b = new WrapGridElement(children) { MaximumRowsOrColumns = 3 };
        Assert.Equal(a, b);
    }

    [Fact]
    public void WrapGrid_Record_Inequality_Different_MaxRows()
    {
        var children = new Element[] { Factories.Text("A") };
        var a = new WrapGridElement(children) { MaximumRowsOrColumns = 3 };
        var b = new WrapGridElement(children) { MaximumRowsOrColumns = 4 };
        Assert.NotEqual(a, b);
    }

    // ════════════════════════════════════════════════════════════════
    //  Set() extension
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Set_Adds_Setter_To_WrapGridElement()
    {
        var el = WrapGrid(Factories.Text("A"))
            .Set(wg => wg.HorizontalChildrenAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center);
        Assert.NotEqual(WrapGrid(Factories.Text("A")), el);
    }

    // ════════════════════════════════════════════════════════════════
    //  Modifiers work on WrapGridElement
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void Modifiers_Work_On_WrapGrid()
    {
        var el = WrapGrid(Factories.Text("A")).Margin(8).Width(400);
        Assert.NotNull(el.Modifiers);
        Assert.Equal(400, el.Modifiers!.Width);
    }

    // ════════════════════════════════════════════════════════════════
    //  Reconciler dispatch
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void CanUpdate_Same_WrapGrid_Elements()
    {
        var reconciler = new Reconciler();
        var a = WrapGrid(Factories.Text("A"));
        var b = WrapGrid(Factories.Text("B"));
        Assert.True(reconciler.CanUpdate(a, b));
    }

    [Fact]
    public void CanUpdate_WrapGrid_Vs_Stack_Returns_False()
    {
        var reconciler = new Reconciler();
        Assert.False(reconciler.CanUpdate(WrapGrid(Factories.Text("A")), VStack(Factories.Text("A"))));
    }

    [Fact]
    public void Mount_Dispatches_WrapGridElement()
    {
        var reconciler = new Reconciler();
        try
        {
            var ctrl = reconciler.Mount(WrapGrid(3, Factories.Text("A"), Factories.Text("B")), () => { });
            Assert.NotNull(ctrl);
            Assert.IsType<VariableSizedWrapGrid>(ctrl);
        }
        catch (global::System.Runtime.InteropServices.COMException)
        {
            // Expected on CI/non-WinUI thread
        }
    }

    // ════════════════════════════════════════════════════════════════
    //  Empty WrapGrid
    // ════════════════════════════════════════════════════════════════

    [Fact]
    public void WrapGrid_Empty_Has_No_Children()
    {
        var el = WrapGrid();
        Assert.Empty(el.Children);
    }

    [Fact]
    public void WrapGrid_WithKey_Sets_Key()
    {
        var el = WrapGrid(Factories.Text("A")).WithKey("grid-1");
        Assert.Equal("grid-1", el.Key);
    }
}

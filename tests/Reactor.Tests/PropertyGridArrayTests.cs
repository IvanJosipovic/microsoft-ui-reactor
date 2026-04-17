using Microsoft.UI.Reactor.Controls;
using Xunit;

namespace Microsoft.UI.Reactor.Tests;

/// <summary>
/// Tests for array/list support in the PropertyGrid.
/// </summary>
public class PropertyGridArrayTests
{
    private class ItemModel
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
        public ItemModel() { }
        public ItemModel(string name, int value) { Name = name; Value = value; }
    }

    private class NoDefaultCtor
    {
        public string Label { get; }
        public NoDefaultCtor(string label) => Label = label;
    }

    // ── Array resolution ──────────────────────────────────────────

    [Fact]
    public void Array_Property_Renders_With_Correct_Item_Count()
    {
        var items = new[] { new ItemModel("A", 1), new ItemModel("B", 2), new ItemModel("C", 3) };
        Assert.Equal(3, ArrayOperations.GetCount(items));
    }

    [Fact]
    public void List_Property_Renders_With_Correct_Item_Count()
    {
        var items = new List<ItemModel>
        {
            new("A", 1), new("B", 2)
        };
        Assert.Equal(2, ArrayOperations.GetCount(items));
    }

    // ── Add ───────────────────────────────────────────────────────

    [Fact]
    public void Add_To_List_Appends()
    {
        var items = new List<ItemModel> { new("A", 1) };
        var result = ArrayOperations.Add(items, new ItemModel("B", 2), typeof(ItemModel));

        Assert.Same(items, result); // mutates in-place
        Assert.Equal(2, items.Count);
        Assert.Equal("B", items[1].Name);
    }

    [Fact]
    public void Add_To_Array_Returns_New_Array()
    {
        var items = new[] { new ItemModel("A", 1) };
        var result = (ItemModel[])ArrayOperations.Add(items, new ItemModel("B", 2), typeof(ItemModel));

        Assert.NotSame(items, result); // new array
        Assert.Equal(2, result.Length);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("B", result[1].Name);
    }

    // ── Remove ────────────────────────────────────────────────────

    [Fact]
    public void Remove_From_List()
    {
        var items = new List<ItemModel>
        {
            new("A", 1), new("B", 2), new("C", 3)
        };
        ArrayOperations.RemoveAt(items, 1, typeof(ItemModel));

        Assert.Equal(2, items.Count);
        Assert.Equal("A", items[0].Name);
        Assert.Equal("C", items[1].Name);
    }

    [Fact]
    public void Remove_From_Array_Returns_New_Array()
    {
        var items = new[] { new ItemModel("A", 1), new ItemModel("B", 2), new ItemModel("C", 3) };
        var result = (ItemModel[])ArrayOperations.RemoveAt(items, 1, typeof(ItemModel));

        Assert.Equal(2, result.Length);
        Assert.Equal("A", result[0].Name);
        Assert.Equal("C", result[1].Name);
    }

    // ── Reorder ───────────────────────────────────────────────────

    [Fact]
    public void MoveUp_In_List()
    {
        var items = new List<ItemModel>
        {
            new("A", 1), new("B", 2), new("C", 3)
        };
        ArrayOperations.MoveUp(items, 2, typeof(ItemModel));

        Assert.Equal("A", items[0].Name);
        Assert.Equal("C", items[1].Name);
        Assert.Equal("B", items[2].Name);
    }

    [Fact]
    public void MoveDown_In_List()
    {
        var items = new List<ItemModel>
        {
            new("A", 1), new("B", 2), new("C", 3)
        };
        ArrayOperations.MoveDown(items, 0, typeof(ItemModel));

        Assert.Equal("B", items[0].Name);
        Assert.Equal("A", items[1].Name);
        Assert.Equal("C", items[2].Name);
    }

    [Fact]
    public void MoveUp_At_Index_0_Is_Noop()
    {
        var items = new List<ItemModel>
        {
            new("A", 1), new("B", 2)
        };
        ArrayOperations.MoveUp(items, 0, typeof(ItemModel));

        Assert.Equal("A", items[0].Name);
        Assert.Equal("B", items[1].Name);
    }

    [Fact]
    public void MoveDown_At_Last_Index_Is_Noop()
    {
        var items = new List<ItemModel>
        {
            new("A", 1), new("B", 2)
        };
        ArrayOperations.MoveDown(items, 1, typeof(ItemModel));

        Assert.Equal("A", items[0].Name);
        Assert.Equal("B", items[1].Name);
    }

    // ── Array replacement via setter ──────────────────────────────

    [Fact]
    public void Array_Replacement_Via_Setter_After_Mutation()
    {
        var registry = new TypeRegistry();
        var parent = new ArrayParent { Items = new[] { "A", "B" } };
        var meta = registry.Resolve(typeof(ArrayParent));
        var descriptors = meta.Decompose!(parent);
        var itemsProp = descriptors.First(d => d.Name == "Items");

        // Remove via array operation
        var newArray = ArrayOperations.RemoveAt(parent.Items, 0, typeof(string));
        var result = itemsProp.SetValue!(parent, newArray);

        Assert.Same(parent, result); // mutable parent
        Assert.Single(parent.Items);
        Assert.Equal("B", parent.Items[0]);
    }

    // ── CreateElement ─────────────────────────────────────────────

    [Fact]
    public void CreateElement_Null_For_No_Parameterless_Ctor()
    {
        var registry = new TypeRegistry();
        var meta = (ArrayTypeMetadata)registry.Resolve(typeof(NoDefaultCtor[]));
        Assert.Null(meta.CreateElement);
    }

    [Fact]
    public async Task CreateElement_Works_For_Parameterless_Ctor()
    {
        var registry = new TypeRegistry();
        var meta = (ArrayTypeMetadata)registry.Resolve(typeof(ItemModel[]));
        Assert.NotNull(meta.CreateElement);

        var item = await meta.CreateElement!();
        Assert.NotNull(item);
        Assert.IsType<ItemModel>(item);
    }

    // ── Element type detection ────────────────────────────────────

    [Fact]
    public void GetElementType_For_Array()
    {
        Assert.Equal(typeof(int), ArrayOperations.GetElementType(typeof(int[])));
        Assert.Equal(typeof(string), ArrayOperations.GetElementType(typeof(string[])));
    }

    [Fact]
    public void GetElementType_For_List()
    {
        Assert.Equal(typeof(int), ArrayOperations.GetElementType(typeof(List<int>)));
        Assert.Equal(typeof(string), ArrayOperations.GetElementType(typeof(List<string>)));
    }

    // ── Test model ────────────────────────────────────────────────

    private class ArrayParent
    {
        public string[] Items { get; set; } = Array.Empty<string>();
    }
}

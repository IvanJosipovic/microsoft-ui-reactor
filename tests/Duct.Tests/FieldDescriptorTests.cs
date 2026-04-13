using Duct.Data;
using Xunit;

namespace Duct.Tests;

/// <summary>
/// Tests for FieldDescriptor record, PinPosition enum, and grid-specific attributes.
/// </summary>
public class FieldDescriptorTests
{
    // ── 0A.1: FieldDescriptor Record ────────────────────────────

    [Fact]
    public void FieldDescriptor_Creation_SetsRequiredProperties()
    {
        var fd = new FieldDescriptor
        {
            Name = "Age",
            FieldType = typeof(int),
            GetValue = obj => 42,
        };

        Assert.Equal("Age", fd.Name);
        Assert.Equal(typeof(int), fd.FieldType);
        Assert.Equal(42, fd.GetValue(new object()));
    }

    [Fact]
    public void FieldDescriptor_Defaults_AreCorrect()
    {
        var fd = new FieldDescriptor
        {
            Name = "X",
            FieldType = typeof(string),
            GetValue = _ => null,
        };

        Assert.Null(fd.DisplayName);
        Assert.Null(fd.SetValue);
        Assert.False(fd.IsReadOnly);
        Assert.Null(fd.Category);
        Assert.Null(fd.Description);
        Assert.Equal(0, fd.Order);
        Assert.Null(fd.Editor);
        Assert.Null(fd.Validators);
        Assert.Null(fd.AsyncValidators);
        Assert.Null(fd.Width);
        Assert.Null(fd.MinWidth);
        Assert.Null(fd.MaxWidth);
        Assert.Null(fd.Flex);
        Assert.True(fd.Sortable);
        Assert.True(fd.Filterable);
        Assert.Equal(PinPosition.None, fd.Pin);
        Assert.Null(fd.CellRenderer);
        Assert.Null(fd.FormatValue);
    }

    [Fact]
    public void FieldDescriptor_WithExpression_CreatesModifiedCopy()
    {
        var fd = new FieldDescriptor
        {
            Name = "Name",
            FieldType = typeof(string),
            GetValue = _ => "hello",
            Sortable = true,
        };

        var modified = fd with { DisplayName = "Full Name", Sortable = false, Pin = PinPosition.Left };

        Assert.Equal("Name", modified.Name);
        Assert.Equal("Full Name", modified.DisplayName);
        Assert.False(modified.Sortable);
        Assert.Equal(PinPosition.Left, modified.Pin);
        // Original unchanged
        Assert.Null(fd.DisplayName);
        Assert.True(fd.Sortable);
    }

    [Fact]
    public void SetValue_ReturnNewOwner_MutableObject_ReturnsSameReference()
    {
        var model = new MutableModel { Name = "A" };

        var fd = new FieldDescriptor
        {
            Name = "Name",
            FieldType = typeof(string),
            GetValue = obj => ((MutableModel)obj).Name,
            SetValue = (obj, val) =>
            {
                ((MutableModel)obj).Name = (string)val!;
                return obj; // same reference
            },
        };

        var result = fd.SetValue!(model, "B");
        Assert.Same(model, result);
        Assert.Equal("B", model.Name);
    }

    [Fact]
    public void SetValue_ReturnNewOwner_ImmutableRecord_ReturnsNewObject()
    {
        var point = new ImmutablePoint(1, 2);

        var fd = new FieldDescriptor
        {
            Name = "X",
            FieldType = typeof(int),
            GetValue = obj => ((ImmutablePoint)obj).X,
            SetValue = (obj, val) => ((ImmutablePoint)obj) with { X = (int)val! },
        };

        var result = fd.SetValue!(point, 99);
        Assert.NotSame(point, result);
        Assert.Equal(99, ((ImmutablePoint)result).X);
        Assert.Equal(2, ((ImmutablePoint)result).Y);
        // Original unchanged
        Assert.Equal(1, point.X);
    }

    // ── 0A.2: Grid-Specific Attributes ──────────────────────────

    [Fact]
    public void ColumnWidthAttribute_SetsProperties()
    {
        var attr = new ColumnWidthAttribute(150) { MinWidth = 80, MaxWidth = 300 };
        Assert.Equal(150, attr.Width);
        Assert.Equal(80, attr.MinWidth);
        Assert.Equal(300, attr.MaxWidth);
    }

    [Fact]
    public void ColumnWidthAttribute_DefaultMinMaxAreZero()
    {
        var attr = new ColumnWidthAttribute(200);
        Assert.Equal(200, attr.Width);
        Assert.Equal(0, attr.MinWidth);
        Assert.Equal(0, attr.MaxWidth);
    }

    [Fact]
    public void ColumnPinAttribute_SetsPosition()
    {
        var left = new ColumnPinAttribute(PinPosition.Left);
        var right = new ColumnPinAttribute(PinPosition.Right);
        Assert.Equal(PinPosition.Left, left.Position);
        Assert.Equal(PinPosition.Right, right.Position);
    }

    [Fact]
    public void NotSortableAttribute_CanBeConstructed()
    {
        var attr = new NotSortableAttribute();
        Assert.NotNull(attr);
    }

    [Fact]
    public void NotFilterableAttribute_CanBeConstructed()
    {
        var attr = new NotFilterableAttribute();
        Assert.NotNull(attr);
    }

    [Fact]
    public void PinPosition_EnumValues()
    {
        Assert.Equal(0, (int)PinPosition.None);
        Assert.Equal(1, (int)PinPosition.Left);
        Assert.Equal(2, (int)PinPosition.Right);
    }

    // ── Test models ─────────────────────────────────────────────

    private class MutableModel
    {
        public string Name { get; set; } = "";
    }

    private record ImmutablePoint(int X, int Y);
}

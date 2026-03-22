// C# port of Meta's Yoga layout engine FlexDirection utilities.
// Ported from yoga/algorithm/FlexDirection.h

namespace Duct.Yoga;

/// <summary>
/// Direction-aware utility functions for resolving flex axes to physical edges.
/// </summary>
internal static class FlexDirectionHelper
{
    public static bool IsRow(YogaFlexDirection flexDirection)
        => flexDirection == YogaFlexDirection.Row || flexDirection == YogaFlexDirection.RowReverse;

    public static bool IsColumn(YogaFlexDirection flexDirection)
        => flexDirection == YogaFlexDirection.Column || flexDirection == YogaFlexDirection.ColumnReverse;

    /// <summary>Apply RTL transformation to flex direction.</summary>
    public static YogaFlexDirection ResolveDirection(YogaFlexDirection flexDirection, YogaDirection direction)
    {
        if (direction == YogaDirection.RTL)
        {
            if (flexDirection == YogaFlexDirection.Row) return YogaFlexDirection.RowReverse;
            if (flexDirection == YogaFlexDirection.RowReverse) return YogaFlexDirection.Row;
        }
        return flexDirection;
    }

    /// <summary>Get the perpendicular (cross) direction.</summary>
    public static YogaFlexDirection ResolveCrossDirection(YogaFlexDirection flexDirection, YogaDirection direction)
        => IsColumn(flexDirection)
            ? ResolveDirection(YogaFlexDirection.Row, direction)
            : YogaFlexDirection.Column;

    /// <summary>Get the physical edge at the flex-start of an axis.</summary>
    public static YogaPhysicalEdge FlexStartEdge(YogaFlexDirection flexDirection) => flexDirection switch
    {
        YogaFlexDirection.Column => YogaPhysicalEdge.Top,
        YogaFlexDirection.ColumnReverse => YogaPhysicalEdge.Bottom,
        YogaFlexDirection.Row => YogaPhysicalEdge.Left,
        YogaFlexDirection.RowReverse => YogaPhysicalEdge.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(flexDirection)),
    };

    /// <summary>Get the physical edge at the flex-end of an axis.</summary>
    public static YogaPhysicalEdge FlexEndEdge(YogaFlexDirection flexDirection) => flexDirection switch
    {
        YogaFlexDirection.Column => YogaPhysicalEdge.Bottom,
        YogaFlexDirection.ColumnReverse => YogaPhysicalEdge.Top,
        YogaFlexDirection.Row => YogaPhysicalEdge.Right,
        YogaFlexDirection.RowReverse => YogaPhysicalEdge.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(flexDirection)),
    };

    /// <summary>Get the inline-start edge (direction-aware).</summary>
    public static YogaPhysicalEdge InlineStartEdge(YogaFlexDirection flexDirection, YogaDirection direction)
    {
        if (IsRow(flexDirection))
            return direction == YogaDirection.RTL ? YogaPhysicalEdge.Right : YogaPhysicalEdge.Left;
        return YogaPhysicalEdge.Top;
    }

    /// <summary>Get the inline-end edge (direction-aware).</summary>
    public static YogaPhysicalEdge InlineEndEdge(YogaFlexDirection flexDirection, YogaDirection direction)
    {
        if (IsRow(flexDirection))
            return direction == YogaDirection.RTL ? YogaPhysicalEdge.Left : YogaPhysicalEdge.Right;
        return YogaPhysicalEdge.Bottom;
    }

    /// <summary>Get the dimension (Width or Height) for a flex direction.</summary>
    public static YogaDimension Dimension(YogaFlexDirection flexDirection) => flexDirection switch
    {
        YogaFlexDirection.Column => YogaDimension.Height,
        YogaFlexDirection.ColumnReverse => YogaDimension.Height,
        YogaFlexDirection.Row => YogaDimension.Width,
        YogaFlexDirection.RowReverse => YogaDimension.Width,
        _ => throw new ArgumentOutOfRangeException(nameof(flexDirection)),
    };
}

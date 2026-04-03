namespace Duct.PropertyGrid;

/// <summary>
/// Describes a single property within a decomposed type.
/// </summary>
public record PropertyDescriptor
{
    /// <summary>Property name (used as key in Compose dictionary).</summary>
    public required string Name { get; init; }

    /// <summary>Display label shown in the grid.</summary>
    public string? DisplayName { get; init; }

    /// <summary>The CLR type of this property's value.</summary>
    public required Type PropertyType { get; init; }

    /// <summary>Gets the current value from the parent object.</summary>
    public required Func<object> GetValue { get; init; }

    /// <summary>
    /// Sets the value on the parent object. Non-null for mutable properties.
    /// Null for immutable properties (use parent's Compose instead).
    /// </summary>
    public Action<object>? SetValue { get; init; }

    /// <summary>Category for grouping. Null = default/uncategorized.</summary>
    public string? Category { get; init; }

    /// <summary>Help text shown as tooltip.</summary>
    public string? Description { get; init; }

    /// <summary>Declaration order for stable sorting.</summary>
    public int Order { get; init; }

    /// <summary>Whether this property is read-only in the grid.</summary>
    public bool IsReadOnly { get; init; }
}

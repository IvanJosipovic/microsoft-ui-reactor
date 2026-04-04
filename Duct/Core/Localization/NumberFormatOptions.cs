namespace Duct.Core.Localization;

public enum NumberStyle
{
    Default,
    Currency,
    Percent
}

public sealed class NumberFormatOptions
{
    public NumberStyle Style { get; init; }
    public string? CurrencyCode { get; init; }
    public int? MinimumFractionDigits { get; init; }
    public int? MaximumFractionDigits { get; init; }
}

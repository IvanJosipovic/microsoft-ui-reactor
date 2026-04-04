namespace Duct.Core.Localization;

public enum DateStyle
{
    Default,
    Short,
    Long,
    Full
}

public sealed class DateFormatOptions
{
    public DateStyle Style { get; init; }
}

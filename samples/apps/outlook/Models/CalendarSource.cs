namespace ReactorOutlook.Models;

public sealed record CalendarSource(
    string Id,
    string Name,
    string ColorHex,
    bool IsEnabled
);

using ReactorOutlook.Models;

namespace ReactorOutlook.Services;

public sealed class MockCalendarService : ICalendarService
{
    public Task<CalendarSource[]> GetCalendarSourcesAsync() =>
        Task.FromResult(MockData.GetCalendarSources());

    public Task<CalendarEvent[]> GetEventsAsync(DateTimeOffset weekStart, DateTimeOffset weekEnd) =>
        Task.FromResult(MockData.GetCalendarEvents(weekStart));
}

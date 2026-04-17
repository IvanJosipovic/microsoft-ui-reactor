using ReactorOutlook.Models;

namespace ReactorOutlook.Services;

public interface ICalendarService
{
    Task<CalendarSource[]> GetCalendarSourcesAsync();
    Task<CalendarEvent[]> GetEventsAsync(DateTimeOffset weekStart, DateTimeOffset weekEnd);
}

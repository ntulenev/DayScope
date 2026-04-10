using DayScope.Application.Calendar;

namespace DayScope.Application.Abstractions;

public interface ICalendarService
{
    bool IsEnabled { get; }

    Task<CalendarLoadResult> GetEventsForDateAsync(
        DateOnly day,
        TimeZoneInfo timeZone,
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken);
}

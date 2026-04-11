using DayScope.Application.Calendar;

namespace DayScope.Application.Abstractions;

/// <summary>
/// Loads calendar data for a specific day.
/// </summary>
public interface ICalendarService
{
    bool IsEnabled { get; }

    /// <summary>
    /// Loads calendar events for the requested day in the requested local time zone.
    /// </summary>
    /// <param name="day">The day to load.</param>
    /// <param name="timeZone">The time zone that defines the requested day boundaries.</param>
    /// <param name="interactionMode">Whether the request may use interactive authentication.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The result of the calendar load operation.</returns>
    Task<CalendarLoadResult> GetEventsForDateAsync(
        DateOnly day,
        TimeZoneInfo timeZone,
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken);
}

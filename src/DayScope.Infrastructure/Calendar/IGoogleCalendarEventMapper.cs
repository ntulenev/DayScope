using Google.Apis.Calendar.v3.Data;

using DayScope.Domain.Calendar;

namespace DayScope.Infrastructure.Calendar;

/// <summary>
/// Maps Google Calendar SDK events to DayScope domain events.
/// </summary>
public interface IGoogleCalendarEventMapper
{
    /// <summary>
    /// Maps a Google Calendar event into the normalized domain model.
    /// </summary>
    /// <param name="calendarEvent">The SDK event to map.</param>
    /// <param name="timeZone">The fallback time zone for all-day events.</param>
    /// <returns>The normalized event, or <see langword="null"/> when the source event cannot be represented.</returns>
    CalendarEvent? MapEvent(Event calendarEvent, TimeZoneInfo timeZone);
}

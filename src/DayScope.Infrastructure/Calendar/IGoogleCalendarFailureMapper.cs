using DayScope.Application.Calendar;

namespace DayScope.Infrastructure.Calendar;

/// <summary>
/// Maps Google Calendar failures to application-level load statuses.
/// </summary>
public interface IGoogleCalendarFailureMapper
{
    /// <summary>
    /// Maps the provided exception to a calendar load status.
    /// </summary>
    /// <param name="exception">The exception thrown by Google infrastructure.</param>
    /// <returns>The calendar load status to surface to the application layer.</returns>
    CalendarLoadStatus Map(Exception exception);
}

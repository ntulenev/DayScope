using DayScope.Application.Calendar;

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Maps calendar load state to the user-facing day-schedule status text.
/// </summary>
internal static class DayScheduleStatusTextProvider
{
    /// <summary>
    /// Returns the status text shown above the schedule for the current load state.
    /// </summary>
    /// <param name="status">The current calendar load status.</param>
    /// <param name="isToday">Whether the selected day is today.</param>
    /// <param name="hasNoEvents">Whether the rendered schedule contains no events.</param>
    /// <returns>The status message to display, or an empty string when none is needed.</returns>
    public static string GetStatusText(
        CalendarLoadStatus status,
        bool isToday,
        bool hasNoEvents)
    {
        if (status == CalendarLoadStatus.Success && hasNoEvents)
        {
            return isToday
                ? "No events scheduled for today."
                : "No events scheduled for this day.";
        }

        return status switch
        {
            CalendarLoadStatus.Loading => isToday
                ? "Loading today's schedule..."
                : "Loading schedule...",
            CalendarLoadStatus.Success => string.Empty,
            CalendarLoadStatus.Disabled => "Google Calendar is disabled in appsettings.",
            CalendarLoadStatus.ClientSecretsMissing =>
                "Add Google OAuth client JSON to connect Google Calendar.",
            CalendarLoadStatus.AuthorizationRequired => isToday
                ? "Google Calendar sign-in is required to show today's schedule."
                : "Google Calendar sign-in is required to show this day's schedule.",
            CalendarLoadStatus.AccessDenied => "Calendar not found or access denied.",
            CalendarLoadStatus.Unavailable => "Google Calendar is unavailable right now.",
            CalendarLoadStatus.NoEvents => isToday
                ? "No events scheduled for today."
                : "No events scheduled for this day.",
            _ => string.Empty
        };
    }
}

using DayScope.Application.Calendar;
using DayScope.Application.DaySchedule;

namespace DayScope.Application.Dashboard;

/// <summary>
/// Coordinates calendar loading and day schedule display state generation.
/// </summary>
public interface IDayScheduleDashboardService
{
    bool IsCalendarEnabled { get; }

    TimeSpan CalendarRefreshInterval { get; }

    /// <summary>
    /// Builds the current display state from the last loaded agenda.
    /// </summary>
    /// <param name="availableScheduleWidth">The available width for the schedule canvas, if known.</param>
    /// <returns>The current display state.</returns>
    DayScheduleDisplayState GetCurrentDisplayState(double? availableScheduleWidth = null);

    /// <summary>
    /// Moves the selected date by the specified number of days.
    /// </summary>
    /// <param name="dayOffset">The number of days to move backward or forward.</param>
    void ShiftSelectedDate(int dayOffset);

    /// <summary>
    /// Refreshes calendar data and returns the updated display state.
    /// </summary>
    /// <param name="interactionMode">Whether interactive authentication is allowed.</param>
    /// <param name="availableScheduleWidth">The available width for the schedule canvas, if known.</param>
    /// <param name="cancellationToken">The cancellation token for the refresh.</param>
    /// <returns>The refreshed display state.</returns>
    Task<DayScheduleDisplayState> RefreshCalendarAsync(
        CalendarInteractionMode interactionMode,
        double? availableScheduleWidth,
        CancellationToken cancellationToken);
}

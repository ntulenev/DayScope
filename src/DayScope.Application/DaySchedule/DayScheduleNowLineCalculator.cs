namespace DayScope.Application.DaySchedule;

/// <summary>
/// Calculates the current-time marker position for the rendered schedule.
/// </summary>
internal static class DayScheduleNowLineCalculator
{
    /// <summary>
    /// Calculates the current-time marker offset within the visible timeline.
    /// </summary>
    /// <param name="localNow">The current time in the local display zone.</param>
    /// <param name="selectedDate">The selected date being displayed.</param>
    /// <param name="timelineStart">The first visible timeline instant.</param>
    /// <param name="timelineEnd">The final visible timeline instant.</param>
    /// <param name="hourHeight">The rendered height of one hour slot.</param>
    /// <returns>The marker offset in pixels, or <see langword="null"/> when it should not be shown.</returns>
    public static double? TryCalculate(
        DateTimeOffset localNow,
        DateOnly selectedDate,
        DateTimeOffset timelineStart,
        DateTimeOffset timelineEnd,
        int hourHeight)
    {
        if (selectedDate != DateOnly.FromDateTime(localNow.DateTime) ||
            localNow < timelineStart ||
            localNow > timelineEnd)
        {
            return null;
        }

        return (localNow - timelineStart).TotalMinutes / 60d * hourHeight;
    }
}

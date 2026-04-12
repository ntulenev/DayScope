using DayScope.Domain.Configuration;

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Builds timeline metrics used across the day schedule display pipeline.
/// </summary>
internal static class DayScheduleTimelineMetricsFactory
{
    private const int TIMELINE_BOTTOM_PADDING = 18;

    /// <summary>
    /// Creates the shared timeline metrics for the selected day and layout settings.
    /// </summary>
    /// <param name="localZone">The primary local time zone.</param>
    /// <param name="selectedDate">The selected date being rendered.</param>
    /// <param name="settings">The schedule display settings.</param>
    /// <param name="availableScheduleWidth">The measured schedule width, if available.</param>
    /// <returns>The computed timeline metrics for the rendered schedule.</returns>
    public static DayScheduleTimelineMetrics Create(
        TimeZoneInfo localZone,
        DateOnly selectedDate,
        DayScheduleSettings settings,
        double? availableScheduleWidth)
    {
        ArgumentNullException.ThrowIfNull(localZone);
        ArgumentNullException.ThrowIfNull(settings);

        var labelReferenceInstant = CreateDateTimeOffset(localZone, selectedDate, 12);
        var timelineStart = CreateDateTimeOffset(localZone, selectedDate, settings.StartHour);
        var timelineEnd = CreateDateTimeOffset(localZone, selectedDate, settings.EndHour);
        var timelineHeight = ((settings.EndHour - settings.StartHour) * settings.HourHeight)
            + TIMELINE_BOTTOM_PADDING;
        var scheduleWidth = ResolveScheduleWidth(settings.ScheduleCanvasWidth, availableScheduleWidth);

        return new DayScheduleTimelineMetrics(
            labelReferenceInstant,
            timelineStart,
            timelineEnd,
            timelineHeight,
            scheduleWidth);
    }

    private static int ResolveScheduleWidth(
        int configuredWidth,
        double? availableScheduleWidth)
    {
        var width = availableScheduleWidth.HasValue
            ? (int)Math.Floor(availableScheduleWidth.Value)
            : configuredWidth;

        return Math.Max(420, width);
    }

    private static DateTimeOffset CreateDateTimeOffset(
        TimeZoneInfo timeZone,
        DateOnly date,
        int hour)
    {
        var dayOffset = hour / 24;
        var normalizedHour = hour % 24;
        var dateTime = date
            .AddDays(dayOffset)
            .ToDateTime(new TimeOnly(normalizedHour, 0));

        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }
}

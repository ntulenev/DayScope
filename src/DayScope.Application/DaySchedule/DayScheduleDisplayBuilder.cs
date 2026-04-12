using System.Globalization;

using DayScope.Application.Calendar;
using DayScope.Domain.Configuration;

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Builds the display model shown by the day schedule dashboard.
/// </summary>
public static class DayScheduleDisplayBuilder
{
    /// <summary>
    /// Converts the current calendar load result into a display state for the dashboard.
    /// </summary>
    /// <param name="loadResult">The current calendar load result.</param>
    /// <param name="settings">The schedule display settings.</param>
    /// <param name="now">The current time.</param>
    /// <param name="selectedDate">The selected date to display.</param>
    /// <param name="localZone">The local time zone used for display calculations.</param>
    /// <param name="availableScheduleWidth">The available schedule width, if known.</param>
    /// <returns>The display state shown by the dashboard.</returns>
    public static DayScheduleDisplayState Build(
        CalendarLoadResult loadResult,
        DayScheduleSettings settings,
        DateTimeOffset now,
        DateOnly selectedDate,
        TimeZoneInfo localZone,
        double? availableScheduleWidth = null)
    {
        ArgumentNullException.ThrowIfNull(loadResult);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localZone);

        var localNow = TimeZoneInfo.ConvertTime(now, localZone);
        var isToday = selectedDate == DateOnly.FromDateTime(localNow.DateTime);
        var dateDisplay = selectedDate.ToDateTime(TimeOnly.MinValue);
        var timelineMetrics = DayScheduleTimelineMetricsFactory.Create(
            localZone,
            selectedDate,
            settings,
            availableScheduleWidth);
        var secondaryZone = DayScheduleTimeZoneResolver.TryResolve(settings.SecondaryTimeZoneId);

        var allDayEvents = loadResult.Agenda.Events
            .Where(calendarEvent => calendarEvent.IsAllDay)
            .Select(calendarEvent => DayScheduleEventPresentationFactory.CreateAllDayEvent(
                calendarEvent,
                localZone))
            .ToArray();

        var timedEvents = DayScheduleTimelineLayout.BuildTimedEvents(
            loadResult.Agenda.Events,
            timelineMetrics.TimelineStart,
            timelineMetrics.TimelineEnd,
            timelineMetrics.ScheduleWidth,
            settings.HourHeight,
            localZone);

        var statusText = DayScheduleStatusTextProvider.GetStatusText(
            loadResult.Status,
            isToday,
            allDayEvents.Length == 0 && timedEvents.Count == 0);

        return new DayScheduleDisplayState(
            selectedDate,
            dateDisplay.ToString("MMMM yyyy", _culture),
            dateDisplay.ToString("ddd", _culture),
            selectedDate.ToString("%d", _culture),
            dateDisplay.ToString("dddd, d MMMM", _culture),
            DayScheduleTimeZoneLabelFormatter.Format(
                localZone,
                settings.PrimaryTimeZoneLabel,
                timelineMetrics.LabelReferenceInstant),
            secondaryZone is null
                ? null
                : DayScheduleTimeZoneLabelFormatter.Format(
                    secondaryZone,
                    settings.SecondaryTimeZoneLabel,
                    TimeZoneInfo.ConvertTime(timelineMetrics.LabelReferenceInstant, secondaryZone)),
            DayScheduleTimelineLabelBuilder.Build(
                timelineMetrics.TimelineStart,
                settings,
                localZone),
            secondaryZone is null
                ? []
                : DayScheduleTimelineLabelBuilder.Build(
                    timelineMetrics.TimelineStart,
                    settings,
                    secondaryZone),
            allDayEvents,
            timedEvents,
            timelineMetrics.ScheduleWidth,
            timelineMetrics.TimelineHeight,
            statusText,
            !string.IsNullOrWhiteSpace(statusText),
            DayScheduleNowLineCalculator.TryCalculate(
                localNow,
                selectedDate,
                timelineMetrics.TimelineStart,
                timelineMetrics.TimelineEnd,
                settings.HourHeight),
            localNow.ToString("h:mm tt", _culture).Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");
}

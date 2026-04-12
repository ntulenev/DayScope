namespace DayScope.Application.DaySchedule;

/// <summary>
/// Carries timeline-related metrics used to build the day schedule display.
/// </summary>
/// <param name="LabelReferenceInstant">The instant used to compute time-zone labels.</param>
/// <param name="TimelineStart">The first visible timeline instant.</param>
/// <param name="TimelineEnd">The final visible timeline instant.</param>
/// <param name="TimelineHeight">The rendered timeline height.</param>
/// <param name="ScheduleWidth">The effective schedule canvas width.</param>
internal sealed record DayScheduleTimelineMetrics(
    DateTimeOffset LabelReferenceInstant,
    DateTimeOffset TimelineStart,
    DateTimeOffset TimelineEnd,
    int TimelineHeight,
    int ScheduleWidth);

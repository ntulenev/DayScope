namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents the complete display state for the day schedule dashboard.
/// </summary>
/// <param name="DisplayDate">The selected date being displayed.</param>
/// <param name="MonthTitle">The month heading shown in the UI.</param>
/// <param name="DayTitle">The short day-of-week label.</param>
/// <param name="DayNumberText">The day-of-month label.</param>
/// <param name="DateText">The formatted long date label.</param>
/// <param name="PrimaryTimeZoneLabel">The primary time zone label.</param>
/// <param name="SecondaryTimeZoneLabel">The secondary time zone label, if configured.</param>
/// <param name="PrimaryTimelineHours">The primary time column labels.</param>
/// <param name="SecondaryTimelineHours">The secondary time column labels.</param>
/// <param name="AllDayEvents">The all-day events shown above the timeline.</param>
/// <param name="TimedEvents">The timed events laid out on the timeline.</param>
/// <param name="ScheduleCanvasWidth">The schedule canvas width.</param>
/// <param name="TimelineHeight">The schedule timeline height.</param>
/// <param name="StatusText">The status message shown above the schedule.</param>
/// <param name="ShowStatus">Whether the status message should be visible.</param>
/// <param name="NowLineTop">The vertical position of the current-time marker, if visible.</param>
/// <param name="NowLineText">The current-time label.</param>
public sealed record DayScheduleDisplayState(
    DateOnly DisplayDate,
    string MonthTitle,
    string DayTitle,
    string DayNumberText,
    string DateText,
    string PrimaryTimeZoneLabel,
    string? SecondaryTimeZoneLabel,
    IReadOnlyList<TimelineHourDisplayState> PrimaryTimelineHours,
    IReadOnlyList<TimelineHourDisplayState> SecondaryTimelineHours,
    IReadOnlyList<AllDayEventDisplayState> AllDayEvents,
    IReadOnlyList<TimedEventDisplayState> TimedEvents,
    int ScheduleCanvasWidth,
    int TimelineHeight,
    string StatusText,
    bool ShowStatus,
    double? NowLineTop,
    string NowLineText);

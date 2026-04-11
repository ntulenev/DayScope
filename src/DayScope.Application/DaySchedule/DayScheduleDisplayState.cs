namespace DayScope.Application.DaySchedule;

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

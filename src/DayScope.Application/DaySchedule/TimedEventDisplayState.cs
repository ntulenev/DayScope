namespace DayScope.Application.DaySchedule;

public sealed record TimedEventDisplayState(
    string Title,
    string ScheduleText,
    double Top,
    double Height,
    double Left,
    double Width,
    bool IsCompact,
    bool IsMicro,
    bool ShowScheduleText,
    bool ShowStatusBadge,
    EventAppearance Appearance,
    string StatusLabel,
    string LeadingIcon,
    EventDetailsDisplayState Details);

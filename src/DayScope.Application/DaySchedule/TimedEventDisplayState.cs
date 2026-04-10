namespace DayScope.Application.DaySchedule;

public sealed record TimedEventDisplayState(
    string Title,
    string ScheduleText,
    double Top,
    double Height,
    double Left,
    double Width,
    bool IsCompact,
    EventAppearance Appearance,
    string StatusLabel,
    string LeadingIcon,
    EventDetailsDisplayState Details);

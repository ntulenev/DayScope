namespace DayScope.Application.DaySchedule;

public sealed record AllDayEventDisplayState(
    string Title,
    EventAppearance Appearance,
    string StatusLabel,
    string LeadingIcon);

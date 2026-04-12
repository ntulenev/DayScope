namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents a timed event after clipping and formatting but before collision layout is applied.
/// </summary>
internal sealed record TimedEventLayoutCandidate(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    string ScheduleText,
    int HourHeight,
    EventAppearance Appearance,
    string StatusLabel,
    string LeadingIcon,
    EventDetailsDisplayState Details);

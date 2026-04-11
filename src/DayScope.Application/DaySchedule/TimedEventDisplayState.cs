namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents a timed event laid out on the schedule timeline.
/// </summary>
/// <param name="Title">The event title.</param>
/// <param name="ScheduleText">The formatted event time range.</param>
/// <param name="Top">The top offset within the schedule canvas.</param>
/// <param name="Height">The event card height.</param>
/// <param name="Left">The left offset within the schedule canvas.</param>
/// <param name="Width">The event card width.</param>
/// <param name="IsCompact">Whether the event uses the compact card layout.</param>
/// <param name="IsMicro">Whether the event uses the smallest card layout.</param>
/// <param name="ShowScheduleText">Whether the schedule text should be shown on the card.</param>
/// <param name="ShowStatusBadge">Whether the status badge should be shown on the card.</param>
/// <param name="Appearance">The visual appearance to use.</param>
/// <param name="StatusLabel">The short status label shown on the card.</param>
/// <param name="LeadingIcon">The icon prefix shown before the title.</param>
/// <param name="Details">The details shown when the event is opened.</param>
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

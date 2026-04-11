namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents an all-day event card rendered above the schedule timeline.
/// </summary>
/// <param name="Title">The event title.</param>
/// <param name="Appearance">The visual appearance to use.</param>
/// <param name="StatusLabel">The short status label shown on the card.</param>
/// <param name="LeadingIcon">The icon prefix shown before the title.</param>
/// <param name="Details">The details shown when the event is opened.</param>
public sealed record AllDayEventDisplayState(
    string Title,
    EventAppearance Appearance,
    string StatusLabel,
    string LeadingIcon,
    EventDetailsDisplayState Details);

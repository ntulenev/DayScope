namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents the detailed event information shown in the overlay.
/// </summary>
/// <param name="Title">The event title.</param>
/// <param name="ScheduleText">The formatted event schedule text.</param>
/// <param name="Appearance">The visual appearance to use.</param>
/// <param name="StatusLabel">The participant status label.</param>
/// <param name="LeadingIcon">The icon prefix shown before the title.</param>
/// <param name="Organizer">The organizer display label, if known.</param>
/// <param name="Description">The normalized event description, if any.</param>
/// <param name="JoinUrl">The meeting URL, if any.</param>
/// <param name="Participants">The participant list to display.</param>
public sealed record EventDetailsDisplayState(
    string Title,
    string ScheduleText,
    EventAppearance Appearance,
    string StatusLabel,
    string LeadingIcon,
    string? Organizer,
    string? Description,
    Uri? JoinUrl,
    IReadOnlyList<EventParticipantDisplayState> Participants);

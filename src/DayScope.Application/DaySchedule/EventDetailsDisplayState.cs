namespace DayScope.Application.DaySchedule;

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

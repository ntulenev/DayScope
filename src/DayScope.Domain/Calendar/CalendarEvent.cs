namespace DayScope.Domain.Calendar;

public enum CalendarParticipationStatus
{
    Accepted = 0,
    AwaitingResponse = 1,
    Tentative = 2,
    Declined = 3,
    Cancelled = 4
}

public enum CalendarEventKind
{
    Default = 0,
    FocusTime = 1,
    OutOfOffice = 2,
    WorkingLocation = 3,
    Task = 4,
    AppointmentSchedule = 5
}

public sealed record CalendarEventParticipant(
    string? DisplayName,
    string? Email,
    CalendarParticipationStatus ParticipationStatus,
    bool IsSelf)
{
    public string DisplayLabel =>
        !string.IsNullOrWhiteSpace(DisplayName)
            ? DisplayName.Trim()
            : !string.IsNullOrWhiteSpace(Email)
                ? Email.Trim()
                : "Unknown participant";
}

public sealed record CalendarEvent(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset? End,
    bool IsAllDay,
    CalendarParticipationStatus ParticipationStatus,
    CalendarEventKind EventKind,
    string? OrganizerName,
    string? OrganizerEmail,
    string? Description,
    Uri? JoinUrl,
    IReadOnlyList<CalendarEventParticipant> Participants)
{
    public string SafeTitle => string.IsNullOrWhiteSpace(Title)
        ? "Untitled event"
        : Title;

    public string? OrganizerDisplayLabel =>
        !string.IsNullOrWhiteSpace(OrganizerName)
            ? OrganizerName.Trim()
            : !string.IsNullOrWhiteSpace(OrganizerEmail)
                ? OrganizerEmail.Trim()
                : null;
}

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

public sealed record CalendarEvent(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset? End,
    bool IsAllDay,
    CalendarParticipationStatus ParticipationStatus,
    CalendarEventKind EventKind)
{
    public string SafeTitle => string.IsNullOrWhiteSpace(Title)
        ? "Untitled event"
        : Title;
}

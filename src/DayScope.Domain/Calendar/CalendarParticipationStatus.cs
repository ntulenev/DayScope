namespace DayScope.Domain.Calendar;

/// <summary>
/// Represents the signed-in user's participation state for an event.
/// </summary>
public enum CalendarParticipationStatus
{
    Accepted = 0,
    AwaitingResponse = 1,
    Tentative = 2,
    Declined = 3,
    Cancelled = 4
}

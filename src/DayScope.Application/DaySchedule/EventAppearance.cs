namespace DayScope.Application.DaySchedule;

/// <summary>
/// Represents the visual state used when rendering an event.
/// </summary>
public enum EventAppearance
{
    Accepted = 0,
    AwaitingResponse = 1,
    Tentative = 2,
    Declined = 3,
    Cancelled = 4
}

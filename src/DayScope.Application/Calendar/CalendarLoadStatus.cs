namespace DayScope.Application.Calendar;

/// <summary>
/// Represents the status of a calendar load operation.
/// </summary>
public enum CalendarLoadStatus
{
    Loading = 0,
    Success = 1,
    Disabled = 2,
    ClientSecretsMissing = 3,
    AuthorizationRequired = 4,
    AccessDenied = 5,
    Unavailable = 6,
    NoEvents = 7
}

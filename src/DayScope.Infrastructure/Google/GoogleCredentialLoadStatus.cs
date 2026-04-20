namespace DayScope.Infrastructure.Google;

/// <summary>
/// Represents the outcome of attempting to load Google OAuth credentials.
/// </summary>
public enum GoogleCredentialLoadStatus
{
    Success = 0,
    Disabled = 1,
    ClientSecretsMissing = 2,
    AuthorizationRequired = 3,
    Unavailable = 4
}

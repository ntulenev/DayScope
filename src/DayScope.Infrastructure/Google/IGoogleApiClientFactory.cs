using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Gmail.v1;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Creates configured Google SDK clients from an authorized credential.
/// </summary>
public interface IGoogleApiClientFactory
{
    /// <summary>
    /// Creates a Google Calendar SDK client for the provided credential.
    /// </summary>
    /// <param name="credential">The authorized credential.</param>
    /// <returns>The configured Calendar SDK client.</returns>
    CalendarService CreateCalendarService(UserCredential credential);

    /// <summary>
    /// Creates a Gmail SDK client for the provided credential.
    /// </summary>
    /// <param name="credential">The authorized credential.</param>
    /// <returns>The configured Gmail SDK client.</returns>
    GmailService CreateGmailService(UserCredential credential);
}

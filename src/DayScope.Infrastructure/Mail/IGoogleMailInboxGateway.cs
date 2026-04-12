using Google.Apis.Auth.OAuth2;

namespace DayScope.Infrastructure.Mail;

/// <summary>
/// Executes Gmail API requests used to populate the inbox summary.
/// </summary>
public interface IGoogleMailInboxGateway
{
    /// <summary>
    /// Loads unread-count and profile data for the current Gmail account.
    /// </summary>
    /// <param name="credential">The authorized Google credential.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The raw Gmail inbox data.</returns>
    Task<GoogleMailInboxData> GetInboxDataAsync(
        UserCredential credential,
        CancellationToken cancellationToken);
}

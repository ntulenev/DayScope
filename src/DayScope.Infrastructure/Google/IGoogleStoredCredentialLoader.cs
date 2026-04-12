using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Loads cached Google OAuth credentials from the configured token store.
/// </summary>
public interface IGoogleStoredCredentialLoader
{
    /// <summary>
    /// Loads a previously authorized credential from the token store.
    /// </summary>
    /// <param name="flow">The authorization flow used to access the token store.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The stored credential, or <see langword="null"/> when none exists.</returns>
    Task<UserCredential?> LoadCredentialAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken);
}

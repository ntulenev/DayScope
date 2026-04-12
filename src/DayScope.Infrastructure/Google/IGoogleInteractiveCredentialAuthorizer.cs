using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Performs installed-app OAuth sign-in for Google-backed services.
/// </summary>
public interface IGoogleInteractiveCredentialAuthorizer
{
    /// <summary>
    /// Runs the installed-app OAuth flow and stores the resulting credential.
    /// </summary>
    /// <param name="flow">The authorization flow to use.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The authorized user credential.</returns>
    Task<UserCredential> AuthorizeAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken);
}

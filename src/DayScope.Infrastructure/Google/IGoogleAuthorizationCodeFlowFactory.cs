using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Creates configured Google OAuth authorization flows for installed-app sign-in.
/// </summary>
public interface IGoogleAuthorizationCodeFlowFactory
{
    /// <summary>
    /// Creates the configured authorization flow for the provided client secrets.
    /// </summary>
    /// <param name="clientSecrets">The Google OAuth client secrets.</param>
    /// <returns>The configured authorization flow.</returns>
    GoogleAuthorizationCodeFlow CreateAuthorizationFlow(ClientSecrets clientSecrets);
}

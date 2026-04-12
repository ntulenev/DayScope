using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Performs installed-app OAuth sign-in by using Google's local-server receiver.
/// </summary>
public sealed class GoogleInstalledAppAuthorizer : IGoogleInteractiveCredentialAuthorizer
{
    /// <inheritdoc />
    public async Task<UserCredential> AuthorizeAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(flow);

        var authApp = new AuthorizationCodeInstalledApp(
            flow,
            new LocalServerCodeReceiver());
        return await authApp.AuthorizeAsync(
            GoogleOAuthDefaults.TokenStoreUserId,
            cancellationToken);
    }
}

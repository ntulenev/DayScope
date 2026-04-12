using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Loads cached Google OAuth credentials from the token store.
/// </summary>
public sealed class GoogleStoredCredentialLoader : IGoogleStoredCredentialLoader
{
    /// <inheritdoc />
    public async Task<UserCredential?> LoadCredentialAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(flow);

        var token = await flow.LoadTokenAsync(
            GoogleOAuthDefaults.TokenStoreUserId,
            cancellationToken);
        return token is null
            ? null
            : new UserCredential(flow, GoogleOAuthDefaults.TokenStoreUserId, token);
    }
}

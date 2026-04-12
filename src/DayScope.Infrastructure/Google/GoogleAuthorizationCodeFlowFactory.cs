using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Util.Store;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Creates configured Google OAuth authorization flows for the application.
/// </summary>
public sealed class GoogleAuthorizationCodeFlowFactory : IGoogleAuthorizationCodeFlowFactory
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleAuthorizationCodeFlowFactory"/> class.
    /// </summary>
    /// <param name="settings">The configured Google integration settings.</param>
    /// <param name="tokenStoreDirectoryProvider">The provider for the OAuth token-store directory.</param>
    public GoogleAuthorizationCodeFlowFactory(
        IOptions<GoogleCalendarSettings> settings,
        IGoogleTokenStoreDirectoryProvider tokenStoreDirectoryProvider)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tokenStoreDirectoryProvider);

        _settings = settings.Value;
        _tokenStoreDirectoryProvider = tokenStoreDirectoryProvider;
    }

    /// <inheritdoc />
    public GoogleAuthorizationCodeFlow CreateAuthorizationFlow(ClientSecrets clientSecrets)
    {
        ArgumentNullException.ThrowIfNull(clientSecrets);

        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            DataStore = new FileDataStore(_tokenStoreDirectoryProvider.GetTokenStoreDirectory(), true),
            Scopes = GoogleOAuthDefaults.Scopes,
            LoginHint = string.IsNullOrWhiteSpace(_settings.LoginHint) ? null : _settings.LoginHint,
            Prompt = _settings.ForceAccountSelection ? "select_account" : null
        };

        return new PkceGoogleAuthorizationCodeFlow(initializer);
    }

    private readonly GoogleCalendarSettings _settings;
    private readonly IGoogleTokenStoreDirectoryProvider _tokenStoreDirectoryProvider;
}

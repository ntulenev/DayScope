using Google.Apis.Auth.OAuth2.Responses;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Coordinates Google OAuth credential acquisition by delegating file-system and OAuth details.
/// </summary>
public sealed class GoogleCredentialProvider : IGoogleCredentialProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCredentialProvider"/> class.
    /// </summary>
    /// <param name="settings">The configured Google integration settings.</param>
    /// <param name="clientSecretsLoader">The loader used to read Google OAuth client secrets.</param>
    /// <param name="authorizationCodeFlowFactory">The factory used to create OAuth flows.</param>
    /// <param name="storedCredentialLoader">The loader used to read cached credentials.</param>
    /// <param name="interactiveCredentialAuthorizer">The authorizer used for interactive sign-in.</param>
    public GoogleCredentialProvider(
        IOptions<GoogleCalendarSettings> settings,
        IGoogleClientSecretsLoader clientSecretsLoader,
        IGoogleAuthorizationCodeFlowFactory authorizationCodeFlowFactory,
        IGoogleStoredCredentialLoader storedCredentialLoader,
        IGoogleInteractiveCredentialAuthorizer interactiveCredentialAuthorizer)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(clientSecretsLoader);
        ArgumentNullException.ThrowIfNull(authorizationCodeFlowFactory);
        ArgumentNullException.ThrowIfNull(storedCredentialLoader);
        ArgumentNullException.ThrowIfNull(interactiveCredentialAuthorizer);

        _settings = settings.Value;
        _clientSecretsLoader = clientSecretsLoader;
        _authorizationCodeFlowFactory = authorizationCodeFlowFactory;
        _storedCredentialLoader = storedCredentialLoader;
        _interactiveCredentialAuthorizer = interactiveCredentialAuthorizer;
    }

    /// <inheritdoc />
    public bool IsEnabled => _settings.Enabled;

    /// <inheritdoc />
    public async Task<GoogleCredentialLoadResult> GetCredentialAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return GoogleCredentialLoadResult.Disabled();
        }

        var clientSecretsResult = _clientSecretsLoader.LoadClientSecrets();
        if (clientSecretsResult.ClientSecrets is null)
        {
            return GoogleCredentialLoadResult.ClientSecretsMissing();
        }

        try
        {
            var flow = _authorizationCodeFlowFactory.CreateAuthorizationFlow(
                clientSecretsResult.ClientSecrets);
            var credential = allowInteractiveAuthentication
                ? await _interactiveCredentialAuthorizer.AuthorizeAsync(flow, cancellationToken)
                : await _storedCredentialLoader.LoadCredentialAsync(flow, cancellationToken);

            return credential is null
                ? GoogleCredentialLoadResult.AuthorizationRequired()
                : GoogleCredentialLoadResult.Success(credential);
        }
        catch (TokenResponseException)
        {
            return GoogleCredentialLoadResult.AuthorizationRequired();
        }
        catch (Exception ex) when (GoogleConnectivityFailureDetector.IsConnectivityFailure(ex))
        {
            return GoogleCredentialLoadResult.Unavailable();
        }
    }

    private readonly GoogleCalendarSettings _settings;
    private readonly IGoogleClientSecretsLoader _clientSecretsLoader;
    private readonly IGoogleAuthorizationCodeFlowFactory _authorizationCodeFlowFactory;
    private readonly IGoogleStoredCredentialLoader _storedCredentialLoader;
    private readonly IGoogleInteractiveCredentialAuthorizer _interactiveCredentialAuthorizer;
}

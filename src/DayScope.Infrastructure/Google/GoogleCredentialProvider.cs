using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Loads and refreshes Google OAuth credentials used by infrastructure services.
/// </summary>
public sealed class GoogleCredentialProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCredentialProvider"/> class.
    /// </summary>
    /// <param name="settings">The configured Google integration settings.</param>
    public GoogleCredentialProvider(IOptions<GoogleCalendarSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings.Value;
    }

    internal bool IsEnabled => _settings.Enabled;

    /// <summary>
    /// Loads a cached Google credential and optionally performs interactive authorization when required.
    /// </summary>
    /// <param name="allowInteractiveAuthentication">Whether the user may be prompted to sign in.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The credential load result.</returns>
    internal async Task<GoogleCredentialLoadResult> GetCredentialAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return GoogleCredentialLoadResult.Disabled();
        }

        var clientSecretsPath = PathResolver.ResolvePath(_settings.ClientSecretsPath);
        if (string.IsNullOrWhiteSpace(clientSecretsPath) || !File.Exists(clientSecretsPath))
        {
            return GoogleCredentialLoadResult.ClientSecretsMissing();
        }

        try
        {
            using var stream = File.OpenRead(clientSecretsPath);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            var flow = CreateAuthorizationFlow(clientSecrets);
            var credential = allowInteractiveAuthentication
                ? await AuthorizeInteractivelyAsync(flow, cancellationToken)
                : await LoadCredentialAsync(flow, cancellationToken);

            return credential is null
                ? GoogleCredentialLoadResult.AuthorizationRequired()
                : GoogleCredentialLoadResult.Success(credential);
        }
        catch (TokenResponseException)
        {
            return GoogleCredentialLoadResult.AuthorizationRequired();
        }
        catch (TaskCanceledException)
        {
            return GoogleCredentialLoadResult.AuthorizationRequired();
        }
    }

    /// <summary>
    /// Creates the OAuth authorization flow configured for the application token store and scopes.
    /// </summary>
    /// <param name="clientSecrets">The Google OAuth client secrets.</param>
    /// <returns>The configured authorization flow.</returns>
    private GoogleAuthorizationCodeFlow CreateAuthorizationFlow(ClientSecrets clientSecrets)
    {
        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            DataStore = new FileDataStore(GetTokenStoreDirectory(), true),
            Scopes = _scopes,
            LoginHint = string.IsNullOrWhiteSpace(_settings.LoginHint) ? null : _settings.LoginHint,
            Prompt = _settings.ForceAccountSelection ? "select_account" : null
        };

        return new PkceGoogleAuthorizationCodeFlow(initializer);
    }

    /// <summary>
    /// Runs the installed-app OAuth flow and stores the resulting credential.
    /// </summary>
    /// <param name="flow">The authorization flow to use.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The authorized user credential.</returns>
    private static async Task<UserCredential> AuthorizeInteractivelyAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken)
    {
        var authApp = new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver());
        return await authApp.AuthorizeAsync(TOKEN_STORE_USER_ID, cancellationToken);
    }

    /// <summary>
    /// Loads a previously authorized credential from the token store.
    /// </summary>
    /// <param name="flow">The authorization flow used to access the token store.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The stored credential, or <see langword="null"/> when none exists.</returns>
    private static async Task<UserCredential?> LoadCredentialAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken)
    {
        var token = await flow.LoadTokenAsync(TOKEN_STORE_USER_ID, cancellationToken);
        return token is null
            ? null
            : new UserCredential(flow, TOKEN_STORE_USER_ID, token);
    }

    /// <summary>
    /// Resolves the directory used to persist Google OAuth tokens.
    /// </summary>
    /// <returns>The absolute token store directory path.</returns>
    private string GetTokenStoreDirectory()
    {
        var configuredPath = PathResolver.ResolvePath(_settings.TokenStoreDirectory);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            Directory.CreateDirectory(configuredPath);
            return configuredPath;
        }

        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DayScope",
            "GoogleCalendarToken");
        Directory.CreateDirectory(fallbackPath);
        return fallbackPath;
    }

    private const string TOKEN_STORE_USER_ID = "dayscope-google-services";
    private static readonly string[] _scopes =
    [
        CalendarService.Scope.CalendarReadonly,
        GmailService.Scope.GmailReadonly
    ];

    private readonly GoogleCalendarSettings _settings;
}

/// <summary>
/// Represents the outcome of attempting to load Google OAuth credentials.
/// </summary>
internal enum GoogleCredentialLoadStatus
{
    Success = 0,
    Disabled = 1,
    ClientSecretsMissing = 2,
    AuthorizationRequired = 3
}

/// <summary>
/// Carries the result of a Google credential load attempt.
/// </summary>
/// <param name="Status">The credential load status.</param>
/// <param name="Credential">The loaded credential, when available.</param>
internal sealed record GoogleCredentialLoadResult(
    GoogleCredentialLoadStatus Status,
    UserCredential? Credential)
{
    /// <summary>
    /// Creates a successful result that contains a usable credential.
    /// </summary>
    /// <param name="credential">The loaded credential.</param>
    /// <returns>The successful load result.</returns>
    internal static GoogleCredentialLoadResult Success(UserCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        return new GoogleCredentialLoadResult(GoogleCredentialLoadStatus.Success, credential);
    }

    /// <summary>
    /// Creates a result that indicates Google integration is disabled.
    /// </summary>
    /// <returns>The disabled result.</returns>
    internal static GoogleCredentialLoadResult Disabled() =>
        new(GoogleCredentialLoadStatus.Disabled, null);

    /// <summary>
    /// Creates a result that indicates the OAuth client secrets file is missing.
    /// </summary>
    /// <returns>The missing-client-secrets result.</returns>
    internal static GoogleCredentialLoadResult ClientSecretsMissing() =>
        new(GoogleCredentialLoadStatus.ClientSecretsMissing, null);

    /// <summary>
    /// Creates a result that indicates the user must authorize the application.
    /// </summary>
    /// <returns>The authorization-required result.</returns>
    internal static GoogleCredentialLoadResult AuthorizationRequired() =>
        new(GoogleCredentialLoadStatus.AuthorizationRequired, null);
}

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

public sealed class GoogleCredentialProvider
{
    public GoogleCredentialProvider(IOptions<GoogleCalendarSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings.Value;
    }

    internal bool IsEnabled => _settings.Enabled;

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

    private static async Task<UserCredential> AuthorizeInteractivelyAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken)
    {
        var authApp = new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver());
        return await authApp.AuthorizeAsync(TOKEN_STORE_USER_ID, cancellationToken);
    }

    private static async Task<UserCredential?> LoadCredentialAsync(
        GoogleAuthorizationCodeFlow flow,
        CancellationToken cancellationToken)
    {
        var token = await flow.LoadTokenAsync(TOKEN_STORE_USER_ID, cancellationToken);
        return token is null
            ? null
            : new UserCredential(flow, TOKEN_STORE_USER_ID, token);
    }

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

internal enum GoogleCredentialLoadStatus
{
    Success = 0,
    Disabled = 1,
    ClientSecretsMissing = 2,
    AuthorizationRequired = 3
}

internal sealed record GoogleCredentialLoadResult(
    GoogleCredentialLoadStatus Status,
    UserCredential? Credential)
{
    public static GoogleCredentialLoadResult Success(UserCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        return new GoogleCredentialLoadResult(GoogleCredentialLoadStatus.Success, credential);
    }

    public static GoogleCredentialLoadResult Disabled() =>
        new(GoogleCredentialLoadStatus.Disabled, null);

    public static GoogleCredentialLoadResult ClientSecretsMissing() =>
        new(GoogleCredentialLoadStatus.ClientSecretsMissing, null);

    public static GoogleCredentialLoadResult AuthorizationRequired() =>
        new(GoogleCredentialLoadStatus.AuthorizationRequired, null);
}

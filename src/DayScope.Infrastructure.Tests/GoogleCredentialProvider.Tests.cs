using System.Runtime.CompilerServices;

using FluentAssertions;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;

using Microsoft.Extensions.Options;

using Moq;

using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Tests;

public sealed class GoogleCredentialProviderTests
{
    [Fact(DisplayName = "Credential loading returns disabled when Google integration is disabled.")]
    [Trait("Category", "Unit")]
    public async Task GetCredentialAsyncShouldReturnDisabledWhenGoogleIntegrationIsDisabled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var provider = CreateCredentialProvider(settings: new GoogleCalendarSettings { Enabled = false });

        // Act
        var result = await provider.GetCredentialAsync(true, cancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(GoogleCredentialLoadStatus.Disabled);
        result.Credential.Should().BeNull();
    }

    [Fact(DisplayName = "Credential loading returns client-secrets-missing when the secrets loader cannot load them.")]
    [Trait("Category", "Unit")]
    public async Task GetCredentialAsyncShouldReturnClientSecretsMissingWhenSecretsAreUnavailable()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var secretsLoader = new Mock<IGoogleClientSecretsLoader>(MockBehavior.Strict);
        secretsLoader.Setup(loader => loader.LoadClientSecrets())
            .Returns(GoogleClientSecretsLoadResult.Missing());
        var provider = CreateCredentialProvider(clientSecretsLoader: secretsLoader.Object);

        // Act
        var result = await provider.GetCredentialAsync(true, cancellationTokenSource.Token);

        // Assert
        result.Status.Should().Be(GoogleCredentialLoadStatus.ClientSecretsMissing);
        result.Credential.Should().BeNull();
    }

    [Fact(DisplayName = "Credential loading uses the stored credential loader when interactive authentication is disabled.")]
    [Trait("Category", "Unit")]
    public async Task GetCredentialAsyncShouldUseStoredCredentialLoaderWhenInteractiveAuthenticationIsDisabled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var clientSecrets = new ClientSecrets { ClientId = "client", ClientSecret = "secret" };
        var flow = CreateOpaqueAuthorizationFlow();
        var credential = CreateCredential();
        var flowFactoryCalls = 0;
        var storedLoaderCalls = 0;
        var clientSecretsLoader = new Mock<IGoogleClientSecretsLoader>(MockBehavior.Strict);
        clientSecretsLoader.Setup(loader => loader.LoadClientSecrets())
            .Returns(GoogleClientSecretsLoadResult.Success(clientSecrets));
        var flowFactory = new Mock<IGoogleAuthorizationCodeFlowFactory>(MockBehavior.Strict);
        flowFactory.Setup(factory => factory.CreateAuthorizationFlow(clientSecrets))
            .Callback(() => flowFactoryCalls++)
            .Returns(flow);
        var storedCredentialLoader = new Mock<IGoogleStoredCredentialLoader>(MockBehavior.Strict);
        storedCredentialLoader.Setup(loader => loader.LoadCredentialAsync(
                It.IsAny<GoogleAuthorizationCodeFlow>(),
                token))
            .Callback(() => storedLoaderCalls++)
            .ReturnsAsync(credential);
        var interactiveAuthorizer = new Mock<IGoogleInteractiveCredentialAuthorizer>(MockBehavior.Strict);
        var provider = CreateCredentialProvider(
            clientSecretsLoader.Object,
            flowFactory.Object,
            storedCredentialLoader.Object,
            interactiveAuthorizer.Object);

        // Act
        var result = await provider.GetCredentialAsync(false, token);

        // Assert
        result.Status.Should().Be(GoogleCredentialLoadStatus.Success);
        result.Credential.Should().BeSameAs(credential);
        flowFactoryCalls.Should().Be(1);
        storedLoaderCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Credential loading uses the interactive authorizer when interactive authentication is enabled.")]
    [Trait("Category", "Unit")]
    public async Task GetCredentialAsyncShouldUseInteractiveAuthorizerWhenInteractiveAuthenticationIsEnabled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var clientSecrets = new ClientSecrets { ClientId = "client", ClientSecret = "secret" };
        var flow = CreateOpaqueAuthorizationFlow();
        var credential = CreateCredential();
        var clientSecretsLoader = new Mock<IGoogleClientSecretsLoader>(MockBehavior.Strict);
        clientSecretsLoader.Setup(loader => loader.LoadClientSecrets())
            .Returns(GoogleClientSecretsLoadResult.Success(clientSecrets));
        var flowFactory = new Mock<IGoogleAuthorizationCodeFlowFactory>(MockBehavior.Strict);
        flowFactory.Setup(factory => factory.CreateAuthorizationFlow(clientSecrets))
            .Returns(flow);
        var storedCredentialLoader = new Mock<IGoogleStoredCredentialLoader>(MockBehavior.Strict);
        var interactiveAuthorizer = new Mock<IGoogleInteractiveCredentialAuthorizer>(MockBehavior.Strict);
        interactiveAuthorizer.Setup(authorizer => authorizer.AuthorizeAsync(
                It.IsAny<GoogleAuthorizationCodeFlow>(),
                token))
            .ReturnsAsync(credential);
        var provider = CreateCredentialProvider(
            clientSecretsLoader.Object,
            flowFactory.Object,
            storedCredentialLoader.Object,
            interactiveAuthorizer.Object);

        // Act
        var result = await provider.GetCredentialAsync(true, token);

        // Assert
        result.Status.Should().Be(GoogleCredentialLoadStatus.Success);
        result.Credential.Should().BeSameAs(credential);
    }

    [Fact(DisplayName = "Credential loading returns unavailable when the OAuth flow times out.")]
    [Trait("Category", "Unit")]
    public async Task GetCredentialAsyncShouldReturnUnavailableWhenTheOAuthFlowTimesOut()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var clientSecrets = new ClientSecrets { ClientId = "client", ClientSecret = "secret" };
        var flow = CreateOpaqueAuthorizationFlow();
        var clientSecretsLoader = new Mock<IGoogleClientSecretsLoader>(MockBehavior.Strict);
        clientSecretsLoader.Setup(loader => loader.LoadClientSecrets())
            .Returns(GoogleClientSecretsLoadResult.Success(clientSecrets));
        var flowFactory = new Mock<IGoogleAuthorizationCodeFlowFactory>(MockBehavior.Strict);
        flowFactory.Setup(factory => factory.CreateAuthorizationFlow(clientSecrets))
            .Returns(flow);
        var storedCredentialLoader = new Mock<IGoogleStoredCredentialLoader>(MockBehavior.Strict);
        storedCredentialLoader.Setup(loader => loader.LoadCredentialAsync(
                It.IsAny<GoogleAuthorizationCodeFlow>(),
                token))
            .ThrowsAsync(new TaskCanceledException());
        var interactiveAuthorizer = new Mock<IGoogleInteractiveCredentialAuthorizer>(MockBehavior.Strict);
        var provider = CreateCredentialProvider(
            clientSecretsLoader.Object,
            flowFactory.Object,
            storedCredentialLoader.Object,
            interactiveAuthorizer.Object);

        // Act
        var result = await provider.GetCredentialAsync(false, token);

        // Assert
        result.Status.Should().Be(GoogleCredentialLoadStatus.Unavailable);
        result.Credential.Should().BeNull();
    }

    [Fact(DisplayName = "Credential loading returns authorization required when Google reports an invalid token response.")]
    [Trait("Category", "Unit")]
    public async Task GetCredentialAsyncShouldReturnAuthorizationRequiredWhenGoogleReportsInvalidTokenResponse()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var clientSecrets = new ClientSecrets { ClientId = "client", ClientSecret = "secret" };
        var flow = CreateOpaqueAuthorizationFlow();
        var clientSecretsLoader = new Mock<IGoogleClientSecretsLoader>(MockBehavior.Strict);
        clientSecretsLoader.Setup(loader => loader.LoadClientSecrets())
            .Returns(GoogleClientSecretsLoadResult.Success(clientSecrets));
        var flowFactory = new Mock<IGoogleAuthorizationCodeFlowFactory>(MockBehavior.Strict);
        flowFactory.Setup(factory => factory.CreateAuthorizationFlow(clientSecrets))
            .Returns(flow);
        var storedCredentialLoader = new Mock<IGoogleStoredCredentialLoader>(MockBehavior.Strict);
        var interactiveAuthorizer = new Mock<IGoogleInteractiveCredentialAuthorizer>(MockBehavior.Strict);
        interactiveAuthorizer.Setup(authorizer => authorizer.AuthorizeAsync(
                It.IsAny<GoogleAuthorizationCodeFlow>(),
                token))
            .ThrowsAsync(new TokenResponseException(new TokenErrorResponse { Error = "invalid_grant" }));
        var provider = CreateCredentialProvider(
            clientSecretsLoader.Object,
            flowFactory.Object,
            storedCredentialLoader.Object,
            interactiveAuthorizer.Object);

        // Act
        var result = await provider.GetCredentialAsync(true, token);

        // Assert
        result.Status.Should().Be(GoogleCredentialLoadStatus.AuthorizationRequired);
        result.Credential.Should().BeNull();
    }

    [Fact(DisplayName = "Client secrets loading returns missing when the resolved path is blank or absent.")]
    [Trait("Category", "Unit")]
    public void LoadClientSecretsShouldReturnMissingWhenTheResolvedPathIsBlankOrAbsent()
    {
        // Arrange
        var blankPathResolver = new Mock<IPathResolver>(MockBehavior.Strict);
        blankPathResolver.Setup(resolver => resolver.ResolvePath("configured"))
            .Returns(string.Empty);
        var missingPathResolver = new Mock<IPathResolver>(MockBehavior.Strict);
        missingPathResolver.Setup(resolver => resolver.ResolvePath("configured"))
            .Returns(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "client-secrets.json"));

        var blankLoader = new GoogleClientSecretsLoader(
            Options.Create(new GoogleCalendarSettings { ClientSecretsPath = "configured" }),
            blankPathResolver.Object);
        var missingLoader = new GoogleClientSecretsLoader(
            Options.Create(new GoogleCalendarSettings { ClientSecretsPath = "configured" }),
            missingPathResolver.Object);

        // Act
        var blankResult = blankLoader.LoadClientSecrets();
        var missingResult = missingLoader.LoadClientSecrets();

        // Assert
        blankResult.ClientSecrets.Should().BeNull();
        missingResult.ClientSecrets.Should().BeNull();
    }

    [Fact(DisplayName = "Client secrets loading reads the configured JSON file when it exists.")]
    [Trait("Category", "Unit")]
    public void LoadClientSecretsShouldReadTheConfiguredJsonFileWhenItExists()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), "DayScope.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var secretsPath = Path.Combine(tempDirectory, "client-secrets.json");
        File.WriteAllText(
            secretsPath,
            """
            {
              "installed": {
                "client_id": "client-id",
                "project_id": "dayscope-tests",
                "auth_uri": "https://accounts.google.com/o/oauth2/auth",
                "token_uri": "https://oauth2.googleapis.com/token",
                "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
                "client_secret": "client-secret",
                "redirect_uris": [ "http://localhost" ]
              }
            }
            """);
        var pathResolver = new Mock<IPathResolver>(MockBehavior.Strict);
        pathResolver.Setup(resolver => resolver.ResolvePath("configured"))
            .Returns(secretsPath);
        var loader = new GoogleClientSecretsLoader(
            Options.Create(new GoogleCalendarSettings { ClientSecretsPath = "configured" }),
            pathResolver.Object);

        try
        {
            // Act
            var result = loader.LoadClientSecrets();

            // Assert
            result.ClientSecrets.Should().NotBeNull();
            result.ClientSecrets!.ClientId.Should().Be("client-id");
            result.ClientSecrets.ClientSecret.Should().Be("client-secret");
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    [Fact(DisplayName = "Stored credential loading returns null when the token store has no cached token.")]
    [Trait("Category", "Unit")]
    public async Task LoadCredentialAsyncShouldReturnNullWhenTheTokenStoreHasNoCachedToken()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var dataStore = new RecordingDataStore();
        using var flow = CreateAuthorizationFlow(dataStore);
        var loader = new GoogleStoredCredentialLoader();

        // Act
        var result = await loader.LoadCredentialAsync(flow, token);

        // Assert
        result.Should().BeNull();
        dataStore.LastRequestedKey.Should().Be("dayscope-google-services");
    }

    [Fact(DisplayName = "Stored credential loading returns a user credential when the token store has a cached token.")]
    [Trait("Category", "Unit")]
    public async Task LoadCredentialAsyncShouldReturnAUserCredentialWhenTheTokenStoreHasACachedToken()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var cachedToken = new TokenResponse { AccessToken = "access-token" };
        var dataStore = new RecordingDataStore(cachedToken);
        using var flow = CreateAuthorizationFlow(dataStore);
        var loader = new GoogleStoredCredentialLoader();

        // Act
        var result = await loader.LoadCredentialAsync(flow, token);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().BeSameAs(cachedToken);
        dataStore.LastRequestedKey.Should().Be("dayscope-google-services");
    }

    private static GoogleCredentialProvider CreateCredentialProvider(
        IGoogleClientSecretsLoader? clientSecretsLoader = null,
        IGoogleAuthorizationCodeFlowFactory? authorizationCodeFlowFactory = null,
        IGoogleStoredCredentialLoader? storedCredentialLoader = null,
        IGoogleInteractiveCredentialAuthorizer? interactiveCredentialAuthorizer = null,
        GoogleCalendarSettings? settings = null)
    {
        return new GoogleCredentialProvider(
            Options.Create(settings ?? new GoogleCalendarSettings()),
            clientSecretsLoader ?? CreateDefaultSecretsLoader(),
            authorizationCodeFlowFactory ?? CreateDefaultFlowFactory(),
            storedCredentialLoader ?? CreateDefaultStoredCredentialLoader(),
            interactiveCredentialAuthorizer ?? CreateDefaultInteractiveAuthorizer());
    }

    private static IGoogleClientSecretsLoader CreateDefaultSecretsLoader()
    {
        var loader = new Mock<IGoogleClientSecretsLoader>(MockBehavior.Strict);
        loader.Setup(instance => instance.LoadClientSecrets())
            .Returns(GoogleClientSecretsLoadResult.Missing());

        return loader.Object;
    }

    private static IGoogleAuthorizationCodeFlowFactory CreateDefaultFlowFactory()
        => new Mock<IGoogleAuthorizationCodeFlowFactory>(MockBehavior.Strict).Object;

    private static IGoogleStoredCredentialLoader CreateDefaultStoredCredentialLoader()
        => new Mock<IGoogleStoredCredentialLoader>(MockBehavior.Strict).Object;

    private static IGoogleInteractiveCredentialAuthorizer CreateDefaultInteractiveAuthorizer()
        => new Mock<IGoogleInteractiveCredentialAuthorizer>(MockBehavior.Strict).Object;

    private static GoogleAuthorizationCodeFlow CreateAuthorizationFlow(IDataStore? dataStore = null)
    {
        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = "client-id",
                ClientSecret = "client-secret"
            },
            Scopes = ["scope"],
            DataStore = dataStore ?? new RecordingDataStore()
        });
    }

    private static GoogleAuthorizationCodeFlow CreateOpaqueAuthorizationFlow()
        => (GoogleAuthorizationCodeFlow)RuntimeHelpers.GetUninitializedObject(typeof(GoogleAuthorizationCodeFlow));

    private static UserCredential CreateCredential()
        => (UserCredential)RuntimeHelpers.GetUninitializedObject(typeof(UserCredential));

    private sealed class RecordingDataStore(TokenResponse? token = null) : IDataStore
    {
        public string? LastRequestedKey { get; private set; }

        public Task ClearAsync() => Task.CompletedTask;

        public Task DeleteAsync<T>(string key)
        {
            LastRequestedKey = key;
            return Task.CompletedTask;
        }

        public Task<T> GetAsync<T>(string key)
        {
            LastRequestedKey = key;
            return Task.FromResult((T)(object?)token!);
        }

        public Task StoreAsync<T>(string key, T value)
        {
            LastRequestedKey = key;
            return Task.CompletedTask;
        }
    }
}

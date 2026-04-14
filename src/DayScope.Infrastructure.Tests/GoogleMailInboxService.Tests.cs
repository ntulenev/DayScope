using System.Runtime.CompilerServices;

using FluentAssertions;

using Google;
using Google.Apis.Auth.OAuth2;

using Moq;

using DayScope.Application.Abstractions;
using DayScope.Infrastructure.Google;
using DayScope.Infrastructure.Mail;

namespace DayScope.Infrastructure.Tests;

public sealed class GoogleMailInboxServiceTests
{
    [Fact(DisplayName = "The constructor throws when the credential provider is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenCredentialProviderIsNull()
    {
        // Arrange
        var mailInboxGateway = new Mock<IGoogleMailInboxGateway>(MockBehavior.Strict).Object;
        var workspaceUriBuilder = new Mock<IGoogleWorkspaceUriBuilder>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleMailInboxService(
            null!,
            mailInboxGateway,
            workspaceUriBuilder);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the mail inbox gateway is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenMailInboxGatewayIsNull()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict).Object;
        var workspaceUriBuilder = new Mock<IGoogleWorkspaceUriBuilder>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleMailInboxService(
            credentialProvider,
            null!,
            workspaceUriBuilder);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the workspace URI builder is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenWorkspaceUriBuilderIsNull()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict).Object;
        var mailInboxGateway = new Mock<IGoogleMailInboxGateway>(MockBehavior.Strict).Object;

        // Act
        var action = () => new GoogleMailInboxService(
            credentialProvider,
            mailInboxGateway,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The enabled state mirrors the credential provider.")]
    [Trait("Category", "Unit")]
    public void IsEnabledShouldMirrorCredentialProvider()
    {
        // Arrange
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.SetupGet(provider => provider.IsEnabled)
            .Returns(true);
        var service = new GoogleMailInboxService(
            credentialProvider.Object,
            new Mock<IGoogleMailInboxGateway>(MockBehavior.Strict).Object,
            new RecordingWorkspaceUriBuilder());

        // Act
        var isEnabled = service.IsEnabled;

        // Assert
        isEnabled.Should().BeTrue();
    }

    [Fact(DisplayName = "Inbox loading returns an empty snapshot when the credential provider does not return a usable credential.")]
    [Trait("Category", "Unit")]
    public async Task GetInboxSnapshotAsyncShouldReturnEmptySnapshotWhenCredentialIsUnavailable()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var credentialRequests = 0;
        var workspaceUriBuilder = new RecordingWorkspaceUriBuilder
        {
            BuildInboxUriHandler = emailAddress =>
            {
                emailAddress.Should().BeNull();
                return new Uri("https://mail.google.com/mail/");
            }
        };
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                token))
            .Callback(() => credentialRequests++)
            .ReturnsAsync(GoogleCredentialLoadResult.AuthorizationRequired());
        var service = new GoogleMailInboxService(
            credentialProvider.Object,
            new Mock<IGoogleMailInboxGateway>(MockBehavior.Strict).Object,
            workspaceUriBuilder);

        // Act
        var snapshot = await service.GetInboxSnapshotAsync(true, token);

        // Assert
        snapshot.UnreadCount.Should().BeNull();
        snapshot.EmailAddress.Should().BeNull();
        snapshot.InboxUri.Should().Be(new Uri("https://mail.google.com/mail/"));
        credentialRequests.Should().Be(1);
        workspaceUriBuilder.BuildInboxUriCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Inbox loading returns normalized inbox data when Gmail data is available.")]
    [Trait("Category", "Unit")]
    public async Task GetInboxSnapshotAsyncShouldReturnNormalizedInboxDataWhenGmailDataIsAvailable()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var credential = CreateCredential();
        var credentialRequests = 0;
        var inboxGatewayRequests = 0;
        var workspaceUriBuilder = new RecordingWorkspaceUriBuilder
        {
            BuildInboxUriHandler = emailAddress =>
            {
                emailAddress.Should().Be("user@example.com");
                return new Uri("https://mail.google.com/mail/u/?authuser=user%40example.com#inbox");
            }
        };
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                false,
                token))
            .Callback(() => credentialRequests++)
            .ReturnsAsync(GoogleCredentialLoadResult.Success(credential));
        var mailInboxGateway = new Mock<IGoogleMailInboxGateway>(MockBehavior.Strict);
        mailInboxGateway.Setup(gateway => gateway.GetInboxDataAsync(
                credential,
                token))
            .Callback(() => inboxGatewayRequests++)
            .ReturnsAsync(new GoogleMailInboxData(12, " user@example.com "));
        var service = new GoogleMailInboxService(
            credentialProvider.Object,
            mailInboxGateway.Object,
            workspaceUriBuilder);

        // Act
        var snapshot = await service.GetInboxSnapshotAsync(false, token);

        // Assert
        snapshot.UnreadCount.Should().Be(12);
        snapshot.EmailAddress.Should().Be("user@example.com");
        snapshot.InboxUri.Should().Be(new Uri("https://mail.google.com/mail/u/?authuser=user%40example.com#inbox"));
        credentialRequests.Should().Be(1);
        inboxGatewayRequests.Should().Be(1);
        workspaceUriBuilder.BuildInboxUriCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Inbox loading returns an empty snapshot when Gmail returns an API failure.")]
    [Trait("Category", "Unit")]
    public async Task GetInboxSnapshotAsyncShouldReturnEmptySnapshotWhenGmailReturnsApiFailure()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var credential = CreateCredential();
        var workspaceUriBuilder = new RecordingWorkspaceUriBuilder
        {
            BuildInboxUriHandler = emailAddress =>
            {
                emailAddress.Should().BeNull();
                return new Uri("https://mail.google.com/mail/");
            }
        };
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                token))
            .ReturnsAsync(GoogleCredentialLoadResult.Success(credential));
        var mailInboxGateway = new Mock<IGoogleMailInboxGateway>(MockBehavior.Strict);
        mailInboxGateway.Setup(gateway => gateway.GetInboxDataAsync(
                credential,
                token))
            .ThrowsAsync(new GoogleApiException("Gmail", "Denied"));
        var service = new GoogleMailInboxService(
            credentialProvider.Object,
            mailInboxGateway.Object,
            workspaceUriBuilder);

        // Act
        var snapshot = await service.GetInboxSnapshotAsync(true, token);

        // Assert
        snapshot.UnreadCount.Should().BeNull();
        snapshot.EmailAddress.Should().BeNull();
        snapshot.InboxUri.Should().Be(new Uri("https://mail.google.com/mail/"));
    }

    [Fact(DisplayName = "Inbox loading returns an empty snapshot when Gmail is canceled.")]
    [Trait("Category", "Unit")]
    public async Task GetInboxSnapshotAsyncShouldReturnEmptySnapshotWhenGmailIsCanceled()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var credential = CreateCredential();
        var workspaceUriBuilder = new RecordingWorkspaceUriBuilder
        {
            BuildInboxUriHandler = emailAddress =>
            {
                emailAddress.Should().BeNull();
                return new Uri("https://mail.google.com/mail/");
            }
        };
        var credentialProvider = new Mock<IGoogleCredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(provider => provider.GetCredentialAsync(
                true,
                token))
            .ReturnsAsync(GoogleCredentialLoadResult.Success(credential));
        var mailInboxGateway = new Mock<IGoogleMailInboxGateway>(MockBehavior.Strict);
        mailInboxGateway.Setup(gateway => gateway.GetInboxDataAsync(
                credential,
                token))
            .ThrowsAsync(new TaskCanceledException());
        var service = new GoogleMailInboxService(
            credentialProvider.Object,
            mailInboxGateway.Object,
            workspaceUriBuilder);

        // Act
        var snapshot = await service.GetInboxSnapshotAsync(true, token);

        // Assert
        snapshot.UnreadCount.Should().BeNull();
        snapshot.EmailAddress.Should().BeNull();
        snapshot.InboxUri.Should().Be(new Uri("https://mail.google.com/mail/"));
    }

    private static UserCredential CreateCredential()
        => (UserCredential)RuntimeHelpers.GetUninitializedObject(typeof(UserCredential));

    private sealed class RecordingWorkspaceUriBuilder : IGoogleWorkspaceUriBuilder
    {
        public int BuildInboxUriCalls { get; private set; }

        public Func<string?, Uri> BuildInboxUriHandler { get; init; } =
            static _ => new Uri("https://mail.google.com/mail/");

        public Uri BuildCalendarDayUri(DateOnly displayDate, string? emailAddress) =>
            throw new NotSupportedException();

        public Uri BuildInboxUri(string? emailAddress)
        {
            BuildInboxUriCalls++;
            return BuildInboxUriHandler(emailAddress);
        }
    }
}

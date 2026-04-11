using Google;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

using DayScope.Application.Abstractions;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Mail;

/// <summary>
/// Loads unread inbox data from the Gmail API.
/// </summary>
public sealed class GoogleMailInboxService : IEmailInboxService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMailInboxService"/> class.
    /// </summary>
    /// <param name="credentialProvider">The credential provider used to authorize API calls.</param>
    /// <param name="workspaceUriBuilder">The builder used to create account-aware Gmail links.</param>
    public GoogleMailInboxService(
        GoogleCredentialProvider credentialProvider,
        IGoogleWorkspaceUriBuilder workspaceUriBuilder)
    {
        ArgumentNullException.ThrowIfNull(credentialProvider);
        ArgumentNullException.ThrowIfNull(workspaceUriBuilder);

        _credentialProvider = credentialProvider;
        _workspaceUriBuilder = workspaceUriBuilder;
    }

    public bool IsEnabled => _credentialProvider.IsEnabled;

    /// <summary>
    /// Loads the current unread Gmail snapshot.
    /// </summary>
    /// <param name="allowInteractiveAuthentication">Whether the request may prompt the user to sign in.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The current inbox snapshot.</returns>
    public async Task<EmailInboxSnapshot> GetInboxSnapshotAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken)
    {
        var credentialResult = await _credentialProvider.GetCredentialAsync(
            allowInteractiveAuthentication,
            cancellationToken);
        if (credentialResult.Status != GoogleCredentialLoadStatus.Success ||
            credentialResult.Credential is null)
        {
            return CreateSnapshot(null, null);
        }

        try
        {
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentialResult.Credential,
                ApplicationName = "DayScope"
            });

            var profileRequest = service.Users.GetProfile(GMAIL_USER_ID);
            profileRequest.Fields = "emailAddress";
            var profile = await profileRequest.ExecuteAsync(cancellationToken);

            var request = service.Users.Labels.Get(GMAIL_USER_ID, INBOX_LABEL_ID);
            request.Fields = "threadsUnread";

            var inboxLabel = await request.ExecuteAsync(cancellationToken);
            return CreateSnapshot(
                Math.Max(0, inboxLabel.ThreadsUnread ?? 0),
                profile.EmailAddress);
        }
        catch (GoogleApiException)
        {
            return CreateSnapshot(null, null);
        }
        catch (TaskCanceledException)
        {
            return CreateSnapshot(null, null);
        }
    }

    /// <summary>
    /// Builds the inbox snapshot returned to the application layer.
    /// </summary>
    /// <param name="unreadCount">The unread thread count, if available.</param>
    /// <param name="emailAddress">The signed-in Gmail address, if known.</param>
    /// <returns>The normalized inbox snapshot.</returns>
    private EmailInboxSnapshot CreateSnapshot(
        int? unreadCount,
        string? emailAddress)
    {
        var normalizedEmailAddress = string.IsNullOrWhiteSpace(emailAddress)
            ? null
            : emailAddress.Trim();

        return new EmailInboxSnapshot(
            unreadCount,
            normalizedEmailAddress,
            _workspaceUriBuilder.BuildInboxUri(normalizedEmailAddress));
    }

    private const string GMAIL_USER_ID = "me";
    private const string INBOX_LABEL_ID = "INBOX";

    private readonly GoogleCredentialProvider _credentialProvider;
    private readonly IGoogleWorkspaceUriBuilder _workspaceUriBuilder;
}

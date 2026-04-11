using Google;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

using DayScope.Application.Abstractions;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Mail;

public sealed class GoogleMailInboxService : IEmailInboxService
{
    public GoogleMailInboxService(GoogleCredentialProvider credentialProvider)
    {
        ArgumentNullException.ThrowIfNull(credentialProvider);

        _credentialProvider = credentialProvider;
    }

    public bool IsEnabled => _credentialProvider.IsEnabled;

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

    private static EmailInboxSnapshot CreateSnapshot(
        int? unreadCount,
        string? emailAddress)
    {
        var normalizedEmailAddress = string.IsNullOrWhiteSpace(emailAddress)
            ? null
            : emailAddress.Trim();

        return new EmailInboxSnapshot(
            unreadCount,
            normalizedEmailAddress,
            BuildInboxUri(normalizedEmailAddress));
    }

    private static Uri BuildInboxUri(string? emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return _defaultInboxUri;
        }

        // Inference from current Google web behavior: authuser accepts the signed-in account email.
        return new Uri(
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "https://mail.google.com/mail/u/?authuser={0}#inbox",
                Uri.EscapeDataString(emailAddress)),
            UriKind.Absolute);
    }

    private const string GMAIL_USER_ID = "me";
    private const string INBOX_LABEL_ID = "INBOX";
    private static readonly Uri _defaultInboxUri = new("https://mail.google.com/mail/");

    private readonly GoogleCredentialProvider _credentialProvider;
}

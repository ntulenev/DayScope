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

    public async Task<int?> GetUnreadEmailCountAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken)
    {
        var credentialResult = await _credentialProvider.GetCredentialAsync(
            allowInteractiveAuthentication,
            cancellationToken);
        if (credentialResult.Status != GoogleCredentialLoadStatus.Success ||
            credentialResult.Credential is null)
        {
            return null;
        }

        try
        {
            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentialResult.Credential,
                ApplicationName = "DayScope"
            });

            var request = service.Users.Labels.Get(GMAIL_USER_ID, INBOX_LABEL_ID);
            request.Fields = "messagesUnread";

            var inboxLabel = await request.ExecuteAsync(cancellationToken);
            return Math.Max(0, inboxLabel.MessagesUnread ?? 0);
        }
        catch (GoogleApiException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private const string GMAIL_USER_ID = "me";
    private const string INBOX_LABEL_ID = "INBOX";

    private readonly GoogleCredentialProvider _credentialProvider;
}

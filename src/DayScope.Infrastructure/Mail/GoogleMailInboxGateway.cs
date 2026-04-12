using Google.Apis.Auth.OAuth2;

using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Mail;

/// <summary>
/// Wraps Gmail SDK requests used to populate the inbox summary.
/// </summary>
public sealed class GoogleMailInboxGateway : IGoogleMailInboxGateway
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleMailInboxGateway"/> class.
    /// </summary>
    /// <param name="googleApiClientFactory">The factory used to create configured Google SDK clients.</param>
    public GoogleMailInboxGateway(IGoogleApiClientFactory googleApiClientFactory)
    {
        ArgumentNullException.ThrowIfNull(googleApiClientFactory);

        _googleApiClientFactory = googleApiClientFactory;
    }

    /// <inheritdoc />
    public async Task<GoogleMailInboxData> GetInboxDataAsync(
        UserCredential credential,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credential);

        var service = _googleApiClientFactory.CreateGmailService(credential);

        var profileRequest = service.Users.GetProfile(GMAIL_USER_ID);
        profileRequest.Fields = "emailAddress";
        var profile = await profileRequest.ExecuteAsync(cancellationToken);

        var request = service.Users.Labels.Get(GMAIL_USER_ID, INBOX_LABEL_ID);
        request.Fields = "threadsUnread";

        var inboxLabel = await request.ExecuteAsync(cancellationToken);
        return new GoogleMailInboxData(
            Math.Max(0, inboxLabel.ThreadsUnread ?? 0),
            profile.EmailAddress);
    }

    private const string GMAIL_USER_ID = "me";
    private const string INBOX_LABEL_ID = "INBOX";

    private readonly IGoogleApiClientFactory _googleApiClientFactory;
}

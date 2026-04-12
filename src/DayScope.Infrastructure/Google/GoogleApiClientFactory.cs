using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Creates configured Google SDK clients from an authorized credential.
/// </summary>
public sealed class GoogleApiClientFactory : IGoogleApiClientFactory
{
    /// <inheritdoc />
    public CalendarService CreateCalendarService(UserCredential credential) =>
        new(CreateInitializer(credential));

    /// <inheritdoc />
    public GmailService CreateGmailService(UserCredential credential) =>
        new(CreateInitializer(credential));

    private static BaseClientService.Initializer CreateInitializer(UserCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        return new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = GoogleOAuthDefaults.ApplicationName
        };
    }
}

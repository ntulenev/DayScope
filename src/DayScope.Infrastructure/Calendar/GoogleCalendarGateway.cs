using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Calendar;

/// <summary>
/// Wraps Google Calendar SDK requests used by the infrastructure layer.
/// </summary>
public sealed class GoogleCalendarGateway : IGoogleCalendarGateway
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCalendarGateway"/> class.
    /// </summary>
    /// <param name="googleApiClientFactory">The factory used to create configured Google SDK clients.</param>
    public GoogleCalendarGateway(IGoogleApiClientFactory googleApiClientFactory)
    {
        ArgumentNullException.ThrowIfNull(googleApiClientFactory);

        _googleApiClientFactory = googleApiClientFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Event>> GetEventsAsync(
        UserCredential credential,
        string calendarId,
        DateTimeOffset startOfDay,
        DateTimeOffset endOfDay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarId);

        var service = _googleApiClientFactory.CreateCalendarService(credential);
        var request = service.Events.List(calendarId);
        request.TimeMinDateTimeOffset = startOfDay.ToUniversalTime();
        request.TimeMaxDateTimeOffset = endOfDay.ToUniversalTime();
        request.MaxResults = 250;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.ShowDeleted = true;
        request.SingleEvents = true;
        request.Fields =
            "items(summary,description,start,end,status,eventType,hangoutLink," +
            "attendees(displayName,email,self,responseStatus),organizer(displayName,email,self))";

        var events = await request.ExecuteAsync(cancellationToken);
        return events.Items?.ToArray() ?? [];
    }

    private readonly IGoogleApiClientFactory _googleApiClientFactory;
}

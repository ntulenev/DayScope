using System.Globalization;

using Google;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;

using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Domain.Calendar;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Calendar;

/// <summary>
/// Loads calendar data from the Google Calendar API.
/// </summary>
public sealed class GoogleCalendarService : ICalendarService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleCalendarService"/> class.
    /// </summary>
    /// <param name="settings">The configured Google integration settings.</param>
    /// <param name="credentialProvider">The credential provider used to authorize API calls.</param>
    /// <param name="googleApiClientFactory">The factory used to create configured Google SDK clients.</param>
    public GoogleCalendarService(
        IOptions<GoogleCalendarSettings> settings,
        IGoogleCredentialProvider credentialProvider,
        IGoogleApiClientFactory googleApiClientFactory)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(credentialProvider);
        ArgumentNullException.ThrowIfNull(googleApiClientFactory);

        _settings = settings.Value;
        _credentialProvider = credentialProvider;
        _googleApiClientFactory = googleApiClientFactory;
    }

    public bool IsEnabled => _credentialProvider.IsEnabled;

    /// <summary>
    /// Loads Google Calendar events for the requested day.
    /// </summary>
    /// <param name="day">The day to load.</param>
    /// <param name="timeZone">The time zone that defines the requested day boundaries.</param>
    /// <param name="interactionMode">Whether the request may use interactive authentication.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The result of the calendar load operation.</returns>
    public async Task<CalendarLoadResult> GetEventsForDateAsync(
        DateOnly day,
        TimeZoneInfo timeZone,
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (!IsEnabled)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.Disabled);
        }

        var credentialResult = await _credentialProvider.GetCredentialAsync(
            interactionMode == CalendarInteractionMode.Interactive,
            cancellationToken);
        if (credentialResult.Status == GoogleCredentialLoadStatus.Disabled)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.Disabled);
        }

        if (credentialResult.Status == GoogleCredentialLoadStatus.ClientSecretsMissing)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.ClientSecretsMissing);
        }

        if (credentialResult.Credential is null)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRequired);
        }

        try
        {
            var service = _googleApiClientFactory.CreateCalendarService(
                credentialResult.Credential);

            var startOfDay = CreateDateTimeOffset(timeZone, day, 0);
            var startOfNextDay = CreateDateTimeOffset(timeZone, day.AddDays(1), 0);

            var request = service.Events.List(_settings.CalendarId);
            request.TimeMinDateTimeOffset = startOfDay.ToUniversalTime();
            request.TimeMaxDateTimeOffset = startOfNextDay.ToUniversalTime();
            request.MaxResults = 250;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            request.ShowDeleted = true;
            request.SingleEvents = true;
            request.Fields =
                "items(summary,description,start,end,status,eventType,hangoutLink," +
                "attendees(displayName,email,self,responseStatus),organizer(displayName,email,self))";

            var events = await request.ExecuteAsync(cancellationToken);
            var agendaItems = events.Items?
                .Select(item => MapEvent(item, timeZone))
                .Where(item => item is not null)
                .Cast<CalendarEvent>()
                .Where(item => item.Intersects(startOfDay, startOfNextDay))
                .OrderBy(item => item.Start)
                .ToArray()
                ?? [];

            return agendaItems.Length > 0
                ? CalendarLoadResult.Success(new CalendarAgenda(agendaItems))
                : CalendarLoadResult.FromStatus(CalendarLoadStatus.NoEvents);
        }
        catch (TokenResponseException)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRequired);
        }
        catch (GoogleApiException)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.AccessDenied);
        }
        catch (TaskCanceledException)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRequired);
        }
        catch
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.Unavailable);
        }
    }

    /// <summary>
    /// Maps a Google Calendar API event into the normalized domain model.
    /// </summary>
    /// <param name="calendarEvent">The API event.</param>
    /// <param name="timeZone">The fallback time zone for all-day values.</param>
    /// <returns>The normalized event, or <see langword="null"/> when the source event cannot be represented.</returns>
    private static CalendarEvent? MapEvent(
        Event calendarEvent,
        TimeZoneInfo timeZone)
    {
        if (calendarEvent.Start is null)
        {
            return null;
        }

        if (string.Equals(calendarEvent.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var isAllDay = !string.IsNullOrWhiteSpace(calendarEvent.Start.Date);
        var start = ResolveEventDateTime(calendarEvent.Start, timeZone);
        if (start is null)
        {
            return null;
        }

        var end = ResolveEventDateTime(calendarEvent.End, timeZone);
        return new CalendarEvent(
            string.IsNullOrWhiteSpace(calendarEvent.Summary)
                ? "Untitled event"
                : calendarEvent.Summary,
            start.Value,
            end,
            isAllDay,
            ResolveParticipationStatus(calendarEvent),
            ResolveEventKind(calendarEvent),
            calendarEvent.Organizer?.DisplayName,
            calendarEvent.Organizer?.Email,
            calendarEvent.Description,
            ResolveJoinUrl(calendarEvent),
            calendarEvent.Attendees?
                .Select(MapParticipant)
                .Where(participant => participant is not null)
                .Cast<CalendarEventParticipant>()
                .ToArray()
                ?? []);
    }

    /// <summary>
    /// Maps a Google attendee into the normalized participant model.
    /// </summary>
    /// <param name="attendee">The attendee to map.</param>
    /// <returns>The normalized participant, or <see langword="null"/> when the attendee is missing.</returns>
    private static CalendarEventParticipant? MapParticipant(EventAttendee? attendee)
    {
        if (attendee is null)
        {
            return null;
        }

        return new CalendarEventParticipant(
            attendee.DisplayName,
            attendee.Email,
            ResolveParticipationStatus(attendee.ResponseStatus),
            attendee.Self is true);
    }

    /// <summary>
    /// Maps a Google attendee response status to the domain participation status.
    /// </summary>
    /// <param name="responseStatus">The Google response status value.</param>
    /// <returns>The normalized participation status.</returns>
    private static CalendarParticipationStatus ResolveParticipationStatus(string? responseStatus)
    {
        return responseStatus?.ToUpperInvariant() switch
        {
            "NEEDSACTION" => CalendarParticipationStatus.AwaitingResponse,
            "TENTATIVE" => CalendarParticipationStatus.Tentative,
            "DECLINED" => CalendarParticipationStatus.Declined,
            "ACCEPTED" => CalendarParticipationStatus.Accepted,
            _ => CalendarParticipationStatus.Accepted
        };
    }

    /// <summary>
    /// Resolves the signed-in user's participation status for a Google event.
    /// </summary>
    /// <param name="calendarEvent">The source API event.</param>
    /// <returns>The normalized participation status.</returns>
    private static CalendarParticipationStatus ResolveParticipationStatus(Event calendarEvent)
    {
        if (string.Equals(calendarEvent.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return CalendarParticipationStatus.Cancelled;
        }

        var selfAttendee = calendarEvent.Attendees?.FirstOrDefault(attendee => attendee.Self is true);
        var selfResponseStatus = selfAttendee?.ResponseStatus;
        if (string.IsNullOrWhiteSpace(selfResponseStatus) && calendarEvent.Organizer?.Self is true)
        {
            selfResponseStatus = "accepted";
        }

        return ResolveParticipationStatus(selfResponseStatus);
    }

    /// <summary>
    /// Resolves the meeting link for a Google event when one is available.
    /// </summary>
    /// <param name="calendarEvent">The source API event.</param>
    /// <returns>The validated join URI, or <see langword="null"/> when absent or invalid.</returns>
    private static Uri? ResolveJoinUrl(Event calendarEvent)
    {
        if (!string.IsNullOrWhiteSpace(calendarEvent.HangoutLink))
        {
            return Uri.TryCreate(calendarEvent.HangoutLink, UriKind.Absolute, out var joinUri)
                ? joinUri
                : null;
        }

        return null;
    }

    /// <summary>
    /// Maps the Google event type to the normalized domain event kind.
    /// </summary>
    /// <param name="calendarEvent">The source API event.</param>
    /// <returns>The normalized event kind.</returns>
    private static CalendarEventKind ResolveEventKind(Event calendarEvent)
    {
        return calendarEvent.EventType switch
        {
            "focusTime" => CalendarEventKind.FocusTime,
            "outOfOffice" => CalendarEventKind.OutOfOffice,
            "workingLocation" => CalendarEventKind.WorkingLocation,
            "task" => CalendarEventKind.Task,
            "appointmentSchedule" => CalendarEventKind.AppointmentSchedule,
            _ => CalendarEventKind.Default
        };
    }

    /// <summary>
    /// Resolves a Google event date-time payload into a concrete instant.
    /// </summary>
    /// <param name="eventDateTime">The Google date-time payload.</param>
    /// <param name="timeZone">The fallback time zone for all-day dates.</param>
    /// <returns>The resolved instant, or <see langword="null"/> when the payload is invalid.</returns>
    private static DateTimeOffset? ResolveEventDateTime(
        EventDateTime? eventDateTime,
        TimeZoneInfo timeZone)
    {
        if (eventDateTime?.DateTimeDateTimeOffset is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }

        if (!string.IsNullOrWhiteSpace(eventDateTime?.DateTimeRaw) &&
            DateTimeOffset.TryParse(
                eventDateTime.DateTimeRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedDateTimeOffset))
        {
            return parsedDateTimeOffset;
        }

        if (string.IsNullOrWhiteSpace(eventDateTime?.Date))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(
                eventDateTime.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return null;
        }

        var localDateTime = parsedDate.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime));
    }

    /// <summary>
    /// Creates a local start-of-hour instant for the requested date in the target time zone.
    /// </summary>
    /// <param name="timeZone">The time zone that defines the offset.</param>
    /// <param name="date">The local date.</param>
    /// <param name="hour">The local hour.</param>
    /// <returns>The resulting <see cref="DateTimeOffset"/>.</returns>
    private static DateTimeOffset CreateDateTimeOffset(
        TimeZoneInfo timeZone,
        DateOnly date,
        int hour)
    {
        var dateTime = date.ToDateTime(new TimeOnly(hour, 0));
        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }

    private readonly GoogleCalendarSettings _settings;
    private readonly IGoogleCredentialProvider _credentialProvider;
    private readonly IGoogleApiClientFactory _googleApiClientFactory;
}

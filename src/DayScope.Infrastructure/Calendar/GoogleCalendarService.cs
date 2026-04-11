using System.Globalization;

using Google;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Domain.Calendar;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Google;

namespace DayScope.Infrastructure.Calendar;

public sealed class GoogleCalendarService : ICalendarService
{
    public GoogleCalendarService(
        IOptions<GoogleCalendarSettings> settings,
        GoogleCredentialProvider credentialProvider)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(credentialProvider);

        _settings = settings.Value;
        _credentialProvider = credentialProvider;
    }

    public bool IsEnabled => _credentialProvider.IsEnabled;

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
            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credentialResult.Credential,
                ApplicationName = "DayScope"
            });

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
                .Where(item => IntersectsDay(item, startOfDay, startOfNextDay))
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

    private static bool IntersectsDay(
        CalendarEvent calendarEvent,
        DateTimeOffset startOfDay,
        DateTimeOffset startOfNextDay)
    {
        var eventEnd = calendarEvent.End ?? calendarEvent.Start.AddMinutes(30);
        return calendarEvent.Start < startOfNextDay && eventEnd > startOfDay;
    }

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

    private static DateTimeOffset CreateDateTimeOffset(
        TimeZoneInfo timeZone,
        DateOnly date,
        int hour)
    {
        var dateTime = date.ToDateTime(new TimeOnly(hour, 0));
        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }

    private readonly GoogleCalendarSettings _settings;
    private readonly GoogleCredentialProvider _credentialProvider;
}

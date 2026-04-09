using System.Globalization;

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Domain.Calendar;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;

namespace DayScope.Infrastructure.Calendar;

public sealed class GoogleCalendarService : ICalendarService
{
    public GoogleCalendarService(IOptions<GoogleCalendarSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings.Value;
    }

    public bool IsEnabled => _settings.Enabled;

    public async Task<CalendarLoadResult> GetEventsForDateAsync(
        DateOnly date,
        TimeZoneInfo timeZone,
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.Disabled);
        }

        var clientSecretsPath = PathResolver.ResolvePath(_settings.ClientSecretsPath);
        if (string.IsNullOrWhiteSpace(clientSecretsPath) || !File.Exists(clientSecretsPath))
        {
            return CalendarLoadResult.FromStatus(CalendarLoadStatus.ClientSecretsMissing);
        }

        try
        {
            using var stream = File.OpenRead(clientSecretsPath);
            var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
            var flow = CreateAuthorizationFlow(clientSecrets);
            var credential = await GetCredentialAsync(flow, interactionMode, cancellationToken);
            if (credential is null)
            {
                return CalendarLoadResult.FromStatus(CalendarLoadStatus.AuthorizationRequired);
            }

            var service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "DayScope"
            });

            var startOfDay = CreateDateTimeOffset(timeZone, date, 0);
            var startOfNextDay = CreateDateTimeOffset(timeZone, date.AddDays(1), 0);

            var request = service.Events.List(_settings.CalendarId);
            request.TimeMinDateTimeOffset = startOfDay.ToUniversalTime();
            request.TimeMaxDateTimeOffset = startOfNextDay.ToUniversalTime();
            request.MaxResults = 250;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            request.ShowDeleted = true;
            request.SingleEvents = true;
            request.Fields =
                "items(summary,start,end,status,eventType,attendees(self,responseStatus),organizer(self))";

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

    private GoogleAuthorizationCodeFlow CreateAuthorizationFlow(ClientSecrets clientSecrets)
    {
        var initializer = new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            DataStore = new FileDataStore(GetTokenStoreDirectory(), true),
            Scopes = _scopes,
            LoginHint = string.IsNullOrWhiteSpace(_settings.LoginHint) ? null : _settings.LoginHint,
            Prompt = _settings.ForceAccountSelection ? "select_account" : null
        };

        return new PkceGoogleAuthorizationCodeFlow(initializer);
    }

    private static async Task<UserCredential?> GetCredentialAsync(
        GoogleAuthorizationCodeFlow flow,
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken)
    {
        if (interactionMode == CalendarInteractionMode.Interactive)
        {
            var authApp = new AuthorizationCodeInstalledApp(flow, new LocalServerCodeReceiver());
            return await authApp.AuthorizeAsync(TOKEN_STORE_USER_ID, cancellationToken);
        }

        var token = await flow.LoadTokenAsync(TOKEN_STORE_USER_ID, cancellationToken);
        return token is null
            ? null
            : new UserCredential(flow, TOKEN_STORE_USER_ID, token);
    }

    private string GetTokenStoreDirectory()
    {
        var configuredPath = PathResolver.ResolvePath(_settings.TokenStoreDirectory);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            Directory.CreateDirectory(configuredPath);
            return configuredPath;
        }

        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DayScope",
            "GoogleCalendarToken");
        Directory.CreateDirectory(fallbackPath);
        return fallbackPath;
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
            ResolveEventKind(calendarEvent));
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

        return selfResponseStatus?.ToLowerInvariant() switch
        {
            "needsaction" => CalendarParticipationStatus.AwaitingResponse,
            "tentative" => CalendarParticipationStatus.Tentative,
            "declined" => CalendarParticipationStatus.Declined,
            "accepted" => CalendarParticipationStatus.Accepted,
            _ => CalendarParticipationStatus.Accepted
        };
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

    private const string TOKEN_STORE_USER_ID = "dayscope-google-calendar";
    private static readonly string[] _scopes =
    [
        CalendarService.Scope.CalendarReadonly
    ];

    private readonly GoogleCalendarSettings _settings;
}

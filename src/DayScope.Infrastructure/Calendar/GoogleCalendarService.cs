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
    /// <param name="calendarGateway">The gateway used to execute Google Calendar SDK requests.</param>
    /// <param name="calendarEventMapper">The mapper used to convert SDK events into domain events.</param>
    /// <param name="calendarFailureMapper">The mapper used to translate Google failures into load statuses.</param>
    public GoogleCalendarService(
        IOptions<GoogleCalendarSettings> settings,
        IGoogleCredentialProvider credentialProvider,
        IGoogleCalendarGateway calendarGateway,
        IGoogleCalendarEventMapper calendarEventMapper,
        IGoogleCalendarFailureMapper calendarFailureMapper)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(credentialProvider);
        ArgumentNullException.ThrowIfNull(calendarGateway);
        ArgumentNullException.ThrowIfNull(calendarEventMapper);
        ArgumentNullException.ThrowIfNull(calendarFailureMapper);

        _settings = settings.Value;
        _credentialProvider = credentialProvider;
        _calendarGateway = calendarGateway;
        _calendarEventMapper = calendarEventMapper;
        _calendarFailureMapper = calendarFailureMapper;
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
            var startOfDay = CreateDateTimeOffset(timeZone, day, 0);
            var startOfNextDay = CreateDateTimeOffset(timeZone, day.AddDays(1), 0);
            var events = await _calendarGateway.GetEventsAsync(
                credentialResult.Credential,
                _settings.CalendarId,
                startOfDay,
                startOfNextDay,
                cancellationToken);

            var agendaItems = events
                .Select(item => _calendarEventMapper.MapEvent(item, timeZone))
                .Where(item => item is not null)
                .Cast<CalendarEvent>()
                .Where(item => item.Intersects(startOfDay, startOfNextDay))
                .OrderBy(item => item.Start)
                .ToArray();

            return agendaItems.Length > 0
                ? CalendarLoadResult.Success(new CalendarAgenda(agendaItems))
                : CalendarLoadResult.FromStatus(CalendarLoadStatus.NoEvents);
        }
        catch (Exception ex)
        {
            return CalendarLoadResult.FromStatus(_calendarFailureMapper.Map(ex));
        }
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
    private readonly IGoogleCalendarGateway _calendarGateway;
    private readonly IGoogleCalendarEventMapper _calendarEventMapper;
    private readonly IGoogleCalendarFailureMapper _calendarFailureMapper;
}

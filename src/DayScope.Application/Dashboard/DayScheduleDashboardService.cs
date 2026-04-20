using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.DaySchedule;
using DayScope.Domain.Configuration;

namespace DayScope.Application.Dashboard;

/// <summary>
/// Orchestrates calendar loading and conversion into the dashboard display model.
/// </summary>
public sealed class DayScheduleDashboardService
    : IDayScheduleDashboardService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DayScheduleDashboardService"/> class.
    /// </summary>
    /// <param name="clockService">The clock used to determine the current day.</param>
    /// <param name="calendarService">The calendar data source.</param>
    /// <param name="localTimeZoneProvider">The provider for the local display time zone.</param>
    /// <param name="scheduleOptions">The day schedule display options.</param>
    /// <param name="googleCalendarOptions">The calendar refresh options.</param>
    public DayScheduleDashboardService(
        IClockService clockService,
        ICalendarService calendarService,
        ILocalTimeZoneProvider localTimeZoneProvider,
        IOptions<DayScheduleSettings> scheduleOptions,
        IOptions<GoogleCalendarSettings> googleCalendarOptions)
    {
        ArgumentNullException.ThrowIfNull(clockService);
        ArgumentNullException.ThrowIfNull(calendarService);
        ArgumentNullException.ThrowIfNull(localTimeZoneProvider);
        ArgumentNullException.ThrowIfNull(scheduleOptions);
        ArgumentNullException.ThrowIfNull(googleCalendarOptions);

        _clockService = clockService;
        _calendarService = calendarService;
        _localTimeZone = localTimeZoneProvider.LocalTimeZone;
        _scheduleSettings = scheduleOptions.Value;
        _googleCalendarSettings = googleCalendarOptions.Value;
        _selectedDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(_clockService.Now, _localTimeZone).DateTime);
    }

    public bool IsCalendarEnabled => _calendarService.IsEnabled;

    public TimeSpan CalendarRefreshInterval =>
        TimeSpan.FromMinutes(_googleCalendarSettings.RefreshMinutes);

    /// <summary>
    /// Builds the current display state from the last loaded agenda.
    /// </summary>
    /// <param name="availableScheduleWidth">The available width for the schedule canvas, if known.</param>
    /// <returns>The current display state.</returns>
    public DayScheduleDisplayState GetCurrentDisplayState(double? availableScheduleWidth = null) =>
        DayScheduleDisplayBuilder.Build(
            _lastLoadResult,
            _scheduleSettings,
            _clockService.Now,
            _selectedDate,
            _localTimeZone,
            availableScheduleWidth);

    /// <summary>
    /// Moves the selected date by the specified number of days.
    /// </summary>
    /// <param name="dayOffset">The number of days to move backward or forward.</param>
    public void ShiftSelectedDate(int dayOffset)
    {
        if (dayOffset == 0)
        {
            return;
        }

        _selectedDate = _selectedDate.AddDays(dayOffset);
        _lastLoadResult = CalendarLoadResult.FromStatus(CalendarLoadStatus.Loading);
    }

    /// <summary>
    /// Refreshes the selected day from the calendar service and returns the resulting display state.
    /// </summary>
    /// <param name="interactionMode">Whether interactive authentication is allowed.</param>
    /// <param name="availableScheduleWidth">The available width for the schedule canvas, if known.</param>
    /// <param name="cancellationToken">The cancellation token for the refresh.</param>
    /// <returns>The refreshed display state.</returns>
    public async Task<DayScheduleDisplayState> RefreshCalendarAsync(
        CalendarInteractionMode interactionMode,
        double? availableScheduleWidth,
        CancellationToken cancellationToken)
    {
        if (_isRefreshing)
        {
            return GetCurrentDisplayState(availableScheduleWidth);
        }

        _isRefreshing = true;

        try
        {
            var loadResult = await _calendarService.GetEventsForDateAsync(
                _selectedDate,
                _localTimeZone,
                interactionMode,
                cancellationToken);
            _lastLoadResult = ShouldReuseLastSuccessfulAgenda(loadResult)
                ? new CalendarLoadResult(_lastSuccessfulLoadResult!.Agenda, CalendarLoadStatus.Unavailable)
                : loadResult;

            if (loadResult.Status is CalendarLoadStatus.Success or CalendarLoadStatus.NoEvents)
            {
                _lastSuccessfulLoadResult = loadResult;
                _lastSuccessfulDate = _selectedDate;
            }

            return GetCurrentDisplayState(availableScheduleWidth);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private bool ShouldReuseLastSuccessfulAgenda(CalendarLoadResult loadResult)
    {
        ArgumentNullException.ThrowIfNull(loadResult);

        return loadResult.Status == CalendarLoadStatus.Unavailable &&
            _lastSuccessfulLoadResult is not null &&
            _lastSuccessfulDate == _selectedDate;
    }

    private readonly IClockService _clockService;
    private readonly ICalendarService _calendarService;
    private readonly TimeZoneInfo _localTimeZone;
    private readonly DayScheduleSettings _scheduleSettings;
    private readonly GoogleCalendarSettings _googleCalendarSettings;

    private CalendarLoadResult _lastLoadResult =
        CalendarLoadResult.FromStatus(CalendarLoadStatus.Loading);
    private CalendarLoadResult? _lastSuccessfulLoadResult;
    private DateOnly? _lastSuccessfulDate;
    private DateOnly _selectedDate;
    private bool _isRefreshing;
}

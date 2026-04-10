using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.DaySchedule;
using DayScope.Domain.Configuration;

namespace DayScope.Application.Dashboard;

public sealed class DayScheduleDashboardService
{
    public DayScheduleDashboardService(
        IClockService clockService,
        ICalendarService calendarService,
        IOptions<DayScheduleSettings> scheduleOptions,
        IOptions<GoogleCalendarSettings> googleCalendarOptions)
    {
        ArgumentNullException.ThrowIfNull(clockService);
        ArgumentNullException.ThrowIfNull(calendarService);
        ArgumentNullException.ThrowIfNull(scheduleOptions);
        ArgumentNullException.ThrowIfNull(googleCalendarOptions);

        _clockService = clockService;
        _calendarService = calendarService;
        _scheduleSettings = scheduleOptions.Value;
        _googleCalendarSettings = googleCalendarOptions.Value;
    }

    public bool IsCalendarEnabled => _calendarService.IsEnabled;

    public TimeSpan CalendarRefreshInterval =>
        TimeSpan.FromMinutes(_googleCalendarSettings.RefreshMinutes);

    public DayScheduleDisplayState GetCurrentDisplayState(double? availableScheduleWidth = null) =>
        DayScheduleDisplayBuilder.Build(
            _lastLoadResult,
            _scheduleSettings,
            _clockService.Now,
            availableScheduleWidth);

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
            var localDate = DateOnly.FromDateTime(_clockService.Now.LocalDateTime);
            _lastLoadResult = await _calendarService.GetEventsForDateAsync(
                localDate,
                TimeZoneInfo.Local,
                interactionMode,
                cancellationToken);

            return GetCurrentDisplayState(availableScheduleWidth);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    private readonly IClockService _clockService;
    private readonly ICalendarService _calendarService;
    private readonly DayScheduleSettings _scheduleSettings;
    private readonly GoogleCalendarSettings _googleCalendarSettings;

    private CalendarLoadResult _lastLoadResult =
        CalendarLoadResult.FromStatus(CalendarLoadStatus.Loading);
    private bool _isRefreshing;
}

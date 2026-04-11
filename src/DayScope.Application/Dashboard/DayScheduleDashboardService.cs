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
        _selectedDate = DateOnly.FromDateTime(_clockService.Now.LocalDateTime);
    }

    public bool IsCalendarEnabled => _calendarService.IsEnabled;

    public TimeSpan CalendarRefreshInterval =>
        TimeSpan.FromMinutes(_googleCalendarSettings.RefreshMinutes);

    public DayScheduleDisplayState GetCurrentDisplayState(double? availableScheduleWidth = null) =>
        DayScheduleDisplayBuilder.Build(
            _lastLoadResult,
            _scheduleSettings,
            _clockService.Now,
            _selectedDate,
            availableScheduleWidth);

    public void ShiftSelectedDate(int dayOffset)
    {
        if (dayOffset == 0)
        {
            return;
        }

        _selectedDate = _selectedDate.AddDays(dayOffset);
        _lastLoadResult = CalendarLoadResult.FromStatus(CalendarLoadStatus.Loading);
    }

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
            _lastLoadResult = await _calendarService.GetEventsForDateAsync(
                _selectedDate,
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
    private DateOnly _selectedDate;
    private bool _isRefreshing;
}

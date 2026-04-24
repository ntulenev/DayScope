using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.Dashboard;
using DayScope.Application.DaySchedule;
using DayScope.Threading;

namespace DayScope.ViewModels;

/// <summary>
/// Coordinates timer-driven refresh and navigation for the main window dashboard.
/// </summary>
public sealed class MainWindowDashboardCoordinator : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowDashboardCoordinator"/> class.
    /// </summary>
    /// <param name="dashboardService">The service that builds schedule display state.</param>
    /// <param name="emailInboxService">The service that loads unread email data.</param>
    /// <param name="timerFactory">The factory used to create UI timers.</param>
    public MainWindowDashboardCoordinator(
        IDayScheduleDashboardService dashboardService,
        IEmailInboxService emailInboxService,
        IUiDispatcherTimerFactory timerFactory)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);
        ArgumentNullException.ThrowIfNull(emailInboxService);
        ArgumentNullException.ThrowIfNull(timerFactory);

        _dashboardService = dashboardService;
        _emailInboxService = emailInboxService;
        _clockTimer = timerFactory.Create(TimeSpan.FromMinutes(1));
        _calendarTimer = timerFactory.Create(_dashboardService.CalendarRefreshInterval);

        _clockTimer.Tick += OnClockTimerTickAsync;
        _calendarTimer.Tick += OnCalendarTimerTickAsync;
    }

    /// <summary>
    /// Raised when the schedule display state changes.
    /// </summary>
    public event EventHandler<DayScheduleDisplayStateChangedEventArgs>? DisplayStateChanged;

    /// <summary>
    /// Raised when the inbox snapshot changes.
    /// </summary>
    public event EventHandler<EmailInboxSnapshotChangedEventArgs>? InboxSnapshotChanged;

    /// <summary>
    /// Loads the initial dashboard state and starts background timers.
    /// </summary>
    /// <returns>A task that completes when initialization has finished.</returns>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _lastObservedCurrentDate = _dashboardService.CurrentLocalDate;
        await RefreshDashboardAsync(CalendarInteractionMode.Interactive);

        _clockTimer.StartTimer();
        if (ShouldRunBackgroundRefresh)
        {
            _calendarTimer.StartTimer();
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Triggers an interactive dashboard refresh.
    /// </summary>
    /// <returns>A task that completes when the refresh has finished.</returns>
    public Task RefreshNowAsync() => RefreshDashboardAsync(CalendarInteractionMode.Interactive);

    /// <summary>
    /// Moves the selected schedule date by the provided number of days.
    /// </summary>
    /// <param name="dayOffset">The number of days to move backward or forward.</param>
    /// <returns>A task that completes when navigation and refresh are finished.</returns>
    public async Task NavigateDaysAsync(int dayOffset)
    {
        if (dayOffset == 0)
        {
            return;
        }

        _dashboardService.ShiftSelectedDate(dayOffset);
        PublishDisplayState(_dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
        await RefreshDashboardAsync(CalendarInteractionMode.Interactive);
    }

    /// <summary>
    /// Updates the available schedule width used when building the timeline layout.
    /// </summary>
    /// <param name="availableScheduleWidth">The width available to the schedule canvas.</param>
    public void UpdateAvailableScheduleWidth(double availableScheduleWidth)
    {
        var normalizedWidth = Math.Max(420, Math.Floor(availableScheduleWidth));
        if (Math.Abs(_availableScheduleWidth - normalizedWidth) < 1)
        {
            return;
        }

        _availableScheduleWidth = normalizedWidth;
        PublishDisplayState(_dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
    }

    /// <summary>
    /// Stops the background timers owned by the coordinator.
    /// </summary>
    public void Dispose()
    {
        _clockTimer.Tick -= OnClockTimerTickAsync;
        _calendarTimer.Tick -= OnCalendarTimerTickAsync;
        _clockTimer.StopTimer();
        _calendarTimer.StopTimer();
    }

    private bool ShouldRunBackgroundRefresh =>
        _dashboardService.IsCalendarEnabled || _emailInboxService.IsEnabled;

    private async void OnClockTimerTickAsync(object? sender, EventArgs e)
    {
        var currentLocalDate = _dashboardService.CurrentLocalDate;
        if (_lastObservedCurrentDate.HasValue && currentLocalDate != _lastObservedCurrentDate.Value)
        {
            _lastObservedCurrentDate = currentLocalDate;
            _dashboardService.TrySelectCurrentDate();

            if (_isRefreshing)
            {
                _pendingCurrentDateRefresh = true;
                PublishDisplayState(_dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
                return;
            }

            await RefreshDashboardAsync(CalendarInteractionMode.Background);
            return;
        }

        PublishDisplayState(_dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
    }

    private async void OnCalendarTimerTickAsync(object? sender, EventArgs e)
    {
        await RefreshDashboardAsync(CalendarInteractionMode.Background);
    }

    private async Task RefreshDashboardAsync(CalendarInteractionMode interactionMode)
    {
        if (_isRefreshing)
        {
            PublishDisplayState(_dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
            return;
        }

        _isRefreshing = true;
        _calendarTimer.StopTimer();

        try
        {
            while (true)
            {
                _pendingCurrentDateRefresh = false;

                PublishDisplayState(await _dashboardService.RefreshCalendarAsync(
                    interactionMode,
                    _availableScheduleWidth,
                    CancellationToken.None));

                PublishInboxSnapshot(await _emailInboxService.GetInboxSnapshotAsync(
                    interactionMode == CalendarInteractionMode.Interactive,
                    CancellationToken.None));

                if (!_pendingCurrentDateRefresh)
                {
                    break;
                }

                interactionMode = CalendarInteractionMode.Background;
            }
        }
        finally
        {
            _isRefreshing = false;
            if (ShouldRunBackgroundRefresh && _isInitialized)
            {
                _calendarTimer.StartTimer();
            }
        }
    }

    private void PublishDisplayState(DayScheduleDisplayState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        DisplayStateChanged?.Invoke(this, new DayScheduleDisplayStateChangedEventArgs(state));
    }

    private void PublishInboxSnapshot(EmailInboxSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        InboxSnapshotChanged?.Invoke(this, new EmailInboxSnapshotChangedEventArgs(snapshot));
    }

    private readonly IDayScheduleDashboardService _dashboardService;
    private readonly IEmailInboxService _emailInboxService;
    private readonly IUiDispatcherTimer _clockTimer;
    private readonly IUiDispatcherTimer _calendarTimer;
    private double _availableScheduleWidth = 860;
    private bool _isInitialized;
    private bool _isRefreshing;
    private bool _pendingCurrentDateRefresh;
    private DateOnly? _lastObservedCurrentDate;
}

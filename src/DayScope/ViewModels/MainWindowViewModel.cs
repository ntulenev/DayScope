using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

using DayScope.Application.Calendar;
using DayScope.Application.Dashboard;
using DayScope.Application.DaySchedule;

namespace DayScope.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    public MainWindowViewModel(DayScheduleDashboardService dashboardService)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);

        _dashboardService = dashboardService;
        PrimaryTimelineHours = new ReadOnlyObservableCollection<TimelineHourDisplayState>(_primaryTimelineHoursSource);
        SecondaryTimelineHours = new ReadOnlyObservableCollection<TimelineHourDisplayState>(_secondaryTimelineHoursSource);
        AllDayEvents = new ReadOnlyObservableCollection<AllDayEventDisplayState>(_allDayEventsSource);
        TimedEvents = new ReadOnlyObservableCollection<TimedEventDisplayState>(_timedEventsSource);

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _calendarTimer = new DispatcherTimer
        {
            Interval = _dashboardService.CalendarRefreshInterval
        };

        _clockTimer.Tick += (_, _) => ApplyDisplayState(
            _dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
        _calendarTimer.Tick += async (_, _) => await RefreshCalendarAsync(CalendarInteractionMode.Background);
    }

    public ReadOnlyObservableCollection<TimelineHourDisplayState> PrimaryTimelineHours { get; }

    public ReadOnlyObservableCollection<TimelineHourDisplayState> SecondaryTimelineHours { get; }

    public ReadOnlyObservableCollection<AllDayEventDisplayState> AllDayEvents { get; }

    public ReadOnlyObservableCollection<TimedEventDisplayState> TimedEvents { get; }

    public string MonthTitle { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string DayTitle { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string DayNumberText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string DateText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string PrimaryTimeZoneLabel { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string? SecondaryTimeZoneLabel { get; private set => SetProperty(ref field, value); }

    public bool HasSecondaryTimeZone { get; private set => SetProperty(ref field, value); }

    public GridLength SecondaryTimeColumnWidth
    {
        get; private set => SetProperty(ref field, value);
    } = new(0);

    public double ScheduleCanvasWidth { get; private set => SetProperty(ref field, value); } = 860;

    public double TimelineHeight { get; private set => SetProperty(ref field, value); } = 1000;

    public string StatusText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public bool ShowStatus { get; private set => SetProperty(ref field, value); }

    public double NowLineTop { get; private set => SetProperty(ref field, value); } = -1;

    public string NowLineText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public bool ShowNowLine { get; private set => SetProperty(ref field, value); }

    public async Task InitializeAsync()
    {
        ApplyDisplayState(await _dashboardService.RefreshCalendarAsync(
            CalendarInteractionMode.Interactive,
            _availableScheduleWidth,
            CancellationToken.None));

        _clockTimer.Start();
        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    public void Dispose()
    {
        _clockTimer.Stop();
        _calendarTimer.Stop();
    }

    private async Task RefreshCalendarAsync(CalendarInteractionMode interactionMode)
    {
        _calendarTimer.Stop();
        ApplyDisplayState(await _dashboardService.RefreshCalendarAsync(
            interactionMode,
            _availableScheduleWidth,
            CancellationToken.None));

        if (_dashboardService.IsCalendarEnabled)
        {
            _calendarTimer.Start();
        }
    }

    private void ApplyDisplayState(DayScheduleDisplayState state)
    {
        MonthTitle = state.MonthTitle;
        DayTitle = state.DayTitle;
        DayNumberText = state.DayNumberText;
        DateText = state.DateText;
        PrimaryTimeZoneLabel = state.PrimaryTimeZoneLabel;
        SecondaryTimeZoneLabel = state.SecondaryTimeZoneLabel;
        HasSecondaryTimeZone = !string.IsNullOrWhiteSpace(state.SecondaryTimeZoneLabel);
        SecondaryTimeColumnWidth = HasSecondaryTimeZone ? new GridLength(52) : new GridLength(0);
        ScheduleCanvasWidth = state.ScheduleCanvasWidth;
        TimelineHeight = state.TimelineHeight;
        StatusText = state.StatusText;
        ShowStatus = state.ShowStatus;
        NowLineTop = state.NowLineTop ?? -1;
        NowLineText = state.NowLineText;
        ShowNowLine = state.NowLineTop.HasValue;

        ReplaceCollection(_primaryTimelineHoursSource, state.PrimaryTimelineHours);
        ReplaceCollection(_secondaryTimelineHoursSource, state.SecondaryTimelineHours);
        ReplaceCollection(_allDayEventsSource, state.AllDayEvents);
        ReplaceCollection(_timedEventsSource, state.TimedEvents);
    }

    public void UpdateAvailableScheduleWidth(double availableScheduleWidth)
    {
        var normalizedWidth = Math.Max(420, Math.Floor(availableScheduleWidth));
        if (Math.Abs(_availableScheduleWidth - normalizedWidth) < 1)
        {
            return;
        }

        _availableScheduleWidth = normalizedWidth;
        ApplyDisplayState(_dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
    }

    private static void ReplaceCollection<T>(
        ObservableCollection<T> target,
        IReadOnlyList<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private readonly DayScheduleDashboardService _dashboardService;
    private readonly ObservableCollection<TimelineHourDisplayState> _primaryTimelineHoursSource = [];
    private readonly ObservableCollection<TimelineHourDisplayState> _secondaryTimelineHoursSource = [];
    private readonly ObservableCollection<AllDayEventDisplayState> _allDayEventsSource = [];
    private readonly ObservableCollection<TimedEventDisplayState> _timedEventsSource = [];
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _calendarTimer;
    private double _availableScheduleWidth = 860;
}

using System.Collections.ObjectModel;
using System.Windows;

using DayScope.Application.DaySchedule;

namespace DayScope.ViewModels;

/// <summary>
/// Stores the schedule-specific state rendered by the main window.
/// </summary>
public sealed class MainWindowScheduleState : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowScheduleState"/> class.
    /// </summary>
    public MainWindowScheduleState()
    {
        PrimaryTimelineHours = new ReadOnlyObservableCollection<TimelineHourDisplayState>(_primaryTimelineHoursSource);
        SecondaryTimelineHours = new ReadOnlyObservableCollection<TimelineHourDisplayState>(_secondaryTimelineHoursSource);
        AllDayEvents = new ReadOnlyObservableCollection<AllDayEventDisplayState>(_allDayEventsSource);
        TimedEvents = new ReadOnlyObservableCollection<TimedEventDisplayState>(_timedEventsSource);
    }

    public ReadOnlyObservableCollection<TimelineHourDisplayState> PrimaryTimelineHours { get; }

    public ReadOnlyObservableCollection<TimelineHourDisplayState> SecondaryTimelineHours { get; }

    public ReadOnlyObservableCollection<AllDayEventDisplayState> AllDayEvents { get; }

    public ReadOnlyObservableCollection<TimedEventDisplayState> TimedEvents { get; }

    public DateOnly DisplayDate { get; private set => SetProperty(ref field, value); } =
        DateOnly.FromDateTime(DateTime.Today);

    public string MonthTitle { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string DayTitle { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string DayNumberText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string DateText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string PrimaryTimeZoneLabel { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string? SecondaryTimeZoneLabel { get; private set => SetProperty(ref field, value); }

    public bool HasConfiguredSecondaryTimeZone { get; private set => SetProperty(ref field, value); }

    public bool ShowSecondaryTimeZone => _showSecondaryTimeZone;

    public bool HasSecondaryTimeZone { get; private set => SetProperty(ref field, value); }

    public GridLength PrimaryTimeColumnWidth { get; private set => SetProperty(ref field, value); } = new(72);

    public GridLength SecondaryTimeZoneLeadingGapWidth { get; private set => SetProperty(ref field, value); } = new(0);

    public GridLength SecondaryTimeColumnWidth { get; private set => SetProperty(ref field, value); } = new(0);

    public GridLength SecondaryTimeZoneTrailingGapWidth { get; private set => SetProperty(ref field, value); } = new(4);

    public double ScheduleCanvasWidth { get; private set => SetProperty(ref field, value); } = 860;

    public double TimelineHeight { get; private set => SetProperty(ref field, value); } = 1000;

    public string StatusText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public bool ShowStatus { get; private set => SetProperty(ref field, value); }

    public double NowLineTop { get; private set => SetProperty(ref field, value); } = -1;

    public string NowLineText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public bool ShowNowLine { get; private set => SetProperty(ref field, value); }

    /// <summary>
    /// Applies the latest dashboard display state.
    /// </summary>
    /// <param name="state">The display state to apply.</param>
    public void Apply(DayScheduleDisplayState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        DisplayDate = state.DisplayDate;
        MonthTitle = state.MonthTitle;
        DayTitle = state.DayTitle;
        DayNumberText = state.DayNumberText;
        DateText = state.DateText;
        PrimaryTimeZoneLabel = state.PrimaryTimeZoneLabel;
        SecondaryTimeZoneLabel = state.SecondaryTimeZoneLabel;
        HasConfiguredSecondaryTimeZone = !string.IsNullOrWhiteSpace(state.SecondaryTimeZoneLabel);
        PrimaryTimeColumnWidth = ResolveTimeColumnWidth(state.PrimaryTimeZoneLabel);
        ApplySecondaryTimeZoneVisibility();
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

    /// <summary>
    /// Updates whether the configured secondary time zone should be shown in the UI.
    /// </summary>
    /// <param name="showSecondaryTimeZone">Whether the secondary time zone should be visible.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool SetShowSecondaryTimeZone(bool showSecondaryTimeZone)
    {
        if (!SetProperty(ref _showSecondaryTimeZone, showSecondaryTimeZone, nameof(ShowSecondaryTimeZone)))
        {
            return false;
        }

        ApplySecondaryTimeZoneVisibility();
        return true;
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

    private static GridLength ResolveTimeColumnWidth(string? label)
    {
        const double minimumWidth = 72;
        const double perCharacterWidth = 7.2;
        const double horizontalPadding = 8;

        var normalizedLabel = string.IsNullOrWhiteSpace(label)
            ? string.Empty
            : label.Trim();
        var width = Math.Max(
            minimumWidth,
            Math.Ceiling((normalizedLabel.Length * perCharacterWidth) + horizontalPadding));

        return new GridLength(width);
    }

    private void ApplySecondaryTimeZoneVisibility()
    {
        HasSecondaryTimeZone = HasConfiguredSecondaryTimeZone && ShowSecondaryTimeZone;
        SecondaryTimeZoneLeadingGapWidth = HasSecondaryTimeZone
            ? new GridLength(8)
            : new GridLength(0);
        SecondaryTimeColumnWidth = HasSecondaryTimeZone
            ? ResolveTimeColumnWidth(SecondaryTimeZoneLabel)
            : new GridLength(0);
        SecondaryTimeZoneTrailingGapWidth = HasSecondaryTimeZone
            ? new GridLength(8)
            : new GridLength(4);
    }

    private readonly ObservableCollection<TimelineHourDisplayState> _primaryTimelineHoursSource = [];
    private readonly ObservableCollection<TimelineHourDisplayState> _secondaryTimelineHoursSource = [];
    private readonly ObservableCollection<AllDayEventDisplayState> _allDayEventsSource = [];
    private readonly ObservableCollection<TimedEventDisplayState> _timedEventsSource = [];
    private bool _showSecondaryTimeZone = true;
}

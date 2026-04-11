using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.Dashboard;
using DayScope.Application.DaySchedule;
using DayScope.Threading;

namespace DayScope.ViewModels;

/// <summary>
/// Coordinates the state and actions rendered by the main dashboard window.
/// </summary>
public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dashboardService">The service that builds schedule display state.</param>
    /// <param name="emailInboxService">The service that loads unread email data.</param>
    /// <param name="workspaceUriBuilder">The builder used to create Google Calendar and Gmail links.</param>
    /// <param name="timerFactory">The factory used to create UI timers.</param>
    public MainWindowViewModel(
        IDayScheduleDashboardService dashboardService,
        IEmailInboxService emailInboxService,
        IGoogleWorkspaceUriBuilder workspaceUriBuilder,
        IUiDispatcherTimerFactory timerFactory)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);
        ArgumentNullException.ThrowIfNull(emailInboxService);
        ArgumentNullException.ThrowIfNull(workspaceUriBuilder);
        ArgumentNullException.ThrowIfNull(timerFactory);

        _dashboardService = dashboardService;
        _emailInboxService = emailInboxService;
        _workspaceUriBuilder = workspaceUriBuilder;
        PrimaryTimelineHours = new ReadOnlyObservableCollection<TimelineHourDisplayState>(_primaryTimelineHoursSource);
        SecondaryTimelineHours = new ReadOnlyObservableCollection<TimelineHourDisplayState>(_secondaryTimelineHoursSource);
        AllDayEvents = new ReadOnlyObservableCollection<AllDayEventDisplayState>(_allDayEventsSource);
        TimedEvents = new ReadOnlyObservableCollection<TimedEventDisplayState>(_timedEventsSource);

        _clockTimer = timerFactory.Create(TimeSpan.FromMinutes(1));
        _calendarTimer = timerFactory.Create(_dashboardService.CalendarRefreshInterval);

        _clockTimer.Tick += (_, _) => ApplyDisplayState(
            _dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
        _calendarTimer.Tick += async (_, _) => await RefreshDashboardAsync(CalendarInteractionMode.Background);
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

    public GridLength PrimaryTimeColumnWidth { get; private set => SetProperty(ref field, value); } = new(72);

    public GridLength SecondaryTimeColumnWidth { get; private set => SetProperty(ref field, value); } = new(0);

    public double ScheduleCanvasWidth { get; private set => SetProperty(ref field, value); } = 860;

    public double TimelineHeight { get; private set => SetProperty(ref field, value); } = 1000;

    public string StatusText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public bool ShowStatus { get; private set => SetProperty(ref field, value); }

    public double NowLineTop { get; private set => SetProperty(ref field, value); } = -1;

    public string NowLineText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public bool ShowNowLine { get; private set => SetProperty(ref field, value); }

    public int? UnreadEmailCount => _unreadEmailCount;

    public Uri UnreadEmailInboxUri => _unreadEmailInboxUri;

    public Uri GoogleCalendarUri => _googleCalendarUri;

    public string UnreadEmailCountText => _unreadEmailCount switch
    {
        null => "--",
        > 99 => "99+",
        _ => _unreadEmailCount.Value.ToString(CultureInfo.InvariantCulture)
    };

    public bool HasUnreadEmails => _unreadEmailCount is > 0;

    public string UnreadEmailSummaryText => _unreadEmailCount switch
    {
        null => "Open Gmail inbox",
        0 => "Inbox is clear",
        1 => "1 unread email",
        _ => string.Format(
            CultureInfo.InvariantCulture,
            "{0} unread emails",
            _unreadEmailCount.Value)
    };

    public EventDetailsDisplayState? SelectedEventDetails => _selectedEventDetails;

    public bool IsEventDetailsOpen => SelectedEventDetails is not null;

    public bool HasSelectedEventOrganizer =>
        !string.IsNullOrWhiteSpace(SelectedEventDetails?.Organizer);

    public bool HasSelectedEventDescription =>
        !string.IsNullOrWhiteSpace(SelectedEventDetails?.Description);

    public bool HasSelectedEventParticipants =>
        SelectedEventDetails?.Participants.Count > 0;

    public bool HasSelectedEventJoinUrl =>
        SelectedEventDetails?.JoinUrl is not null;

    public string SelectedEventJoinLabel =>
        SelectedEventDetails?.JoinUrl?.Host.Contains("meet.google.com", StringComparison.OrdinalIgnoreCase) is true
            ? "Join Google Meet"
            : "Open meeting link";

    /// <summary>
    /// Loads the initial dashboard state and starts background timers.
    /// </summary>
    /// <returns>A task that completes when initialization has finished.</returns>
    public async Task InitializeAsync()
    {
        await RefreshDashboardAsync(CalendarInteractionMode.Interactive);

        _clockTimer.StartTimer();
        if (_dashboardService.IsCalendarEnabled || _emailInboxService.IsEnabled)
        {
            _calendarTimer.StartTimer();
        }
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

        CloseEventDetails();
        _dashboardService.ShiftSelectedDate(dayOffset);
        ApplyDisplayState(_dashboardService.GetCurrentDisplayState(_availableScheduleWidth));
        await RefreshDashboardAsync(CalendarInteractionMode.Interactive);
    }

    /// <summary>
    /// Opens the details overlay for the provided event view model.
    /// </summary>
    /// <param name="eventState">The selected timed or all-day event display state.</param>
    public void OpenEventDetails(object? eventState)
    {
        var details = eventState switch
        {
            TimedEventDisplayState timedEvent => timedEvent.Details,
            AllDayEventDisplayState allDayEvent => allDayEvent.Details,
            _ => null
        };

        SetSelectedEventDetails(details);
    }

    /// <summary>
    /// Closes the event details overlay.
    /// </summary>
    public void CloseEventDetails() => SetSelectedEventDetails(null);

    /// <summary>
    /// Stops the background timers owned by the view model.
    /// </summary>
    public void Dispose()
    {
        _clockTimer.StopTimer();
        _calendarTimer.StopTimer();
    }

    /// <summary>
    /// Refreshes the dashboard state and unread email count.
    /// </summary>
    /// <param name="interactionMode">Whether the refresh is interactive or background.</param>
    /// <returns>A task that completes when the refresh finishes.</returns>
    private async Task RefreshDashboardAsync(CalendarInteractionMode interactionMode)
    {
        _calendarTimer.StopTimer();

        try
        {
            ApplyDisplayState(await _dashboardService.RefreshCalendarAsync(
                interactionMode,
                _availableScheduleWidth,
                CancellationToken.None));

            await RefreshUnreadEmailCountAsync(
                interactionMode == CalendarInteractionMode.Interactive,
                CancellationToken.None);
        }
        finally
        {
            if (_dashboardService.IsCalendarEnabled || _emailInboxService.IsEnabled)
            {
                _calendarTimer.StartTimer();
            }
        }
    }

    /// <summary>
    /// Applies a new dashboard display state to the view model.
    /// </summary>
    /// <param name="state">The display state to apply.</param>
    private void ApplyDisplayState(DayScheduleDisplayState state)
    {
        _displayDate = state.DisplayDate;
        MonthTitle = state.MonthTitle;
        DayTitle = state.DayTitle;
        DayNumberText = state.DayNumberText;
        DateText = state.DateText;
        PrimaryTimeZoneLabel = state.PrimaryTimeZoneLabel;
        SecondaryTimeZoneLabel = state.SecondaryTimeZoneLabel;
        HasSecondaryTimeZone = !string.IsNullOrWhiteSpace(state.SecondaryTimeZoneLabel);
        PrimaryTimeColumnWidth = ResolveTimeColumnWidth(state.PrimaryTimeZoneLabel);
        SecondaryTimeColumnWidth = HasSecondaryTimeZone
            ? ResolveTimeColumnWidth(state.SecondaryTimeZoneLabel)
            : new GridLength(0);
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
        UpdateGoogleCalendarUri();
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

    /// <summary>
    /// Calculates the width required to render a time column label.
    /// </summary>
    /// <param name="label">The time zone label to measure.</param>
    /// <returns>The width used for the time column.</returns>
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

    /// <summary>
    /// Refreshes the unread email count and related Google links.
    /// </summary>
    /// <param name="allowInteractiveAuthentication">Whether sign-in prompts are allowed.</param>
    /// <param name="cancellationToken">The cancellation token for the refresh.</param>
    /// <returns>A task that completes when the inbox snapshot has been applied.</returns>
    private async Task RefreshUnreadEmailCountAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken)
    {
        var inboxSnapshot = await _emailInboxService.GetInboxSnapshotAsync(
            allowInteractiveAuthentication,
            cancellationToken);

        SetUnreadEmailCount(inboxSnapshot.UnreadCount);
        SetUnreadEmailInboxUri(inboxSnapshot.InboxUri);
        SetGoogleAccountEmail(inboxSnapshot.EmailAddress);
    }

    /// <summary>
    /// Updates the stored unread email count and dependent derived properties.
    /// </summary>
    /// <param name="unreadEmailCount">The unread email count to apply.</param>
    private void SetUnreadEmailCount(int? unreadEmailCount)
    {
        if (!SetProperty(ref _unreadEmailCount, unreadEmailCount, nameof(UnreadEmailCount)))
        {
            return;
        }

        OnPropertyChanged(nameof(UnreadEmailCountText));
        OnPropertyChanged(nameof(HasUnreadEmails));
        OnPropertyChanged(nameof(UnreadEmailSummaryText));
    }

    /// <summary>
    /// Updates the Gmail inbox URI.
    /// </summary>
    /// <param name="inboxUri">The inbox URI to apply.</param>
    private void SetUnreadEmailInboxUri(Uri inboxUri)
    {
        ArgumentNullException.ThrowIfNull(inboxUri);

        if (!SetProperty(ref _unreadEmailInboxUri, inboxUri, nameof(UnreadEmailInboxUri)))
        {
            return;
        }
    }

    /// <summary>
    /// Updates the signed-in Google account email and dependent links.
    /// </summary>
    /// <param name="emailAddress">The Google account email address, if known.</param>
    private void SetGoogleAccountEmail(string? emailAddress)
    {
        var normalizedEmailAddress = string.IsNullOrWhiteSpace(emailAddress)
            ? null
            : emailAddress.Trim();

        if (string.Equals(
            _googleAccountEmail,
            normalizedEmailAddress,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _googleAccountEmail = normalizedEmailAddress;
        UpdateGoogleCalendarUri();
    }

    /// <summary>
    /// Rebuilds the Google Calendar URI for the currently selected date.
    /// </summary>
    private void UpdateGoogleCalendarUri()
    {
        SetGoogleCalendarUri(_workspaceUriBuilder.BuildCalendarDayUri(_displayDate, _googleAccountEmail));
    }

    /// <summary>
    /// Updates the Google Calendar URI.
    /// </summary>
    /// <param name="calendarUri">The calendar URI to apply.</param>
    private void SetGoogleCalendarUri(Uri calendarUri)
    {
        ArgumentNullException.ThrowIfNull(calendarUri);

        if (!SetProperty(ref _googleCalendarUri, calendarUri, nameof(GoogleCalendarUri)))
        {
            return;
        }
    }

    /// <summary>
    /// Updates the selected event details and dependent overlay properties.
    /// </summary>
    /// <param name="details">The event details to apply, or <see langword="null"/> to close the overlay.</param>
    private void SetSelectedEventDetails(EventDetailsDisplayState? details)
    {
        if (!SetProperty(ref _selectedEventDetails, details, nameof(SelectedEventDetails)))
        {
            return;
        }

        OnPropertyChanged(nameof(IsEventDetailsOpen));
        OnPropertyChanged(nameof(HasSelectedEventOrganizer));
        OnPropertyChanged(nameof(HasSelectedEventDescription));
        OnPropertyChanged(nameof(HasSelectedEventParticipants));
        OnPropertyChanged(nameof(HasSelectedEventJoinUrl));
        OnPropertyChanged(nameof(SelectedEventJoinLabel));
    }

    private readonly IDayScheduleDashboardService _dashboardService;
    private readonly ObservableCollection<TimelineHourDisplayState> _primaryTimelineHoursSource = [];
    private readonly ObservableCollection<TimelineHourDisplayState> _secondaryTimelineHoursSource = [];
    private readonly ObservableCollection<AllDayEventDisplayState> _allDayEventsSource = [];
    private readonly ObservableCollection<TimedEventDisplayState> _timedEventsSource = [];
    private readonly IEmailInboxService _emailInboxService;
    private readonly IGoogleWorkspaceUriBuilder _workspaceUriBuilder;
    private readonly IUiDispatcherTimer _clockTimer;
    private readonly IUiDispatcherTimer _calendarTimer;
    private double _availableScheduleWidth = 860;
    private DateOnly _displayDate = DateOnly.FromDateTime(DateTime.Today);
    private string? _googleAccountEmail;
    private int? _unreadEmailCount;
    private Uri _googleCalendarUri = new("https://calendar.google.com/calendar/r/day", UriKind.Absolute);
    private Uri _unreadEmailInboxUri = new("https://mail.google.com/mail/", UriKind.Absolute);
    private EventDetailsDisplayState? _selectedEventDetails;
}

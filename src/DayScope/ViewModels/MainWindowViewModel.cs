using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.Dashboard;
using DayScope.Application.DaySchedule;

namespace DayScope.ViewModels;

public sealed class MainWindowViewModel : ObservableObject, IDisposable
{
    public MainWindowViewModel(
        DayScheduleDashboardService dashboardService,
        IEmailInboxService emailInboxService)
    {
        ArgumentNullException.ThrowIfNull(dashboardService);
        ArgumentNullException.ThrowIfNull(emailInboxService);

        _dashboardService = dashboardService;
        _emailInboxService = emailInboxService;
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

    public async Task InitializeAsync()
    {
        await RefreshDashboardAsync(CalendarInteractionMode.Interactive);

        _clockTimer.Start();
        if (_dashboardService.IsCalendarEnabled || _emailInboxService.IsEnabled)
        {
            _calendarTimer.Start();
        }
    }

    public Task RefreshNowAsync() => RefreshDashboardAsync(CalendarInteractionMode.Interactive);

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

    public void CloseEventDetails() => SetSelectedEventDetails(null);

    public void Dispose()
    {
        _clockTimer.Stop();
        _calendarTimer.Stop();
    }

    private async Task RefreshDashboardAsync(CalendarInteractionMode interactionMode)
    {
        _calendarTimer.Stop();

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
                _calendarTimer.Start();
            }
        }
    }

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

    private void SetUnreadEmailInboxUri(Uri inboxUri)
    {
        ArgumentNullException.ThrowIfNull(inboxUri);

        if (!SetProperty(ref _unreadEmailInboxUri, inboxUri, nameof(UnreadEmailInboxUri)))
        {
            return;
        }
    }

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

    private void UpdateGoogleCalendarUri()
    {
        SetGoogleCalendarUri(BuildGoogleCalendarUri(_displayDate, _googleAccountEmail));
    }

    private void SetGoogleCalendarUri(Uri calendarUri)
    {
        ArgumentNullException.ThrowIfNull(calendarUri);

        if (!SetProperty(ref _googleCalendarUri, calendarUri, nameof(GoogleCalendarUri)))
        {
            return;
        }
    }

    private static Uri BuildGoogleCalendarUri(
        DateOnly displayDate,
        string? emailAddress)
    {
        var dayPath = string.Format(
            CultureInfo.InvariantCulture,
            "https://calendar.google.com/calendar/r/day/{0}/{1}/{2}",
            displayDate.Year,
            displayDate.Month,
            displayDate.Day);

        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return new Uri(dayPath, UriKind.Absolute);
        }

        // Inference from current Google web behavior: Calendar accepts authuser with the signed-in account email.
        return new Uri(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}?authuser={1}",
                dayPath,
                Uri.EscapeDataString(emailAddress)),
            UriKind.Absolute);
    }

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

    private readonly DayScheduleDashboardService _dashboardService;
    private readonly ObservableCollection<TimelineHourDisplayState> _primaryTimelineHoursSource = [];
    private readonly ObservableCollection<TimelineHourDisplayState> _secondaryTimelineHoursSource = [];
    private readonly ObservableCollection<AllDayEventDisplayState> _allDayEventsSource = [];
    private readonly ObservableCollection<TimedEventDisplayState> _timedEventsSource = [];
    private readonly IEmailInboxService _emailInboxService;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _calendarTimer;
    private double _availableScheduleWidth = 860;
    private DateOnly _displayDate = DateOnly.FromDateTime(DateTime.Today);
    private string? _googleAccountEmail;
    private int? _unreadEmailCount;
    private Uri _googleCalendarUri = new("https://calendar.google.com/calendar/r/day", UriKind.Absolute);
    private Uri _unreadEmailInboxUri = new("https://mail.google.com/mail/", UriKind.Absolute);
    private EventDetailsDisplayState? _selectedEventDetails;
}

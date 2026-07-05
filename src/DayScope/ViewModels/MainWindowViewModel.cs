using DayScope.Themes;

namespace DayScope.ViewModels;

/// <summary>
/// Coordinates the state and actions rendered by the main dashboard window.
/// </summary>
public sealed class MainWindowViewModel : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dashboardCoordinator">The coordinator used to refresh and navigate the dashboard.</param>
    /// <param name="inbox">The inbox state shown by the main window.</param>
    public MainWindowViewModel(
        MainWindowDashboardCoordinator dashboardCoordinator,
        MainWindowInboxState inbox)
        : this(
            dashboardCoordinator,
            inbox,
            secondaryTimeZonePreferenceStore: null,
            calendarZoomPreferenceStore: null,
            privacyModePreferenceStore: null,
            throwOnNullPreferenceStore: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dashboardCoordinator">The coordinator used to refresh and navigate the dashboard.</param>
    /// <param name="inbox">The inbox state shown by the main window.</param>
    /// <param name="secondaryTimeZonePreferenceStore">The store used to persist secondary-time-zone visibility.</param>
    public MainWindowViewModel(
        MainWindowDashboardCoordinator dashboardCoordinator,
        MainWindowInboxState inbox,
        ISecondaryTimeZonePreferenceStore secondaryTimeZonePreferenceStore)
        : this(
            dashboardCoordinator,
            inbox,
            secondaryTimeZonePreferenceStore,
            calendarZoomPreferenceStore: null,
            privacyModePreferenceStore: null,
            throwOnNullPreferenceStore: true,
            throwOnNullCalendarZoomPreferenceStore: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dashboardCoordinator">The coordinator used to refresh and navigate the dashboard.</param>
    /// <param name="inbox">The inbox state shown by the main window.</param>
    /// <param name="secondaryTimeZonePreferenceStore">The store used to persist secondary-time-zone visibility.</param>
    /// <param name="calendarZoomPreferenceStore">The store used to persist calendar body zoom.</param>
    public MainWindowViewModel(
        MainWindowDashboardCoordinator dashboardCoordinator,
        MainWindowInboxState inbox,
        ISecondaryTimeZonePreferenceStore secondaryTimeZonePreferenceStore,
        ICalendarZoomPreferenceStore calendarZoomPreferenceStore)
        : this(
            dashboardCoordinator,
            inbox,
            secondaryTimeZonePreferenceStore,
            calendarZoomPreferenceStore,
            privacyModePreferenceStore: null,
            throwOnNullPreferenceStore: true,
            throwOnNullCalendarZoomPreferenceStore: true,
            throwOnNullPrivacyModePreferenceStore: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="dashboardCoordinator">The coordinator used to refresh and navigate the dashboard.</param>
    /// <param name="inbox">The inbox state shown by the main window.</param>
    /// <param name="secondaryTimeZonePreferenceStore">The store used to persist secondary-time-zone visibility.</param>
    /// <param name="calendarZoomPreferenceStore">The store used to persist calendar body zoom.</param>
    /// <param name="privacyModePreferenceStore">The store used to persist privacy-mode visibility.</param>
    public MainWindowViewModel(
        MainWindowDashboardCoordinator dashboardCoordinator,
        MainWindowInboxState inbox,
        ISecondaryTimeZonePreferenceStore secondaryTimeZonePreferenceStore,
        ICalendarZoomPreferenceStore calendarZoomPreferenceStore,
        IPrivacyModePreferenceStore privacyModePreferenceStore)
        : this(
            dashboardCoordinator,
            inbox,
            secondaryTimeZonePreferenceStore,
            calendarZoomPreferenceStore,
            privacyModePreferenceStore,
            throwOnNullPreferenceStore: true,
            throwOnNullCalendarZoomPreferenceStore: true,
            throwOnNullPrivacyModePreferenceStore: true)
    {
    }

    private MainWindowViewModel(
        MainWindowDashboardCoordinator dashboardCoordinator,
        MainWindowInboxState inbox,
        ISecondaryTimeZonePreferenceStore? secondaryTimeZonePreferenceStore,
        ICalendarZoomPreferenceStore? calendarZoomPreferenceStore,
        IPrivacyModePreferenceStore? privacyModePreferenceStore,
        bool throwOnNullPreferenceStore = false,
        bool throwOnNullCalendarZoomPreferenceStore = false,
        bool throwOnNullPrivacyModePreferenceStore = false)
    {
        ArgumentNullException.ThrowIfNull(dashboardCoordinator);
        ArgumentNullException.ThrowIfNull(inbox);
        if (throwOnNullPreferenceStore)
        {
            ArgumentNullException.ThrowIfNull(secondaryTimeZonePreferenceStore);
        }

        if (throwOnNullCalendarZoomPreferenceStore)
        {
            ArgumentNullException.ThrowIfNull(calendarZoomPreferenceStore);
        }

        if (throwOnNullPrivacyModePreferenceStore)
        {
            ArgumentNullException.ThrowIfNull(privacyModePreferenceStore);
        }

        _dashboardCoordinator = dashboardCoordinator;
        Schedule = new MainWindowScheduleState();
        Inbox = inbox;
        EventDetails = new MainWindowEventDetailsState();
        _secondaryTimeZonePreferenceStore = secondaryTimeZonePreferenceStore;
        _calendarZoomPreferenceStore = calendarZoomPreferenceStore;
        _privacyModePreferenceStore = privacyModePreferenceStore;

        if (_secondaryTimeZonePreferenceStore is not null)
        {
            Schedule.SetShowSecondaryTimeZone(_secondaryTimeZonePreferenceStore.LoadShowSecondaryTimeZone());
        }

        if (_calendarZoomPreferenceStore is not null)
        {
            Schedule.SetCalendarZoomScale(_calendarZoomPreferenceStore.LoadCalendarZoomScale());
        }

        if (_privacyModePreferenceStore is not null)
        {
            ApplyPrivacyMode(_privacyModePreferenceStore.LoadPrivacyModeEnabled());
        }

        _dashboardCoordinator.DisplayStateChanged += OnDisplayStateChanged;
        _dashboardCoordinator.InboxSnapshotChanged += OnInboxSnapshotChanged;
    }

    public MainWindowScheduleState Schedule { get; }

    public MainWindowInboxState Inbox { get; }

    public MainWindowEventDetailsState EventDetails { get; }

    /// <summary>
    /// Loads the initial dashboard state and starts background timers.
    /// </summary>
    /// <returns>A task that completes when initialization has finished.</returns>
    public Task InitializeAsync() => _dashboardCoordinator.InitializeAsync();

    /// <summary>
    /// Triggers an interactive dashboard refresh.
    /// </summary>
    /// <returns>A task that completes when the refresh has finished.</returns>
    public Task RefreshNowAsync() => _dashboardCoordinator.RefreshNowAsync();

    /// <summary>
    /// Refreshes the dashboard when the local day changed while the app was idle.
    /// </summary>
    /// <returns>A task that returns <see langword="true"/> when the selected date changed.</returns>
    public Task<bool> RefreshCurrentDateIfChangedAsync() => _dashboardCoordinator.RefreshCurrentDateIfChangedAsync();

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

        EventDetails.Close();
        await _dashboardCoordinator.NavigateDaysAsync(dayOffset);
    }

    /// <summary>
    /// Opens the details overlay for the provided event view model.
    /// </summary>
    /// <param name="eventState">The selected timed or all-day event display state.</param>
    public void OpenEventDetails(object? eventState)
    {
        EventDetails.ApplyGoogleAccountEmail(Inbox.GoogleAccountEmail);
        EventDetails.Open(eventState);
    }

    /// <summary>
    /// Closes the event details overlay.
    /// </summary>
    public void CloseEventDetails() => EventDetails.Close();

    /// <summary>
    /// Updates the available schedule width used when building the timeline layout.
    /// </summary>
    /// <param name="availableScheduleWidth">The width available to the schedule canvas.</param>
    public void UpdateAvailableScheduleWidth(double availableScheduleWidth) =>
        _dashboardCoordinator.UpdateAvailableScheduleWidth(availableScheduleWidth);

    /// <summary>
    /// Updates whether the configured secondary time zone should be shown in the schedule UI.
    /// </summary>
    /// <param name="showSecondaryTimeZone">Whether the secondary time zone should be visible.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool SetShowSecondaryTimeZone(bool showSecondaryTimeZone)
    {
        if (!Schedule.SetShowSecondaryTimeZone(showSecondaryTimeZone))
        {
            return false;
        }

        _secondaryTimeZonePreferenceStore?.SaveShowSecondaryTimeZone(showSecondaryTimeZone);
        return true;
    }

    /// <summary>
    /// Toggles whether the configured secondary time zone should be shown in the schedule UI.
    /// </summary>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool ToggleShowSecondaryTimeZone() => SetShowSecondaryTimeZone(!Schedule.ShowSecondaryTimeZone);

    /// <summary>
    /// Updates and persists the calendar body zoom scale.
    /// </summary>
    /// <param name="calendarZoomScale">The requested calendar body zoom scale.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool SetCalendarZoomScale(double calendarZoomScale)
    {
        if (!Schedule.SetCalendarZoomScale(calendarZoomScale))
        {
            return false;
        }

        _calendarZoomPreferenceStore?.SaveCalendarZoomScale(Schedule.CalendarZoomScale);
        return true;
    }

    /// <summary>
    /// Increases and persists the calendar body zoom scale.
    /// </summary>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool IncreaseCalendarZoom()
    {
        if (!Schedule.IncreaseCalendarZoom())
        {
            return false;
        }

        _calendarZoomPreferenceStore?.SaveCalendarZoomScale(Schedule.CalendarZoomScale);
        return true;
    }

    /// <summary>
    /// Decreases and persists the calendar body zoom scale.
    /// </summary>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool DecreaseCalendarZoom()
    {
        if (!Schedule.DecreaseCalendarZoom())
        {
            return false;
        }

        _calendarZoomPreferenceStore?.SaveCalendarZoomScale(Schedule.CalendarZoomScale);
        return true;
    }

    /// <summary>
    /// Resets and persists the calendar body zoom scale.
    /// </summary>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool ResetCalendarZoom()
    {
        if (!Schedule.ResetCalendarZoom())
        {
            return false;
        }

        _calendarZoomPreferenceStore?.SaveCalendarZoomScale(Schedule.CalendarZoomScale);
        return true;
    }

    /// <summary>
    /// Updates and persists whether sensitive schedule and email details should be hidden.
    /// </summary>
    /// <param name="isPrivacyModeEnabled">Whether sensitive details should be hidden.</param>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool SetPrivacyModeEnabled(bool isPrivacyModeEnabled)
    {
        if (!ApplyPrivacyMode(isPrivacyModeEnabled))
        {
            return false;
        }

        _privacyModePreferenceStore?.SavePrivacyModeEnabled(isPrivacyModeEnabled);
        return true;
    }

    /// <summary>
    /// Toggles whether sensitive schedule and email details should be hidden.
    /// </summary>
    /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
    public bool TogglePrivacyMode() => SetPrivacyModeEnabled(!Schedule.IsPrivacyModeEnabled);

    /// <summary>
    /// Stops the background timers owned by the view model.
    /// </summary>
    public void Dispose()
    {
        _dashboardCoordinator.DisplayStateChanged -= OnDisplayStateChanged;
        _dashboardCoordinator.InboxSnapshotChanged -= OnInboxSnapshotChanged;
        _dashboardCoordinator.Dispose();
    }

    private void OnDisplayStateChanged(object? sender, DayScheduleDisplayStateChangedEventArgs e)
    {
        Schedule.Apply(e.DisplayState);
        Inbox.ApplyDisplayDate(Schedule.DisplayDate);
    }

    private void OnInboxSnapshotChanged(object? sender, EmailInboxSnapshotChangedEventArgs e)
    {
        Inbox.ApplySnapshot(e.Snapshot);
        EventDetails.ApplyGoogleAccountEmail(Inbox.GoogleAccountEmail);
    }

    private bool ApplyPrivacyMode(bool isPrivacyModeEnabled)
    {
        var changed = Schedule.SetPrivacyModeEnabled(isPrivacyModeEnabled);
        changed = Inbox.SetPrivacyModeEnabled(isPrivacyModeEnabled) || changed;
        changed = EventDetails.SetPrivacyModeEnabled(isPrivacyModeEnabled) || changed;
        return changed;
    }

    private readonly MainWindowDashboardCoordinator _dashboardCoordinator;
    private readonly ISecondaryTimeZonePreferenceStore? _secondaryTimeZonePreferenceStore;
    private readonly ICalendarZoomPreferenceStore? _calendarZoomPreferenceStore;
    private readonly IPrivacyModePreferenceStore? _privacyModePreferenceStore;
}

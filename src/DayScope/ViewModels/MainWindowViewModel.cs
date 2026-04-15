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
    {
        ArgumentNullException.ThrowIfNull(dashboardCoordinator);
        ArgumentNullException.ThrowIfNull(inbox);

        _dashboardCoordinator = dashboardCoordinator;
        Schedule = new MainWindowScheduleState();
        Inbox = inbox;
        EventDetails = new MainWindowEventDetailsState();

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

    private readonly MainWindowDashboardCoordinator _dashboardCoordinator;
}

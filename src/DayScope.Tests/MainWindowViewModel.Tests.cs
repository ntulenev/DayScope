using System.Globalization;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.Dashboard;
using DayScope.Application.DaySchedule;
using DayScope.Themes;
using DayScope.Threading;
using DayScope.ViewModels;

using FluentAssertions;

using Microsoft.Extensions.Hosting;

using Moq;

namespace DayScope.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact(DisplayName = "The constructor throws when the dashboard coordinator is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenDashboardCoordinatorIsNull()
    {
        // Arrange
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());

        // Act
        var action = () => new MainWindowViewModel(null!, inbox);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the inbox state is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenInboxIsNull()
    {
        // Arrange
        using var coordinator = CreateCoordinator(
            CreateDisplayState(),
            new EmailInboxSnapshot(1, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out _,
            out _,
            out _,
            out _);

        // Act
        var action = () => new MainWindowViewModel(coordinator, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "The constructor throws when the secondary-time-zone preference store is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldThrowWhenSecondaryTimeZonePreferenceStoreIsNull()
    {
        // Arrange
        using var coordinator = CreateCoordinator(
            CreateDisplayState(),
            new EmailInboxSnapshot(1, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out _,
            out _,
            out _,
            out _);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());

        // Act
        var action = () => new MainWindowViewModel(coordinator, inbox, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Initialization updates the schedule and inbox state from coordinator events.")]
    [Trait("Category", "Unit")]
    public async Task InitializeAsyncShouldUpdateScheduleAndInboxStateFromCoordinatorEvents()
    {
        // Arrange
        var displayState = CreateDisplayState(
            displayDate: new DateOnly(2026, 4, 16),
            width: 640,
            statusText: "Ready",
            showStatus: true,
            nowLineTop: 140);
        var inboxSnapshot = new EmailInboxSnapshot(
            7,
            " user@example.com ",
            new Uri("https://mail.google.com/mail/u/?authuser=user%40example.com#inbox"));
        using var coordinator = CreateCoordinator(
            displayState,
            inboxSnapshot,
            out var dashboardService,
            out _,
            out _,
            out _);
        var initialDisplayDate = DateOnly.FromDateTime(DateTime.Today);
        var workspaceUriBuilder = new RecordingGoogleWorkspaceUriBuilder
        {
            BuildCalendarDayUriHandler = (requestedDate, emailAddress) =>
            {
                if (requestedDate == initialDisplayDate)
                {
                    emailAddress.Should().BeNull();
                    return new Uri("https://calendar.google.com/calendar/r/day/2026/4/14");
                }

                requestedDate.Should().Be(displayState.DisplayDate);
                return emailAddress is null
                    ? new Uri("https://calendar.google.com/calendar/r/day/2026/4/16")
                    : emailAddress.Should().Be("user@example.com").And.Subject switch
                    {
                        _ => new Uri("https://calendar.google.com/calendar/r/day/2026/4/16?authuser=user%40example.com")
                    };
            }
        };
        var inbox = new MainWindowInboxState(workspaceUriBuilder);
        using var viewModel = new MainWindowViewModel(coordinator, inbox);

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.Schedule.DisplayDate.Should().Be(displayState.DisplayDate);
        viewModel.Schedule.ScheduleCanvasWidth.Should().Be(displayState.ScheduleCanvasWidth);
        viewModel.Schedule.StatusText.Should().Be("Ready");
        viewModel.Schedule.ShowStatus.Should().BeTrue();
        viewModel.Schedule.ShowNowLine.Should().BeTrue();
        viewModel.Inbox.UnreadEmailCount.Should().Be(7);
        viewModel.Inbox.UnreadEmailSummaryText.Should().Be("7 unread emails");
        viewModel.Inbox.GoogleCalendarUri.Should().Be(
            new Uri("https://calendar.google.com/calendar/r/day/2026/4/16?authuser=user%40example.com"));
        dashboardService.RefreshCalls.Should().Be(1);
        workspaceUriBuilder.BuildCalendarDayUriCalls.Should().Be(3);
    }

    [Fact(DisplayName = "Initialization respects the saved secondary-time-zone visibility preference.")]
    [Trait("Category", "Unit")]
    public async Task InitializeAsyncShouldRespectSavedSecondaryTimeZoneVisibilityPreference()
    {
        // Arrange
        using var coordinator = CreateCoordinator(
            CreateDisplayState(secondaryTimeZoneLabel: "GMT+3"),
            new EmailInboxSnapshot(0, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out _,
            out _,
            out _,
            out _);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        var preferenceStore = new RecordingSecondaryTimeZonePreferenceStore(showSecondaryTimeZone: false);
        using var viewModel = new MainWindowViewModel(coordinator, inbox, preferenceStore);

        // Act
        await viewModel.InitializeAsync();

        // Assert
        viewModel.Schedule.HasConfiguredSecondaryTimeZone.Should().BeTrue();
        viewModel.Schedule.ShowSecondaryTimeZone.Should().BeFalse();
        viewModel.Schedule.HasSecondaryTimeZone.Should().BeFalse();
        viewModel.Schedule.SecondaryTimeColumnWidth.Value.Should().Be(0);
        viewModel.Schedule.SecondaryTimeZoneLeadingGapWidth.Value.Should().Be(0);
        preferenceStore.LoadCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Refreshing now delegates to the coordinator and updates the displayed state.")]
    [Trait("Category", "Unit")]
    public async Task RefreshNowAsyncShouldDelegateToCoordinatorAndUpdateState()
    {
        // Arrange
        var initialState = CreateDisplayState(displayDate: new DateOnly(2026, 4, 14));
        var refreshedState = CreateDisplayState(displayDate: new DateOnly(2026, 4, 18), statusText: "Refreshed", showStatus: true);
        using var coordinator = CreateCoordinator(
            initialState,
            new EmailInboxSnapshot(1, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out var dashboardService,
            out _,
            out _,
            out _,
            refreshedDisplayState: refreshedState);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        using var viewModel = new MainWindowViewModel(coordinator, inbox);

        await viewModel.InitializeAsync();

        // Act
        await viewModel.RefreshNowAsync();

        // Assert
        viewModel.Schedule.DisplayDate.Should().Be(refreshedState.DisplayDate);
        viewModel.Schedule.StatusText.Should().Be("Refreshed");
        dashboardService.RefreshCalls.Should().Be(2);
    }

    [Fact(DisplayName = "Navigating by zero days does not close details or call the coordinator.")]
    [Trait("Category", "Unit")]
    public async Task NavigateDaysAsyncShouldDoNothingWhenOffsetIsZero()
    {
        // Arrange
        var detailState = CreateDetails();
        using var coordinator = CreateCoordinator(
            CreateDisplayState(),
            new EmailInboxSnapshot(1, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out var dashboardService,
            out _,
            out _,
            out _);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        using var viewModel = new MainWindowViewModel(coordinator, inbox);
        viewModel.OpenEventDetails(new AllDayEventDisplayState(
            "Offsite",
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            detailState));

        // Act
        await viewModel.NavigateDaysAsync(0);

        // Assert
        viewModel.EventDetails.IsOpen.Should().BeTrue();
        dashboardService.ShiftSelectedDateCalls.Should().Be(0);
        dashboardService.RefreshCalls.Should().Be(0);
    }

    [Fact(DisplayName = "Navigating days closes details and updates the selected schedule date.")]
    [Trait("Category", "Unit")]
    public async Task NavigateDaysAsyncShouldCloseDetailsAndUpdateSelectedScheduleDate()
    {
        // Arrange
        var loadingState = CreateDisplayState(displayDate: new DateOnly(2026, 4, 15), statusText: "Loading", showStatus: true);
        var refreshedState = CreateDisplayState(displayDate: new DateOnly(2026, 4, 15), statusText: "Done");
        using var coordinator = CreateCoordinator(
            CreateDisplayState(),
            new EmailInboxSnapshot(0, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out var dashboardService,
            out _,
            out _,
            out _,
            currentDisplayState: loadingState,
            refreshedDisplayState: refreshedState);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        using var viewModel = new MainWindowViewModel(coordinator, inbox);
        viewModel.OpenEventDetails(new AllDayEventDisplayState(
            "Offsite",
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            CreateDetails()));
        await viewModel.InitializeAsync();

        // Act
        await viewModel.NavigateDaysAsync(1);

        // Assert
        viewModel.EventDetails.IsOpen.Should().BeFalse();
        viewModel.Schedule.DisplayDate.Should().Be(refreshedState.DisplayDate);
        dashboardService.ShiftSelectedDateCalls.Should().Be(1);
        dashboardService.RefreshCalls.Should().Be(2);
    }

    [Fact(DisplayName = "Opening and closing event details delegates to the event details state.")]
    [Trait("Category", "Unit")]
    public void OpenEventDetailsAndCloseEventDetailsShouldUpdateEventDetailsState()
    {
        // Arrange
        using var coordinator = CreateCoordinator(
            CreateDisplayState(),
            new EmailInboxSnapshot(1, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out _,
            out _,
            out _,
            out _);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        using var viewModel = new MainWindowViewModel(coordinator, inbox);
        var details = CreateDetails();
        var eventState = new TimedEventDisplayState(
            "Standup",
            "9:00AM - 10:00AM",
            0,
            60,
            0,
            200,
            false,
            false,
            true,
            true,
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            details);

        // Act
        viewModel.OpenEventDetails(eventState);

        // Assert
        viewModel.EventDetails.SelectedEventDetails.Should().BeSameAs(details);

        // Act
        viewModel.CloseEventDetails();

        // Assert
        viewModel.EventDetails.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "Opening event details after inbox initialization uses the signed-in Google account for Meet links.")]
    [Trait("Category", "Unit")]
    public async Task OpenEventDetailsShouldUseSignedInGoogleAccountForMeetLinks()
    {
        // Arrange
        var inboxSnapshot = new EmailInboxSnapshot(
            1,
            " user@example.com ",
            new Uri("https://mail.google.com/mail/u/?authuser=user%40example.com#inbox"));
        using var coordinator = CreateCoordinator(
            CreateDisplayState(),
            inboxSnapshot,
            out _,
            out _,
            out _,
            out _);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        using var viewModel = new MainWindowViewModel(coordinator, inbox);
        var details = CreateDetails();
        var eventState = new TimedEventDisplayState(
            "Standup",
            "9:00AM - 10:00AM",
            0,
            60,
            0,
            200,
            false,
            false,
            true,
            true,
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            details);
        await viewModel.InitializeAsync();

        // Act
        viewModel.OpenEventDetails(eventState);

        // Assert
        viewModel.EventDetails.SelectedEventDetails.Should().NotBeSameAs(details);
        viewModel.EventDetails.SelectedEventDetails!.JoinUrl.Should().Be(
            new Uri("https://meet.google.com/abc-defg-hij?authuser=user%40example.com"));
    }

    [Fact(DisplayName = "Updating the available schedule width delegates to the coordinator and updates the schedule state.")]
    [Trait("Category", "Unit")]
    public void UpdateAvailableScheduleWidthShouldDelegateToCoordinatorAndUpdateScheduleState()
    {
        // Arrange
        var updatedState = CreateDisplayState(width: 500);
        using var coordinator = CreateCoordinator(
            CreateDisplayState(),
            new EmailInboxSnapshot(1, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out var dashboardService,
            out _,
            out _,
            out _,
            widthDisplayState: updatedState);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        using var viewModel = new MainWindowViewModel(coordinator, inbox);

        // Act
        viewModel.UpdateAvailableScheduleWidth(500.9);

        // Assert
        viewModel.Schedule.ScheduleCanvasWidth.Should().Be(500);
        dashboardService.GetCurrentDisplayStateCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Toggling the secondary time zone updates the layout state and persists the choice.")]
    [Trait("Category", "Unit")]
    public async Task ToggleShowSecondaryTimeZoneShouldUpdateLayoutStateAndPersistChoice()
    {
        // Arrange
        using var coordinator = CreateCoordinator(
            CreateDisplayState(secondaryTimeZoneLabel: "GMT+3"),
            new EmailInboxSnapshot(0, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out _,
            out _,
            out _,
            out _);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        var preferenceStore = new RecordingSecondaryTimeZonePreferenceStore(showSecondaryTimeZone: true);
        using var viewModel = new MainWindowViewModel(coordinator, inbox, preferenceStore);
        await viewModel.InitializeAsync();

        // Act
        var changed = viewModel.ToggleShowSecondaryTimeZone();

        // Assert
        changed.Should().BeTrue();
        viewModel.Schedule.ShowSecondaryTimeZone.Should().BeFalse();
        viewModel.Schedule.HasSecondaryTimeZone.Should().BeFalse();
        viewModel.Schedule.SecondaryTimeColumnWidth.Value.Should().Be(0);
        preferenceStore.SavedValues.Should().ContainSingle()
            .Which.Should().BeFalse();
    }

    [Fact(DisplayName = "Disposing unsubscribes from coordinator events and disposes the coordinator.")]
    [Trait("Category", "Unit")]
    public async Task DisposeShouldUnsubscribeFromCoordinatorEventsAndDisposeCoordinator()
    {
        // Arrange
        var initialState = CreateDisplayState(displayDate: new DateOnly(2026, 4, 14));
        var updatedState = CreateDisplayState(displayDate: new DateOnly(2026, 4, 20));
        using var coordinator = CreateCoordinator(
            initialState,
            new EmailInboxSnapshot(1, "user@example.com", new Uri("https://mail.google.com/mail/")),
            out var dashboardService,
            out _,
            out _,
            out var timers,
            refreshedDisplayState: updatedState);
        var inbox = new MainWindowInboxState(new RecordingGoogleWorkspaceUriBuilder());
        var viewModel = new MainWindowViewModel(coordinator, inbox);

        await viewModel.InitializeAsync();
        viewModel.Dispose();

        // Act
        await coordinator.RefreshNowAsync();

        // Assert
        viewModel.Schedule.DisplayDate.Should().Be(initialState.DisplayDate);
        timers.ClockTimer.StopCalls.Should().Be(1);
        timers.CalendarTimer.StopCalls.Should().BeGreaterThanOrEqualTo(1);
        dashboardService.RefreshCalls.Should().Be(2);
    }

    private static MainWindowDashboardCoordinator CreateCoordinator(
        DayScheduleDisplayState initialDisplayState,
        EmailInboxSnapshot inboxSnapshot,
        out RecordingDashboardService dashboardService,
        out Mock<IEmailInboxService> emailInboxService,
        out RecordingUiDispatcherTimerFactory timerFactory,
        out TimerBundle timers,
        DayScheduleDisplayState? currentDisplayState = null,
        DayScheduleDisplayState? refreshedDisplayState = null,
        DayScheduleDisplayState? widthDisplayState = null)
    {
        dashboardService = new RecordingDashboardService(
            initialDisplayState,
            currentDisplayState ?? initialDisplayState,
            refreshedDisplayState ?? initialDisplayState,
            widthDisplayState ?? initialDisplayState);
        var clockTimer = new FakeUiDispatcherTimer(TimeSpan.FromMinutes(1));
        var calendarTimer = new FakeUiDispatcherTimer(TimeSpan.FromMinutes(5));
        timers = new TimerBundle(clockTimer, calendarTimer);
        timerFactory = new RecordingUiDispatcherTimerFactory(clockTimer, calendarTimer);
        emailInboxService = new Mock<IEmailInboxService>(MockBehavior.Strict);
        emailInboxService.SetupGet(service => service.IsEnabled)
            .Returns(false);
        emailInboxService.Setup(service => service.GetInboxSnapshotAsync(
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inboxSnapshot);

        return new MainWindowDashboardCoordinator(
            dashboardService,
            emailInboxService.Object,
            timerFactory,
            CreateApplicationLifetime().Object);
    }

    private static Mock<IHostApplicationLifetime> CreateApplicationLifetime()
    {
        var applicationLifetime = new Mock<IHostApplicationLifetime>(MockBehavior.Strict);
        applicationLifetime.SetupGet(lifetime => lifetime.ApplicationStopping)
            .Returns(SharedApplicationStoppingSource.Token);
        return applicationLifetime;
    }

    private static readonly CancellationTokenSource SharedApplicationStoppingSource = new();

    private static DayScheduleDisplayState CreateDisplayState(
        DateOnly? displayDate = null,
        int width = 860,
        string statusText = "",
        bool showStatus = false,
        double? nowLineTop = null,
        string? secondaryTimeZoneLabel = null)
    {
        var day = displayDate ?? new DateOnly(2026, 4, 14);
        return new DayScheduleDisplayState(
            day,
            "April 2026",
            "Tue",
            day.Day.ToString(CultureInfo.InvariantCulture),
            "Tuesday, 14 April",
            "UTC",
            secondaryTimeZoneLabel,
            [],
            [],
            [],
            [],
            width,
            1000,
            statusText,
            showStatus,
            nowLineTop,
            "9:00AM");
    }

    private static EventDetailsDisplayState CreateDetails()
    {
        return new EventDetailsDisplayState(
            "Standup",
            "9:00AM - 10:00AM",
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            "Alice",
            "Discuss roadmap",
            new Uri("https://meet.google.com/abc-defg-hij"),
            []);
    }

    private sealed class RecordingSecondaryTimeZonePreferenceStore(bool showSecondaryTimeZone) : ISecondaryTimeZonePreferenceStore
    {
        public int LoadCalls { get; private set; }

        public List<bool> SavedValues { get; } = [];

        public bool LoadShowSecondaryTimeZone()
        {
            LoadCalls++;
            return showSecondaryTimeZone;
        }

        public void SaveShowSecondaryTimeZone(bool showSecondaryTimeZone) => SavedValues.Add(showSecondaryTimeZone);
    }

    private sealed class RecordingDashboardService(
        DayScheduleDisplayState initialDisplayState,
        DayScheduleDisplayState currentDisplayState,
        DayScheduleDisplayState refreshedDisplayState,
        DayScheduleDisplayState widthDisplayState) : IDayScheduleDashboardService
    {
        private bool _initialized;

        public bool IsCalendarEnabled => false;

        public TimeSpan CalendarRefreshInterval => TimeSpan.FromMinutes(5);

        public DateOnly CurrentLocalDate => initialDisplayState.DisplayDate;

        public int RefreshCalls { get; private set; }

        public int ShiftSelectedDateCalls { get; private set; }

        public int GetCurrentDisplayStateCalls { get; private set; }

        public DayScheduleDisplayState GetCurrentDisplayState(double? availableScheduleWidth = null)
        {
            GetCurrentDisplayStateCalls++;
            return availableScheduleWidth.HasValue
                ? widthDisplayState
                : currentDisplayState;
        }

        public void ShiftSelectedDate(int dayOffset) => ShiftSelectedDateCalls++;

        public bool TrySelectCurrentDate() => false;

        public Task<DayScheduleDisplayState> RefreshCalendarAsync(
            CalendarInteractionMode interactionMode,
            double? availableScheduleWidth,
            CancellationToken cancellationToken)
        {
            RefreshCalls++;

            if (!_initialized)
            {
                _initialized = true;
                return Task.FromResult(initialDisplayState);
            }

            return Task.FromResult(refreshedDisplayState);
        }
    }

    private sealed class RecordingGoogleWorkspaceUriBuilder : IGoogleWorkspaceUriBuilder
    {
        public int BuildCalendarDayUriCalls { get; private set; }

        public Func<DateOnly, string?, Uri> BuildCalendarDayUriHandler { get; init; } =
            static (displayDate, _) => new Uri(
                $"https://calendar.google.com/calendar/r/day/{displayDate.Year}/{displayDate.Month}/{displayDate.Day}",
                UriKind.Absolute);

        public Uri BuildCalendarDayUri(DateOnly displayDate, string? emailAddress)
        {
            BuildCalendarDayUriCalls++;
            return BuildCalendarDayUriHandler(displayDate, emailAddress);
        }

        public Uri BuildInboxUri(string? emailAddress) =>
            new("https://mail.google.com/mail/", UriKind.Absolute);
    }

    private sealed class FakeUiDispatcherTimer(TimeSpan interval) : IUiDispatcherTimer
    {
        public event EventHandler? Tick;

        public TimeSpan Interval { get; set; } = interval;

        public int StartCalls { get; private set; }

        public int StopCalls { get; private set; }

        public void StartTimer() => StartCalls++;

        public void StopTimer() => StopCalls++;

        public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
    }

    private sealed class RecordingUiDispatcherTimerFactory(
        IUiDispatcherTimer clockTimer,
        IUiDispatcherTimer calendarTimer) : IUiDispatcherTimerFactory
    {
        public IUiDispatcherTimer Create(TimeSpan interval)
        {
            return interval == TimeSpan.FromMinutes(1)
                ? clockTimer
                : interval == TimeSpan.FromMinutes(5)
                    ? calendarTimer
                    : throw new ArgumentOutOfRangeException(nameof(interval));
        }
    }

    private sealed record TimerBundle(
        FakeUiDispatcherTimer ClockTimer,
        FakeUiDispatcherTimer CalendarTimer);
}

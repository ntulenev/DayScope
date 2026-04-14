using FluentAssertions;

using Moq;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.Dashboard;
using DayScope.Application.DaySchedule;
using DayScope.Threading;
using DayScope.ViewModels;

namespace DayScope.Tests;

public sealed class MainWindowDashboardCoordinatorTests
{
    [Fact(DisplayName = "Initialization refreshes the dashboard, publishes both snapshots, and starts background timers when refresh is enabled.")]
    [Trait("Category", "Unit")]
    public async Task InitializeAsyncShouldRefreshPublishSnapshotsAndStartTimersWhenRefreshIsEnabled()
    {
        // Arrange
        var displayState = CreateDisplayState();
        var inboxSnapshot = CreateInboxSnapshot(unreadCount: 3);
        var clockTimer = new FakeUiDispatcherTimer(TimeSpan.FromMinutes(1));
        var calendarTimer = new FakeUiDispatcherTimer(TimeSpan.FromMinutes(5));
        var createTimerCalls = 0;
        var refreshCalls = 0;
        var inboxCalls = 0;
        var timerFactory = new Mock<IUiDispatcherTimerFactory>(MockBehavior.Strict);
        timerFactory.Setup(factory => factory.Create(TimeSpan.FromMinutes(1)))
            .Callback(() => createTimerCalls++)
            .Returns(clockTimer);
        timerFactory.Setup(factory => factory.Create(TimeSpan.FromMinutes(5)))
            .Callback(() => createTimerCalls++)
            .Returns(calendarTimer);
        var dashboardService = new Mock<IDayScheduleDashboardService>(MockBehavior.Strict);
        dashboardService.SetupGet(service => service.CalendarRefreshInterval)
            .Returns(TimeSpan.FromMinutes(5));
        dashboardService.SetupGet(service => service.IsCalendarEnabled)
            .Returns(true);
        dashboardService.Setup(service => service.RefreshCalendarAsync(
                CalendarInteractionMode.Interactive,
                860,
                CancellationToken.None))
            .Callback(() => refreshCalls++)
            .ReturnsAsync(displayState);
        var emailInboxService = new Mock<IEmailInboxService>(MockBehavior.Strict);
        emailInboxService.Setup(email => email.GetInboxSnapshotAsync(true, CancellationToken.None))
            .Callback(() => inboxCalls++)
            .ReturnsAsync(inboxSnapshot);
        using var coordinator = new MainWindowDashboardCoordinator(
            dashboardService.Object,
            emailInboxService.Object,
            timerFactory.Object);
        DayScheduleDisplayState? publishedDisplayState = null;
        EmailInboxSnapshot? publishedSnapshot = null;
        coordinator.DisplayStateChanged += (_, args) => publishedDisplayState = args.DisplayState;
        coordinator.InboxSnapshotChanged += (_, args) => publishedSnapshot = args.Snapshot;

        // Act
        await coordinator.InitializeAsync();

        // Assert
        publishedDisplayState.Should().BeSameAs(displayState);
        publishedSnapshot.Should().BeSameAs(inboxSnapshot);
        clockTimer.StartCalls.Should().Be(1);
        calendarTimer.StopCalls.Should().Be(1);
        calendarTimer.StartCalls.Should().Be(1);
        refreshCalls.Should().Be(1);
        inboxCalls.Should().Be(1);
        createTimerCalls.Should().Be(2);
    }

    [Fact(DisplayName = "Day navigation publishes the current loading state before the refreshed state.")]
    [Trait("Category", "Unit")]
    public async Task NavigateDaysAsyncShouldPublishLoadingStateBeforeRefreshedState()
    {
        // Arrange
        var loadingState = CreateDisplayState(statusText: "Loading schedule...", showStatus: true);
        var refreshedState = CreateDisplayState(displayDate: new DateOnly(2026, 4, 15));
        var publishedStates = new List<DayScheduleDisplayState>();
        var shiftSelectedDateCalls = 0;
        var getCurrentDisplayStateCalls = 0;
        var refreshCalls = 0;
        var inboxCalls = 0;
        var timerFactory = CreateTimerFactory(
            new FakeUiDispatcherTimer(TimeSpan.FromMinutes(1)),
            new FakeUiDispatcherTimer(TimeSpan.FromMinutes(5)));
        var dashboardService = new Mock<IDayScheduleDashboardService>(MockBehavior.Strict);
        dashboardService.SetupGet(service => service.CalendarRefreshInterval)
            .Returns(TimeSpan.FromMinutes(5));
        dashboardService.SetupGet(service => service.IsCalendarEnabled)
            .Returns(false);
        dashboardService.Setup(service => service.ShiftSelectedDate(1))
            .Callback(() => shiftSelectedDateCalls++);
        dashboardService.Setup(service => service.GetCurrentDisplayState(860))
            .Callback(() => getCurrentDisplayStateCalls++)
            .Returns(loadingState);
        dashboardService.Setup(service => service.RefreshCalendarAsync(
                CalendarInteractionMode.Interactive,
                860,
                CancellationToken.None))
            .Callback(() => refreshCalls++)
            .ReturnsAsync(refreshedState);
        var emailInboxService = new Mock<IEmailInboxService>(MockBehavior.Strict);
        emailInboxService.SetupGet(email => email.IsEnabled)
            .Returns(false);
        emailInboxService.Setup(email => email.GetInboxSnapshotAsync(true, CancellationToken.None))
            .Callback(() => inboxCalls++)
            .ReturnsAsync(CreateInboxSnapshot(unreadCount: 0));
        using var coordinator = new MainWindowDashboardCoordinator(
            dashboardService.Object,
            emailInboxService.Object,
            timerFactory);
        coordinator.DisplayStateChanged += (_, args) => publishedStates.Add(args.DisplayState);

        // Act
        await coordinator.NavigateDaysAsync(1);

        // Assert
        publishedStates.Should().ContainInOrder(loadingState, refreshedState);
        shiftSelectedDateCalls.Should().Be(1);
        getCurrentDisplayStateCalls.Should().Be(1);
        refreshCalls.Should().Be(1);
        inboxCalls.Should().Be(1);
        timerFactory.CreateCalls.Should().Be(2);
    }

    [Fact(DisplayName = "Schedule-width updates normalize the measured width and publish only when the width changes.")]
    [Trait("Category", "Unit")]
    public void UpdateAvailableScheduleWidthShouldNormalizeWidthAndPublishOnlyWhenTheWidthChanges()
    {
        // Arrange
        var displayState = CreateDisplayState(width: 500);
        var publishedStates = new List<DayScheduleDisplayState>();
        var getCurrentDisplayStateCalls = 0;
        var timerFactory = CreateTimerFactory(
            new FakeUiDispatcherTimer(TimeSpan.FromMinutes(1)),
            new FakeUiDispatcherTimer(TimeSpan.FromMinutes(5)));
        var dashboardService = new Mock<IDayScheduleDashboardService>(MockBehavior.Strict);
        dashboardService.SetupGet(service => service.CalendarRefreshInterval)
            .Returns(TimeSpan.FromMinutes(5));
        dashboardService.Setup(service => service.GetCurrentDisplayState(500))
            .Callback(() => getCurrentDisplayStateCalls++)
            .Returns(displayState);
        var emailInboxService = new Mock<IEmailInboxService>(MockBehavior.Strict);
        using var coordinator = new MainWindowDashboardCoordinator(
            dashboardService.Object,
            emailInboxService.Object,
            timerFactory);
        coordinator.DisplayStateChanged += (_, args) => publishedStates.Add(args.DisplayState);

        // Act
        coordinator.UpdateAvailableScheduleWidth(500.9);
        coordinator.UpdateAvailableScheduleWidth(500.2);

        // Assert
        publishedStates.Should().ContainSingle()
            .Which.Should().BeSameAs(displayState);
        getCurrentDisplayStateCalls.Should().Be(1);
        timerFactory.CreateCalls.Should().Be(2);
    }

    private static RecordingUiDispatcherTimerFactory CreateTimerFactory(
        IUiDispatcherTimer clockTimer,
        IUiDispatcherTimer calendarTimer)
    {
        var timerFactory = new RecordingUiDispatcherTimerFactory(clockTimer, calendarTimer);
        return timerFactory;
    }

    private static DayScheduleDisplayState CreateDisplayState(
        DateOnly? displayDate = null,
        string statusText = "",
        bool showStatus = false,
        int width = 860)
    {
        return new DayScheduleDisplayState(
            displayDate ?? new DateOnly(2026, 4, 14),
            "April 2026",
            "Tue",
            "14",
            "Tuesday, 14 April",
            "UTC",
            null,
            [],
            [],
            [],
            [],
            width,
            1000,
            statusText,
            showStatus,
            null,
            "9:00AM");
    }

    private static EmailInboxSnapshot CreateInboxSnapshot(int? unreadCount)
        => new(unreadCount, "user@example.com", new Uri("https://mail.google.com/mail/"));

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
        public int CreateCalls { get; private set; }

        public IUiDispatcherTimer Create(TimeSpan interval)
        {
            CreateCalls++;

            return interval == TimeSpan.FromMinutes(1)
                ? clockTimer
                : interval == TimeSpan.FromMinutes(5)
                    ? calendarTimer
                    : throw new ArgumentOutOfRangeException(nameof(interval));
        }
    }
}

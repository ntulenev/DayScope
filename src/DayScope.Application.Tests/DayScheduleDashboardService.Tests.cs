using FluentAssertions;

using Microsoft.Extensions.Options;

using Moq;

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Application.Dashboard;
using DayScope.Domain.Calendar;
using DayScope.Domain.Configuration;

namespace DayScope.Application.Tests;

public sealed class DayScheduleDashboardServiceTests
{
    [Fact(DisplayName = "Refreshing the dashboard loads events for the selected date in the local time zone.")]
    [Trait("Category", "Unit")]
    public async Task RefreshCalendarAsyncShouldLoadEventsForSelectedDateInLocalTimeZone()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero);
        var selectedDate = new DateOnly(2026, 4, 14);
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var getEventsCalls = 0;
        var clockService = new Mock<IClockService>(MockBehavior.Strict);
        clockService.SetupGet(service => service.Now)
            .Returns(now);
        var calendarService = new Mock<ICalendarService>(MockBehavior.Strict);
        calendarService.SetupGet(service => service.IsEnabled)
            .Returns(true);
        var localTimeZoneProvider = new Mock<ILocalTimeZoneProvider>(MockBehavior.Strict);
        localTimeZoneProvider.SetupGet(provider => provider.LocalTimeZone)
            .Returns(TimeZoneInfo.Utc);
        calendarService.Setup(service => service.GetEventsForDateAsync(
                selectedDate,
                TimeZoneInfo.Utc,
                CalendarInteractionMode.Interactive,
                token))
            .Callback(() => getEventsCalls++)
            .ReturnsAsync(CalendarLoadResult.Success(new CalendarAgenda([CreateEvent(now)])));
        var service = CreateService(
            clockService.Object,
            calendarService.Object,
            localTimeZoneProvider.Object,
            refreshMinutes: 7);

        // Act
        var state = await service.RefreshCalendarAsync(
            CalendarInteractionMode.Interactive,
            540,
            token);

        // Assert
        state.DisplayDate.Should().Be(selectedDate);
        state.TimedEvents.Should().ContainSingle();
        state.TimedEvents[0].Title.Should().Be("Standup");
        service.IsCalendarEnabled.Should().BeTrue();
        service.CalendarRefreshInterval.Should().Be(TimeSpan.FromMinutes(7));
        getEventsCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Shifting the selected date resets the display state to loading for the new day.")]
    [Trait("Category", "Unit")]
    public void ShiftSelectedDateShouldResetDisplayStateToLoadingForTheShiftedDay()
    {
        // Arrange
        var clockService = new Mock<IClockService>(MockBehavior.Strict);
        clockService.SetupGet(service => service.Now)
            .Returns(new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero));
        var calendarService = new Mock<ICalendarService>(MockBehavior.Strict);
        var localTimeZoneProvider = new Mock<ILocalTimeZoneProvider>(MockBehavior.Strict);
        localTimeZoneProvider.SetupGet(provider => provider.LocalTimeZone)
            .Returns(TimeZoneInfo.Utc);
        var service = CreateService(
            clockService.Object,
            calendarService.Object,
            localTimeZoneProvider.Object);

        // Act
        service.ShiftSelectedDate(1);
        var state = service.GetCurrentDisplayState();

        // Assert
        state.DisplayDate.Should().Be(new DateOnly(2026, 4, 15));
        state.StatusText.Should().Be("Loading schedule...");
        state.ShowStatus.Should().BeTrue();
    }

    [Fact(DisplayName = "Refreshing while another refresh is already running returns the current display state.")]
    [Trait("Category", "Unit")]
    public async Task RefreshCalendarAsyncShouldReturnCurrentDisplayStateWhenRefreshIsAlreadyRunning()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero);
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var refreshCompletionSource = new TaskCompletionSource<CalendarLoadResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var getEventsCalls = 0;
        var clockService = new Mock<IClockService>(MockBehavior.Strict);
        clockService.SetupGet(service => service.Now)
            .Returns(now);
        var calendarService = new Mock<ICalendarService>(MockBehavior.Strict);
        calendarService.Setup(service => service.GetEventsForDateAsync(
                new DateOnly(2026, 4, 14),
                TimeZoneInfo.Utc,
                CalendarInteractionMode.Interactive,
                token))
            .Callback(() => getEventsCalls++)
            .Returns(refreshCompletionSource.Task);
        var localTimeZoneProvider = new Mock<ILocalTimeZoneProvider>(MockBehavior.Strict);
        localTimeZoneProvider.SetupGet(provider => provider.LocalTimeZone)
            .Returns(TimeZoneInfo.Utc);
        var service = CreateService(
            clockService.Object,
            calendarService.Object,
            localTimeZoneProvider.Object);
        var firstRefresh = service.RefreshCalendarAsync(
            CalendarInteractionMode.Interactive,
            600,
            token);

        // Act
        var state = await service.RefreshCalendarAsync(
            CalendarInteractionMode.Interactive,
            620,
            token);

        // Assert
        state.DisplayDate.Should().Be(new DateOnly(2026, 4, 14));
        state.StatusText.Should().Be("Loading today's schedule...");
        state.ScheduleCanvasWidth.Should().Be(620);
        getEventsCalls.Should().Be(1);

        refreshCompletionSource.SetResult(CalendarLoadResult.FromStatus(CalendarLoadStatus.NoEvents));
        await firstRefresh;
    }

    [Fact(DisplayName = "Refreshing keeps the last successful agenda visible when the network is temporarily unavailable.")]
    [Trait("Category", "Unit")]
    public async Task RefreshCalendarAsyncShouldKeepTheLastSuccessfulAgendaWhenNetworkIsUnavailable()
    {
        // Arrange
        var now = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero);
        using var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;
        var agenda = new CalendarAgenda([CreateEvent(now)]);
        var clockService = new Mock<IClockService>(MockBehavior.Strict);
        clockService.SetupGet(service => service.Now)
            .Returns(now);
        var calendarService = new Mock<ICalendarService>(MockBehavior.Strict);
        calendarService.SetupSequence(service => service.GetEventsForDateAsync(
                new DateOnly(2026, 4, 14),
                TimeZoneInfo.Utc,
                CalendarInteractionMode.Background,
                token))
            .ReturnsAsync(CalendarLoadResult.Success(agenda))
            .ReturnsAsync(CalendarLoadResult.FromStatus(CalendarLoadStatus.Unavailable));
        var localTimeZoneProvider = new Mock<ILocalTimeZoneProvider>(MockBehavior.Strict);
        localTimeZoneProvider.SetupGet(provider => provider.LocalTimeZone)
            .Returns(TimeZoneInfo.Utc);
        var service = CreateService(
            clockService.Object,
            calendarService.Object,
            localTimeZoneProvider.Object);

        // Act
        var initialState = await service.RefreshCalendarAsync(
            CalendarInteractionMode.Background,
            540,
            token);
        var offlineState = await service.RefreshCalendarAsync(
            CalendarInteractionMode.Background,
            540,
            token);

        // Assert
        initialState.TimedEvents.Should().ContainSingle();
        offlineState.TimedEvents.Should().ContainSingle();
        offlineState.TimedEvents[0].Title.Should().Be("Standup");
        offlineState.StatusText.Should().Be(
            "No internet connection. DayScope will retry automatically when it's back.");
        offlineState.ShowStatus.Should().BeTrue();
    }

    private static DayScheduleDashboardService CreateService(
        IClockService clockService,
        ICalendarService calendarService,
        ILocalTimeZoneProvider localTimeZoneProvider,
        int refreshMinutes = 5)
    {
        return new DayScheduleDashboardService(
            clockService,
            calendarService,
            localTimeZoneProvider,
            Options.Create(new DayScheduleSettings()),
            Options.Create(new GoogleCalendarSettings { RefreshMinutes = refreshMinutes }));
    }

    private static CalendarEvent CreateEvent(DateTimeOffset now)
    {
        return new CalendarEvent(
            "Standup",
            now.AddHours(1),
            now.AddHours(2),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            "Alice",
            "alice@example.com",
            null,
            null,
            []);
    }
}

using FluentAssertions;

using DayScope.Application.Calendar;
using DayScope.Application.DaySchedule;
using DayScope.Domain.Calendar;
using DayScope.Domain.Configuration;

namespace DayScope.Application.Tests;

public sealed class DayScheduleDisplayBuilderTests
{
    [Fact(DisplayName = "Build composes schedule display state with primary and secondary timeline data.")]
    [Trait("Category", "Unit")]
    public void BuildShouldComposeScheduleDisplayStateWithTimelineData()
    {
        // Arrange
        var localZone = TimeZoneInfo.CreateCustomTimeZone(
            "Test/LocalPlusTwo",
            TimeSpan.FromHours(2),
            "Test Local Plus Two",
            "Test Local Plus Two");
        var selectedDate = new DateOnly(2026, 4, 14);
        var settings = new DayScheduleSettings
        {
            StartHour = 6,
            EndHour = 9,
            HourHeight = 60,
            ScheduleCanvasWidth = 860,
            PrimaryTimeZoneLabel = " Local ",
            SecondaryTimeZoneId = "UTC",
            SecondaryTimeZoneLabel = " Remote "
        };
        var allDayEvent = CreateEvent(
            "Release day",
            new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.FromHours(2)),
            isAllDay: true);
        var timedEvent = CreateEvent(
            "Planning",
            new DateTimeOffset(2026, 4, 14, 7, 30, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 4, 14, 8, 0, 0, TimeSpan.FromHours(2)),
            isAllDay: false);
        var loadResult = CalendarLoadResult.Success(new CalendarAgenda([timedEvent, allDayEvent]));

        // Act
        var state = DayScheduleDisplayBuilder.Build(
            loadResult,
            settings,
            new DateTimeOffset(2026, 4, 14, 7, 15, 0, TimeSpan.FromHours(2)),
            selectedDate,
            localZone,
            500.9);

        // Assert
        state.DisplayDate.Should().Be(selectedDate);
        state.MonthTitle.Should().Be("April 2026");
        state.DayTitle.Should().Be("Tue");
        state.DayNumberText.Should().Be("14");
        state.DateText.Should().Be("Tuesday, 14 April");
        state.PrimaryTimeZoneLabel.Should().Be("Local");
        state.SecondaryTimeZoneLabel.Should().Be("Remote");
        state.PrimaryTimelineHours.Select(hour => hour.Text)
            .Should().ContainInOrder("6AM", "7AM", "8AM", "9AM");
        state.PrimaryTimelineHours.Select(hour => hour.Top)
            .Should().ContainInOrder(0, 60, 120, 180);
        state.SecondaryTimelineHours.Select(hour => hour.Text)
            .Should().ContainInOrder("4AM", "5AM", "6AM", "7AM");
        state.AllDayEvents.Should().ContainSingle();
        state.AllDayEvents[0].Title.Should().Be("Release day");
        state.TimedEvents.Should().ContainSingle();
        state.TimedEvents[0].Title.Should().Be("Planning");
        state.TimedEvents[0].Top.Should().Be(90);
        state.TimedEvents[0].Height.Should().Be(30);
        state.TimedEvents[0].ScheduleText.Should().Be("7:30AM-8:00AM");
        state.ScheduleCanvasWidth.Should().Be(500);
        state.TimelineHeight.Should().Be(198);
        state.StatusText.Should().BeEmpty();
        state.ShowStatus.Should().BeFalse();
        state.NowLineTop.Should().Be(75);
        state.NowLineText.Should().Be("7:15AM");
    }

    [Fact(DisplayName = "Build omits secondary timeline data and reports an empty future day.")]
    [Trait("Category", "Unit")]
    public void BuildShouldOmitSecondaryTimelineDataAndReportAnEmptyFutureDay()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 8,
            EndHour = 10,
            HourHeight = 50,
            ScheduleCanvasWidth = 480
        };
        var selectedDate = new DateOnly(2026, 4, 15);

        // Act
        var state = DayScheduleDisplayBuilder.Build(
            CalendarLoadResult.Success(new CalendarAgenda([])),
            settings,
            new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
            selectedDate,
            TimeZoneInfo.Utc);

        // Assert
        state.DisplayDate.Should().Be(selectedDate);
        state.PrimaryTimeZoneLabel.Should().Be("UTC+00:00");
        state.SecondaryTimeZoneLabel.Should().BeNull();
        state.PrimaryTimelineHours.Select(hour => hour.Text)
            .Should().ContainInOrder("8AM", "9AM", "10AM");
        state.SecondaryTimelineHours.Should().BeEmpty();
        state.AllDayEvents.Should().BeEmpty();
        state.TimedEvents.Should().BeEmpty();
        state.ScheduleCanvasWidth.Should().Be(480);
        state.TimelineHeight.Should().Be(118);
        state.StatusText.Should().Be("No events scheduled for this day.");
        state.ShowStatus.Should().BeTrue();
        state.NowLineTop.Should().BeNull();
        state.NowLineText.Should().Be("9:00AM");
    }

    private static CalendarEvent CreateEvent(
        string title,
        DateTimeOffset start,
        DateTimeOffset end,
        bool isAllDay)
    {
        return new CalendarEvent(
            title,
            start,
            end,
            isAllDay,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            null,
            null,
            null,
            null,
            []);
    }
}

using DayScope.Application.DaySchedule;
using DayScope.ViewModels;

using FluentAssertions;

namespace DayScope.Tests;

public sealed class ScheduleClipboardTextBuilderTests
{
    [Fact(DisplayName = "Build formats active day all-day and timed events for the clipboard.")]
    [Trait("Category", "Unit")]
    public void BuildShouldFormatActiveDayEventsForClipboard()
    {
        // Arrange
        var schedule = new MainWindowScheduleState();
        schedule.Apply(CreateDisplayState(
            allDayEvents:
            [
                new AllDayEventDisplayState(
                    "Planning",
                    EventAppearance.Accepted,
                    "Confirmed",
                    string.Empty,
                    CreateDetails("Planning", "All day"))
            ],
            timedEvents:
            [
                CreateTimedEvent("Review", "2:00PM-3:00PM", top: 360, statusLabel: "Maybe"),
                CreateTimedEvent("Standup", "9:00AM-9:30AM", top: 60, statusLabel: "Confirmed")
            ]));

        // Act
        var result = ScheduleClipboardTextBuilder.Build(schedule);

        // Assert
        result.Should().Be(string.Join(
            Environment.NewLine,
            "Tuesday, April 14, 2026",
            "Time zone: UTC",
            string.Empty,
            "All day",
            "- Planning (Confirmed)",
            string.Empty,
            "Schedule",
            "- 9:00AM-9:30AM: Standup (Confirmed)",
            "- 2:00PM-3:00PM: Review (Maybe)"));
    }

    [Fact(DisplayName = "Build formats an empty active day without event sections.")]
    [Trait("Category", "Unit")]
    public void BuildShouldFormatEmptyDay()
    {
        // Arrange
        var schedule = new MainWindowScheduleState();
        schedule.Apply(CreateDisplayState(allDayEvents: [], timedEvents: []));

        // Act
        var result = ScheduleClipboardTextBuilder.Build(schedule);

        // Assert
        result.Should().Be(string.Join(
            Environment.NewLine,
            "Tuesday, April 14, 2026",
            "Time zone: UTC",
            string.Empty,
            "No events scheduled."));
    }

    private static DayScheduleDisplayState CreateDisplayState(
        IReadOnlyList<AllDayEventDisplayState>? allDayEvents = null,
        IReadOnlyList<TimedEventDisplayState>? timedEvents = null)
    {
        return new DayScheduleDisplayState(
            new DateOnly(2026, 4, 14),
            "April 2026",
            "Tue",
            "14",
            "Tuesday, 14 April",
            "UTC",
            null,
            [],
            [],
            allDayEvents ?? [],
            timedEvents ?? [],
            860,
            1000,
            string.Empty,
            false,
            null,
            string.Empty);
    }

    private static TimedEventDisplayState CreateTimedEvent(
        string title,
        string scheduleText,
        double top,
        string statusLabel)
    {
        return new TimedEventDisplayState(
            title,
            scheduleText,
            top,
            40,
            0,
            200,
            false,
            false,
            true,
            true,
            EventAppearance.Accepted,
            statusLabel,
            string.Empty,
            CreateDetails(title, scheduleText));
    }

    private static EventDetailsDisplayState CreateDetails(string title, string scheduleText)
    {
        return new EventDetailsDisplayState(
            title,
            scheduleText,
            EventAppearance.Accepted,
            "Confirmed",
            string.Empty,
            "Alice",
            string.Empty,
            null,
            []);
    }
}

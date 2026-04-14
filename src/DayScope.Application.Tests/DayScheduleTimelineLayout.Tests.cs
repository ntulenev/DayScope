using FluentAssertions;

using DayScope.Application.DaySchedule;
using DayScope.Domain.Calendar;

namespace DayScope.Application.Tests;

public sealed class DayScheduleTimelineLayoutTests
{
    [Fact(DisplayName = "Timed-event layout excludes all-day events and returns an empty list when nothing intersects the visible range.")]
    [Trait("Category", "Unit")]
    public void BuildTimedEventsShouldExcludeAllDayEventsAndReturnEmptyWhenNothingIntersects()
    {
        // Arrange
        var events = new[]
        {
            CreateEvent(
                "All day",
                new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero),
                isAllDay: true),
            CreateEvent(
                "Before range",
                new DateTimeOffset(2026, 4, 14, 4, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 14, 5, 0, 0, TimeSpan.Zero))
        };

        // Act
        var result = DayScheduleTimelineLayout.BuildTimedEvents(
            events,
            new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero),
            420,
            80,
            TimeZoneInfo.Utc);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(DisplayName = "Timed-event layout stacks overlapping events into columns and keeps later non-overlapping events full width.")]
    [Trait("Category", "Unit")]
    public void BuildTimedEventsShouldStackOverlappingEventsIntoColumnsAndKeepLaterEventsFullWidth()
    {
        // Arrange
        var timelineStart = new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero);
        var timelineEnd = new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero);
        var overlappingLeft = CreateEvent(
            "Event A",
            new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero));
        var overlappingRight = CreateEvent(
            "Event B",
            new DateTimeOffset(2026, 4, 14, 9, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 10, 30, 0, TimeSpan.Zero));
        var laterFullWidth = CreateEvent(
            "Event C",
            new DateTimeOffset(2026, 4, 14, 11, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 12, 0, 0, TimeSpan.Zero));

        // Act
        var result = DayScheduleTimelineLayout.BuildTimedEvents(
            [laterFullWidth, overlappingRight, overlappingLeft],
            timelineStart,
            timelineEnd,
            420,
            80,
            TimeZoneInfo.Utc);

        // Assert
        result.Should().HaveCount(3);
        result.Select(item => item.Title).Should().ContainInOrder("Event A", "Event B", "Event C");

        result[0].Left.Should().Be(0);
        result[0].Width.Should().Be(205);
        result[0].IsCompact.Should().BeTrue();
        result[0].ShowScheduleText.Should().BeFalse();
        result[0].ShowStatusBadge.Should().BeTrue();

        result[1].Left.Should().Be(215);
        result[1].Width.Should().Be(205);
        result[1].IsCompact.Should().BeTrue();

        result[2].Left.Should().Be(0);
        result[2].Width.Should().Be(420);
        result[2].IsCompact.Should().BeFalse();
        result[2].ShowScheduleText.Should().BeTrue();
        result[2].ShowStatusBadge.Should().BeTrue();
    }

    [Fact(DisplayName = "Timed-event layout marks very short events as micro and hides schedule text and status badges.")]
    [Trait("Category", "Unit")]
    public void BuildTimedEventsShouldMarkVeryShortEventsAsMicroAndHideScheduleTextAndStatusBadges()
    {
        // Arrange
        var shortEvent = CreateEvent(
            "Short",
            new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 9, 10, 0, TimeSpan.Zero));

        // Act
        var result = DayScheduleTimelineLayout.BuildTimedEvents(
            [shortEvent],
            new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero),
            420,
            80,
            TimeZoneInfo.Utc);

        // Assert
        result.Should().ContainSingle();
        result[0].Height.Should().Be(20);
        result[0].IsMicro.Should().BeTrue();
        result[0].IsCompact.Should().BeTrue();
        result[0].ShowScheduleText.Should().BeFalse();
        result[0].ShowStatusBadge.Should().BeFalse();
    }

    private static CalendarEvent CreateEvent(
        string title,
        DateTimeOffset start,
        DateTimeOffset end,
        bool isAllDay = false)
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

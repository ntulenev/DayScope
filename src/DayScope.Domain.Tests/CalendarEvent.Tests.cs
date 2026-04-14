using FluentAssertions;

using DayScope.Domain.Calendar;

namespace DayScope.Domain.Tests;

public sealed class CalendarEventTests
{
    [Fact(DisplayName = "The constructor normalizes display text and ignores non-absolute join links.")]
    [Trait("Category", "Unit")]
    public void CtorShouldNormalizeDisplayTextAndIgnoreNonAbsoluteJoinLinks()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero);

        // Act
        var calendarEvent = new CalendarEvent(
            "   ",
            start,
            null,
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            " Alice ",
            " alice@example.com ",
            " Line 1\r\nLine 2  ",
            new Uri("/meet", UriKind.Relative),
            [new CalendarEventParticipant(" Bob ", " bob@example.com ", CalendarParticipationStatus.Tentative, false)]);

        // Assert
        calendarEvent.Title.Should().Be("Untitled event");
        calendarEvent.OrganizerName.Should().Be("Alice");
        calendarEvent.OrganizerEmail.Should().Be("alice@example.com");
        calendarEvent.Description.Should().Be("Line 1\nLine 2");
        calendarEvent.JoinUrl.Should().BeNull();
        calendarEvent.Participants.Should().ContainSingle();
        calendarEvent.Participants[0].DisplayName.Should().Be("Bob");
    }

    [Fact(DisplayName = "Timed events fall back to a thirty-minute duration when the end time is missing or invalid.")]
    [Trait("Category", "Unit")]
    public void CtorShouldFallbackToThirtyMinutesWhenTimedEventEndIsMissingOrInvalid()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero);

        // Act
        var calendarEvent = new CalendarEvent(
            "Standup",
            start,
            start.AddMinutes(-15),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            null,
            null,
            null,
            null,
            []);

        // Assert
        calendarEvent.End.Should().Be(start.AddMinutes(30));
        calendarEvent.EffectiveEnd.Should().Be(start.AddMinutes(30));
    }

    [Fact(DisplayName = "All-day events fall back to a one-day duration when the end time is missing.")]
    [Trait("Category", "Unit")]
    public void EffectiveEndShouldFallbackToOneDayWhenAllDayEventEndIsMissing()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero);

        // Act
        var calendarEvent = new CalendarEvent(
            "Focus day",
            start,
            null,
            true,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.FocusTime,
            null,
            "alice@example.com",
            null,
            null,
            []);

        // Assert
        calendarEvent.EffectiveEnd.Should().Be(start.AddDays(1));
    }

    [Fact(DisplayName = "The organizer display label prefers the organizer name and falls back to the email address.")]
    [Trait("Category", "Unit")]
    public void OrganizerDisplayLabelShouldPreferNameAndFallbackToEmail()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero);
        var namedEvent = new CalendarEvent(
            "Standup",
            start,
            start.AddMinutes(30),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            " Alice ",
            "alice@example.com",
            null,
            null,
            []);
        var emailOnlyEvent = new CalendarEvent(
            "Standup",
            start,
            start.AddMinutes(30),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            null,
            " alice@example.com ",
            null,
            null,
            []);

        // Act

        // Assert
        namedEvent.OrganizerDisplayLabel.Should().Be("Alice");
        emailOnlyEvent.OrganizerDisplayLabel.Should().Be("alice@example.com");
    }

    [Fact(DisplayName = "Events report overlap only when their effective duration intersects the requested range.")]
    [Trait("Category", "Unit")]
    public void IntersectsShouldReportOverlapOnlyForIntersectingRanges()
    {
        // Arrange
        var calendarEvent = new CalendarEvent(
            "Standup",
            new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            null,
            null,
            null,
            null,
            []);

        // Act
        var overlapping = calendarEvent.Intersects(
            new DateTimeOffset(2026, 4, 14, 9, 30, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 10, 30, 0, TimeSpan.Zero));
        var touchingBoundary = calendarEvent.Intersects(
            new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 11, 0, 0, TimeSpan.Zero));

        // Assert
        overlapping.Should().BeTrue();
        touchingBoundary.Should().BeFalse();
    }

    [Fact(DisplayName = "Intersect checks throw when the range end does not follow the range start.")]
    [Trait("Category", "Unit")]
    public void IntersectsShouldThrowWhenRangeEndDoesNotFollowRangeStart()
    {
        // Arrange
        var calendarEvent = new CalendarEvent(
            "Standup",
            new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            null,
            null,
            null,
            null,
            []);

        // Act
        var action = () => calendarEvent.Intersects(
            new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero));

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Absolute join links are preserved and blank descriptions normalize to null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldPreserveAbsoluteJoinLinksAndNormalizeBlankDescriptionsToNull()
    {
        // Arrange
        var start = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero);
        var joinUri = new Uri("https://meet.google.com/abc-defg-hij");

        // Act
        var calendarEvent = new CalendarEvent(
            " Standup ",
            start,
            start.AddMinutes(30),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            " ",
            " ",
            " ",
            joinUri,
            null);

        // Assert
        calendarEvent.Title.Should().Be("Standup");
        calendarEvent.Description.Should().BeNull();
        calendarEvent.JoinUrl.Should().Be(joinUri);
        calendarEvent.OrganizerDisplayLabel.Should().BeNull();
        calendarEvent.Participants.Should().BeEmpty();
    }
}

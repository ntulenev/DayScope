using FluentAssertions;

using DayScope.Domain.Calendar;

namespace DayScope.Domain.Tests;

public sealed class CalendarEventParticipantTests
{
    [Fact(DisplayName = "The constructor trims participant fields and the display label prefers the display name.")]
    [Trait("Category", "Unit")]
    public void CtorShouldTrimParticipantFieldsAndPreferDisplayName()
    {
        // Arrange

        // Act
        var participant = new CalendarEventParticipant(
            " Alice ",
            " alice@example.com ",
            CalendarParticipationStatus.Accepted,
            isSelf: true);

        // Assert
        participant.DisplayName.Should().Be("Alice");
        participant.Email.Should().Be("alice@example.com");
        participant.DisplayLabel.Should().Be("Alice");
        participant.IsSelf.Should().BeTrue();
    }

    [Fact(DisplayName = "The display label falls back to the email address and then to an unknown placeholder.")]
    [Trait("Category", "Unit")]
    public void DisplayLabelShouldFallbackToEmailAndThenUnknownParticipant()
    {
        // Arrange
        var emailOnlyParticipant = new CalendarEventParticipant(
            null,
            " bob@example.com ",
            CalendarParticipationStatus.Tentative,
            isSelf: false);
        var unknownParticipant = new CalendarEventParticipant(
            " ",
            " ",
            CalendarParticipationStatus.Declined,
            isSelf: false);

        // Act

        // Assert
        emailOnlyParticipant.DisplayLabel.Should().Be("bob@example.com");
        unknownParticipant.DisplayName.Should().BeNull();
        unknownParticipant.Email.Should().BeNull();
        unknownParticipant.DisplayLabel.Should().Be("Unknown participant");
    }
}

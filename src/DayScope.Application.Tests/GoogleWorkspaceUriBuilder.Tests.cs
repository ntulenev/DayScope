using FluentAssertions;

using DayScope.Application.Google;

namespace DayScope.Application.Tests;

public sealed class GoogleWorkspaceUriBuilderTests
{
    [Fact(DisplayName = "Calendar day links omit authuser when the email address is blank.")]
    [Trait("Category", "Unit")]
    public void BuildCalendarDayUriShouldOmitAuthUserWhenEmailIsBlank()
    {
        // Arrange
        var builder = new GoogleWorkspaceUriBuilder();
        var displayDate = new DateOnly(2026, 4, 14);

        // Act
        var uri = builder.BuildCalendarDayUri(displayDate, "   ");

        // Assert
        uri.Should().Be(new Uri("https://calendar.google.com/calendar/r/day/2026/4/14"));
    }

    [Fact(DisplayName = "Calendar day links include a trimmed authuser query when the email address is provided.")]
    [Trait("Category", "Unit")]
    public void BuildCalendarDayUriShouldIncludeTrimmedAuthUserWhenEmailIsProvided()
    {
        // Arrange
        var builder = new GoogleWorkspaceUriBuilder();
        var displayDate = new DateOnly(2026, 4, 14);

        // Act
        var uri = builder.BuildCalendarDayUri(displayDate, " person+test@example.com ");

        // Assert
        uri.Should().Be(new Uri(
            "https://calendar.google.com/calendar/r/day/2026/4/14?authuser=person%2Btest%40example.com"));
    }

    [Fact(DisplayName = "Inbox links return the default Gmail URI when the email address is blank.")]
    [Trait("Category", "Unit")]
    public void BuildInboxUriShouldReturnDefaultUriWhenEmailIsBlank()
    {
        // Arrange
        var builder = new GoogleWorkspaceUriBuilder();

        // Act
        var uri = builder.BuildInboxUri(null);

        // Assert
        uri.Should().Be(new Uri("https://mail.google.com/mail/"));
    }

    [Fact(DisplayName = "Inbox links include a trimmed authuser query when the email address is provided.")]
    [Trait("Category", "Unit")]
    public void BuildInboxUriShouldIncludeTrimmedAuthUserWhenEmailIsProvided()
    {
        // Arrange
        var builder = new GoogleWorkspaceUriBuilder();

        // Act
        var uri = builder.BuildInboxUri(" person@example.com ");

        // Assert
        uri.Should().Be(new Uri("https://mail.google.com/mail/u/?authuser=person%40example.com#inbox"));
    }
}

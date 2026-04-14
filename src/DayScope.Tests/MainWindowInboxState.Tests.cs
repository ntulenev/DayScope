using FluentAssertions;

using DayScope.Application.Abstractions;
using DayScope.ViewModels;

namespace DayScope.Tests;

public sealed class MainWindowInboxStateTests
{
    [Fact(DisplayName = "The constructor builds the initial Google Calendar link for the current display date.")]
    [Trait("Category", "Unit")]
    public void CtorShouldBuildInitialGoogleCalendarUri()
    {
        // Arrange
        var displayDate = DateOnly.FromDateTime(DateTime.Today);
        var initialUri = new Uri("https://calendar.google.com/calendar/r/day/2026/4/14");
        var workspaceUriBuilder = new RecordingGoogleWorkspaceUriBuilder
        {
            BuildCalendarDayUriHandler = (requestedDate, emailAddress) =>
            {
                requestedDate.Should().Be(displayDate);
                emailAddress.Should().BeNull();
                return initialUri;
            }
        };

        // Act
        var state = new MainWindowInboxState(workspaceUriBuilder);

        // Assert
        state.GoogleCalendarUri.Should().Be(initialUri);
        workspaceUriBuilder.BuildCalendarDayUriCalls.Should().Be(1);
    }

    [Fact(DisplayName = "Applying an inbox snapshot updates unread counters, inbox links, and the account-aware calendar link.")]
    [Trait("Category", "Unit")]
    public void ApplySnapshotShouldUpdateUnreadCountersInboxLinkAndCalendarLink()
    {
        // Arrange
        var displayDate = DateOnly.FromDateTime(DateTime.Today);
        var initialUri = new Uri("https://calendar.google.com/calendar/r/day/2026/4/14");
        var accountAwareCalendarUri = new Uri(
            "https://calendar.google.com/calendar/r/day/2026/4/14?authuser=user%40example.com");
        var inboxUri = new Uri("https://mail.google.com/mail/u/?authuser=user%40example.com#inbox");
        var workspaceUriBuilder = new RecordingGoogleWorkspaceUriBuilder
        {
            BuildCalendarDayUriHandler = (requestedDate, emailAddress) =>
            {
                requestedDate.Should().Be(displayDate);
                return emailAddress is null
                    ? initialUri
                    : accountAwareCalendarUri;
            }
        };
        var state = new MainWindowInboxState(workspaceUriBuilder);

        // Act
        state.ApplySnapshot(new EmailInboxSnapshot(125, " user@example.com ", inboxUri));

        // Assert
        state.UnreadEmailCount.Should().Be(125);
        state.UnreadEmailCountText.Should().Be("99+");
        state.HasUnreadEmails.Should().BeTrue();
        state.UnreadEmailSummaryText.Should().Be("125 unread emails");
        state.UnreadEmailInboxUri.Should().Be(inboxUri);
        state.GoogleCalendarUri.Should().Be(accountAwareCalendarUri);
        workspaceUriBuilder.BuildCalendarDayUriCalls.Should().Be(2);
    }

    [Fact(DisplayName = "Changing the display date rebuilds the Google Calendar link for the new day.")]
    [Trait("Category", "Unit")]
    public void ApplyDisplayDateShouldRebuildGoogleCalendarUriForTheNewDate()
    {
        // Arrange
        var initialDisplayDate = DateOnly.FromDateTime(DateTime.Today);
        var initialUri = new Uri("https://calendar.google.com/calendar/r/day/2026/4/14");
        var updatedUri = new Uri("https://calendar.google.com/calendar/r/day/2026/5/1");
        var updatedDisplayDate = new DateOnly(2026, 5, 1);
        var workspaceUriBuilder = new RecordingGoogleWorkspaceUriBuilder
        {
            BuildCalendarDayUriHandler = (requestedDate, emailAddress) =>
            {
                emailAddress.Should().BeNull();
                return requestedDate == initialDisplayDate
                    ? initialUri
                    : updatedUri;
            }
        };
        var state = new MainWindowInboxState(workspaceUriBuilder);

        // Act
        state.ApplyDisplayDate(updatedDisplayDate);

        // Assert
        state.GoogleCalendarUri.Should().Be(updatedUri);
        workspaceUriBuilder.BuildCalendarDayUriCalls.Should().Be(2);
    }

    private sealed class RecordingGoogleWorkspaceUriBuilder : IGoogleWorkspaceUriBuilder
    {
        public int BuildCalendarDayUriCalls { get; private set; }

        public Func<DateOnly, string?, Uri> BuildCalendarDayUriHandler { get; init; } =
            static (_, _) => throw new InvalidOperationException("No calendar URI handler configured.");

        public Uri BuildCalendarDayUri(DateOnly displayDate, string? emailAddress)
        {
            BuildCalendarDayUriCalls++;
            return BuildCalendarDayUriHandler(displayDate, emailAddress);
        }

        public Uri BuildInboxUri(string? emailAddress) =>
            throw new NotSupportedException();
    }
}

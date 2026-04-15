using FluentAssertions;

using DayScope.Application.DaySchedule;
using DayScope.ViewModels;

namespace DayScope.Tests;

public sealed class MainWindowEventDetailsStateTests
{
    [Fact(DisplayName = "The default state is closed and exposes the fallback join label.")]
    [Trait("Category", "Unit")]
    public void CtorShouldInitializeClosedState()
    {
        // Arrange

        // Act
        var state = new MainWindowEventDetailsState();

        // Assert
        state.SelectedEventDetails.Should().BeNull();
        state.IsOpen.Should().BeFalse();
        state.HasOrganizer.Should().BeFalse();
        state.HasDescription.Should().BeFalse();
        state.HasParticipants.Should().BeFalse();
        state.HasJoinUrl.Should().BeFalse();
        state.JoinLabel.Should().Be("Open meeting link");
    }

    [Fact(DisplayName = "Opening a timed event exposes its details and Google Meet join label.")]
    [Trait("Category", "Unit")]
    public void OpenShouldExposeTimedEventDetails()
    {
        // Arrange
        var details = CreateDetails(
            organizer: "Alice",
            description: "Discuss roadmap",
            joinUrl: new Uri("https://meet.google.com/abc-defg-hij"),
            participants: [new EventParticipantDisplayState("Bob", "Accepted", true)]);
        var timedEvent = new TimedEventDisplayState(
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
        var state = new MainWindowEventDetailsState();

        // Act
        state.Open(timedEvent);

        // Assert
        state.SelectedEventDetails.Should().BeSameAs(details);
        state.IsOpen.Should().BeTrue();
        state.HasOrganizer.Should().BeTrue();
        state.HasDescription.Should().BeTrue();
        state.HasParticipants.Should().BeTrue();
        state.HasJoinUrl.Should().BeTrue();
        state.JoinLabel.Should().Be("Join Google Meet");
    }

    [Fact(DisplayName = "Opening an all-day event exposes its details and the default join label for non-Google links.")]
    [Trait("Category", "Unit")]
    public void OpenShouldExposeAllDayEventDetails()
    {
        // Arrange
        var details = CreateDetails(
            organizer: null,
            description: null,
            joinUrl: new Uri("https://zoom.us/j/123"),
            participants: []);
        var allDayEvent = new AllDayEventDisplayState(
            "Offsite",
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            details);
        var state = new MainWindowEventDetailsState();

        // Act
        state.Open(allDayEvent);

        // Assert
        state.SelectedEventDetails.Should().BeSameAs(details);
        state.IsOpen.Should().BeTrue();
        state.HasOrganizer.Should().BeFalse();
        state.HasDescription.Should().BeFalse();
        state.HasParticipants.Should().BeFalse();
        state.HasJoinUrl.Should().BeTrue();
        state.JoinLabel.Should().Be("Open meeting link");
    }

    [Fact(DisplayName = "Applying the Google account email makes Google Meet links account-aware.")]
    [Trait("Category", "Unit")]
    public void ApplyGoogleAccountEmailShouldMakeGoogleMeetLinksAccountAware()
    {
        // Arrange
        var state = new MainWindowEventDetailsState();
        var details = CreateDetails(
            organizer: "Alice",
            description: "Discuss roadmap",
            joinUrl: new Uri("https://meet.google.com/abc-defg-hij"),
            participants: []);
        state.ApplyGoogleAccountEmail(" user@example.com ");

        // Act
        state.Open(new TimedEventDisplayState(
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
            details));

        // Assert
        state.SelectedEventDetails.Should().NotBeSameAs(details);
        state.SelectedEventDetails!.JoinUrl.Should().Be(
            new Uri("https://meet.google.com/abc-defg-hij?authuser=user%40example.com"));
        state.JoinLabel.Should().Be("Join Google Meet");
    }

    [Fact(DisplayName = "Opening an unsupported object clears the selected event details.")]
    [Trait("Category", "Unit")]
    public void OpenShouldClearDetailsWhenEventTypeIsUnsupported()
    {
        // Arrange
        var state = new MainWindowEventDetailsState();
        state.Open(new AllDayEventDisplayState(
            "Offsite",
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            CreateDetails("Alice", "Details", null, [])));

        // Act
        state.Open(new object());

        // Assert
        state.SelectedEventDetails.Should().BeNull();
        state.IsOpen.Should().BeFalse();
    }

    [Fact(DisplayName = "Closing the state clears the selected event details.")]
    [Trait("Category", "Unit")]
    public void CloseShouldClearSelectedEventDetails()
    {
        // Arrange
        var state = new MainWindowEventDetailsState();
        state.Open(new AllDayEventDisplayState(
            "Offsite",
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            CreateDetails("Alice", "Details", null, [])));

        // Act
        state.Close();

        // Assert
        state.SelectedEventDetails.Should().BeNull();
        state.IsOpen.Should().BeFalse();
        state.HasJoinUrl.Should().BeFalse();
    }

    private static EventDetailsDisplayState CreateDetails(
        string? organizer,
        string? description,
        Uri? joinUrl,
        IReadOnlyList<EventParticipantDisplayState> participants)
    {
        return new EventDetailsDisplayState(
            "Standup",
            "9:00AM - 10:00AM",
            EventAppearance.Accepted,
            "Accepted",
            string.Empty,
            organizer,
            description,
            joinUrl,
            participants);
    }
}

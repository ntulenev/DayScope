using FluentAssertions;

using Google.Apis.Calendar.v3.Data;

using DayScope.Domain.Calendar;
using DayScope.Infrastructure.Calendar;

namespace DayScope.Infrastructure.Tests;

public sealed class GoogleCalendarEventMapperTests
{
    [Fact(DisplayName = "Mapping returns null when the source event has no usable start.")]
    [Trait("Category", "Unit")]
    public void MapEventShouldReturnNullWhenSourceEventHasNoUsableStart()
    {
        // Arrange
        var mapper = new GoogleCalendarEventMapper();
        var timeZone = TimeZoneInfo.Utc;

        // Act
        var missingStartResult = mapper.MapEvent(new Event { Summary = "Missing start" }, timeZone);
        var invalidStartResult = mapper.MapEvent(
            new Event
            {
                Summary = "Invalid start",
                Start = new EventDateTime { Date = "not-a-date" }
            },
            timeZone);

        // Assert
        missingStartResult.Should().BeNull();
        invalidStartResult.Should().BeNull();
    }

    [Fact(DisplayName = "Mapping returns null when the source event is cancelled.")]
    [Trait("Category", "Unit")]
    public void MapEventShouldReturnNullWhenSourceEventIsCancelled()
    {
        // Arrange
        var mapper = new GoogleCalendarEventMapper();

        // Act
        var result = mapper.MapEvent(
            new Event
            {
                Status = "cancelled",
                Start = new EventDateTime { DateTimeRaw = "2026-04-14T09:00:00+00:00" }
            },
            TimeZoneInfo.Utc);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Mapping converts timed events with attendees, join links, and event kinds.")]
    [Trait("Category", "Unit")]
    public void MapEventShouldConvertTimedEventsWithAttendeesJoinLinksAndEventKinds()
    {
        // Arrange
        var mapper = new GoogleCalendarEventMapper();
        var timeZone = TimeZoneInfo.Utc;
        var sourceEvent = new Event
        {
            Summary = "  Design review  ",
            Status = "confirmed",
            EventType = "focusTime",
            Description = " Agenda ",
            HangoutLink = "https://meet.google.com/abc-defg-hij",
            Start = new EventDateTime { DateTimeRaw = "2026-04-14T09:15:00+00:00" },
            End = new EventDateTime { DateTimeRaw = "2026-04-14T10:00:00+00:00" },
            Organizer = new Event.OrganizerData
            {
                DisplayName = "Alice",
                Email = "alice@example.com"
            },
            Attendees =
            [
                new EventAttendee
                {
                    DisplayName = "Bob",
                    Email = "bob@example.com",
                    ResponseStatus = "tentative",
                    Self = false
                },
                new EventAttendee
                {
                    DisplayName = "Me",
                    Email = "me@example.com",
                    ResponseStatus = "accepted",
                    Self = true
                },
                null!
            ]
        };

        // Act
        var result = mapper.MapEvent(sourceEvent, timeZone);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Design review");
        result.Start.Should().Be(new DateTimeOffset(2026, 4, 14, 9, 15, 0, TimeSpan.Zero));
        result.End.Should().Be(new DateTimeOffset(2026, 4, 14, 10, 0, 0, TimeSpan.Zero));
        result.IsAllDay.Should().BeFalse();
        result.ParticipationStatus.Should().Be(CalendarParticipationStatus.Accepted);
        result.EventKind.Should().Be(CalendarEventKind.FocusTime);
        result.OrganizerName.Should().Be("Alice");
        result.OrganizerEmail.Should().Be("alice@example.com");
        result.Description.Should().Be("Agenda");
        result.JoinUrl.Should().Be(new Uri("https://meet.google.com/abc-defg-hij"));
        result.Participants.Should().HaveCount(2);
        result.Participants[0].DisplayLabel.Should().Be("Bob");
        result.Participants[0].ParticipationStatus.Should().Be(CalendarParticipationStatus.Tentative);
        result.Participants[0].IsSelf.Should().BeFalse();
        result.Participants[1].DisplayLabel.Should().Be("Me");
        result.Participants[1].ParticipationStatus.Should().Be(CalendarParticipationStatus.Accepted);
        result.Participants[1].IsSelf.Should().BeTrue();
    }

    [Fact(DisplayName = "Mapping converts all-day events and falls back to organizer-self accepted status.")]
    [Trait("Category", "Unit")]
    public void MapEventShouldConvertAllDayEventsAndFallbackToOrganizerSelfAcceptedStatus()
    {
        // Arrange
        var mapper = new GoogleCalendarEventMapper();
        var berlin = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var sourceEvent = new Event
        {
            Summary = " ",
            EventType = "outOfOffice",
            HangoutLink = "not-a-valid-uri",
            Start = new EventDateTime { Date = "2026-04-14" },
            End = new EventDateTime { Date = "2026-04-15" },
            Organizer = new Event.OrganizerData
            {
                Self = true,
                DisplayName = "  "
            }
        };

        // Act
        var result = mapper.MapEvent(sourceEvent, berlin);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Untitled event");
        result.IsAllDay.Should().BeTrue();
        result.ParticipationStatus.Should().Be(CalendarParticipationStatus.Accepted);
        result.EventKind.Should().Be(CalendarEventKind.OutOfOffice);
        result.Start.Should().Be(new DateTimeOffset(2026, 4, 14, 0, 0, 0, berlin.GetUtcOffset(new DateTime(2026, 4, 14))));
        result.End.Should().Be(new DateTimeOffset(2026, 4, 15, 0, 0, 0, berlin.GetUtcOffset(new DateTime(2026, 4, 15))));
        result.JoinUrl.Should().BeNull();
        result.Participants.Should().BeEmpty();
    }
}

using FluentAssertions;

using DayScope.Application.DaySchedule;
using DayScope.Domain.Calendar;

namespace DayScope.Application.Tests;

public sealed class DayScheduleEventPresentationFactoryTests
{
    [Fact(DisplayName = "Timed event candidates are omitted when the event does not intersect the visible timeline.")]
    [Trait("Category", "Unit")]
    public void CreateTimedEventCandidateShouldReturnNullWhenEventDoesNotIntersectTheVisibleTimeline()
    {
        // Arrange
        var calendarEvent = CreateCalendarEvent(
            start: new DateTimeOffset(2026, 4, 14, 5, 0, 0, TimeSpan.Zero),
            end: new DateTimeOffset(2026, 4, 14, 5, 30, 0, TimeSpan.Zero));

        // Act
        var result = DayScheduleEventPresentationFactory.CreateTimedEventCandidate(
            calendarEvent,
            new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero),
            80,
            TimeZoneInfo.Utc);

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Timed event candidates clip to the visible timeline and include mapped details.")]
    [Trait("Category", "Unit")]
    public void CreateTimedEventCandidateShouldClipToTheVisibleTimelineAndIncludeMappedDetails()
    {
        // Arrange
        var participant = new CalendarEventParticipant(
            "Sam",
            "sam@example.com",
            CalendarParticipationStatus.Tentative,
            isSelf: true);
        var calendarEvent = CreateCalendarEvent(
            start: new DateTimeOffset(2026, 4, 14, 5, 30, 0, TimeSpan.Zero),
            end: new DateTimeOffset(2026, 4, 14, 20, 30, 0, TimeSpan.Zero),
            participationStatus: CalendarParticipationStatus.Declined,
            eventKind: CalendarEventKind.WorkingLocation,
            organizerName: " Alex ",
            description: " Agenda ",
            joinUrl: new Uri("https://example.com/join"),
            participants: [participant]);

        // Act
        var result = DayScheduleEventPresentationFactory.CreateTimedEventCandidate(
            calendarEvent,
            new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero),
            80,
            TimeZoneInfo.Utc);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Planning");
        result.Start.Should().Be(new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero));
        result.End.Should().Be(new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero));
        result.ScheduleText.Should().Be("5:30AM-8:30PM");
        result.Appearance.Should().Be(EventAppearance.Declined);
        result.StatusLabel.Should().Be("Declined");
        result.LeadingIcon.Should().NotBeEmpty();
        result.Details.ScheduleText.Should().Be("5:30AM-8:30PM");
        result.Details.Organizer.Should().Be("Alex");
        result.Details.Description.Should().Be("Agenda");
        result.Details.JoinUrl.Should().Be(new Uri("https://example.com/join"));
        result.Details.Participants.Should().ContainSingle();
        result.Details.Participants[0].DisplayName.Should().Be("Sam");
        result.Details.Participants[0].StatusLabel.Should().Be("Maybe");
        result.Details.Participants[0].IsSelf.Should().BeTrue();
    }

    [Fact(DisplayName = "All-day event presentation uses all-day details and mapped appearance.")]
    [Trait("Category", "Unit")]
    public void CreateAllDayEventShouldUseAllDayDetailsAndMappedAppearance()
    {
        // Arrange
        var calendarEvent = CreateCalendarEvent(
            start: new DateTimeOffset(2026, 4, 14, 0, 0, 0, TimeSpan.Zero),
            end: new DateTimeOffset(2026, 4, 15, 0, 0, 0, TimeSpan.Zero),
            isAllDay: true,
            participationStatus: CalendarParticipationStatus.Cancelled,
            eventKind: CalendarEventKind.OutOfOffice,
            organizerName: "Jordan");

        // Act
        var result = DayScheduleEventPresentationFactory.CreateAllDayEvent(calendarEvent, TimeZoneInfo.Utc);

        // Assert
        result.Title.Should().Be("Planning");
        result.Appearance.Should().Be(EventAppearance.Cancelled);
        result.StatusLabel.Should().Be("Cancelled");
        result.LeadingIcon.Should().NotBeEmpty();
        result.Details.ScheduleText.Should().Be("All day");
        result.Details.Organizer.Should().Be("Jordan");
    }

    [Theory(DisplayName = "Participation statuses map to expected appearances and labels.")]
    [Trait("Category", "Unit")]
    [InlineData(CalendarParticipationStatus.Accepted, EventAppearance.Accepted, "Confirmed")]
    [InlineData(CalendarParticipationStatus.AwaitingResponse, EventAppearance.AwaitingResponse, "Awaiting")]
    [InlineData(CalendarParticipationStatus.Tentative, EventAppearance.Tentative, "Maybe")]
    [InlineData(CalendarParticipationStatus.Declined, EventAppearance.Declined, "Declined")]
    [InlineData(CalendarParticipationStatus.Cancelled, EventAppearance.Cancelled, "Cancelled")]
    [InlineData((CalendarParticipationStatus)999, EventAppearance.Accepted, "Confirmed")]
    public void MappingHelpersShouldReturnExpectedAppearanceAndStatusLabel(
        CalendarParticipationStatus participationStatus,
        EventAppearance expectedAppearance,
        string expectedLabel)
    {
        // Act
        var appearance = DayScheduleEventPresentationFactory.MapAppearance(participationStatus);
        var label = DayScheduleEventPresentationFactory.GetStatusLabel(participationStatus);

        // Assert
        appearance.Should().Be(expectedAppearance);
        label.Should().Be(expectedLabel);
    }

    [Theory(DisplayName = "Event kinds map to expected icon presence.")]
    [Trait("Category", "Unit")]
    [InlineData(CalendarEventKind.Default, true)]
    [InlineData(CalendarEventKind.FocusTime, false)]
    [InlineData(CalendarEventKind.OutOfOffice, false)]
    [InlineData(CalendarEventKind.WorkingLocation, false)]
    [InlineData(CalendarEventKind.Task, false)]
    [InlineData(CalendarEventKind.AppointmentSchedule, false)]
    [InlineData((CalendarEventKind)999, true)]
    public void GetLeadingIconShouldReturnExpectedIconPresence(
        CalendarEventKind eventKind,
        bool expectEmpty)
    {
        // Act
        var result = DayScheduleEventPresentationFactory.GetLeadingIcon(eventKind);

        // Assert
        if (expectEmpty)
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().NotBeEmpty();
        }
    }

    private static CalendarEvent CreateCalendarEvent(
        DateTimeOffset start,
        DateTimeOffset end,
        bool isAllDay = false,
        CalendarParticipationStatus participationStatus = CalendarParticipationStatus.Accepted,
        CalendarEventKind eventKind = CalendarEventKind.Default,
        string? organizerName = null,
        string? description = null,
        Uri? joinUrl = null,
        IReadOnlyList<CalendarEventParticipant>? participants = null)
    {
        return new CalendarEvent(
            "Planning",
            start,
            end,
            isAllDay,
            participationStatus,
            eventKind,
            organizerName,
            "organizer@example.com",
            description,
            joinUrl,
            participants ?? []);
    }
}

using DayScope.Domain.Calendar;

namespace DayScope.Infrastructure.Demo;

/// <summary>
/// Describes one deterministic demo calendar event.
/// </summary>
/// <param name="Title">The event title.</param>
/// <param name="ParticipationStatus">The signed-in user's participation state.</param>
/// <param name="EventKind">The event kind.</param>
/// <param name="Description">The event description.</param>
/// <param name="OrganizerName">The organizer display name.</param>
/// <param name="OrganizerEmail">The organizer email address.</param>
/// <param name="IsAllDay">Whether the event is all-day.</param>
/// <param name="StartHour">The local start hour.</param>
/// <param name="StartMinute">The local start minute.</param>
/// <param name="EndHour">The local end hour.</param>
/// <param name="EndMinute">The local end minute.</param>
/// <param name="JoinPath">The optional relative meeting path.</param>
/// <param name="Participants">The synthetic participant list.</param>
internal sealed record DemoCalendarEventDefinition(
    string Title,
    CalendarParticipationStatus ParticipationStatus,
    CalendarEventKind EventKind,
    string Description,
    string OrganizerName,
    string OrganizerEmail,
    bool IsAllDay,
    int StartHour,
    int StartMinute,
    int EndHour,
    int EndMinute,
    string? JoinPath,
    IReadOnlyList<CalendarEventParticipant> Participants);

using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;
using DayScope.Domain.Calendar;

namespace DayScope.Infrastructure.Demo;

/// <summary>
/// Provides deterministic synthetic calendar data for demo mode.
/// </summary>
public sealed class DemoCalendarService : ICalendarService
{
    public bool IsEnabled => true;

    /// <summary>
    /// Returns a synthetic agenda for the requested day.
    /// </summary>
    /// <param name="day">The day to generate.</param>
    /// <param name="timeZone">The time zone used to build the synthetic event times.</param>
    /// <param name="interactionMode">The interaction mode for the request.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A successful calendar load result containing demo events.</returns>
    public Task<CalendarLoadResult> GetEventsForDateAsync(
        DateOnly day,
        TimeZoneInfo timeZone,
        CalendarInteractionMode interactionMode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        _ = interactionMode;
        _ = cancellationToken;

        return Task.FromResult(
            CalendarLoadResult.Success(
                new CalendarAgenda(BuildEvents(day, timeZone))));
    }

    /// <summary>
    /// Builds the deterministic demo agenda for the requested day.
    /// </summary>
    /// <param name="day">The day to generate.</param>
    /// <param name="timeZone">The time zone used to create event instants.</param>
    /// <returns>The synthetic events for the day.</returns>
    private static IReadOnlyList<CalendarEvent> BuildEvents(
        DateOnly day,
        TimeZoneInfo timeZone)
    {
        return
        [
            CreateAllDayEvent(
                day,
                timeZone,
                "Office day: engineering leadership",
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.WorkingLocation,
                "Synthetic demo event for the UI preview. The day is anchored around office collaboration, planning, and unblock sessions.",
                "Engineering Lead",
                "lead@example.com",
                Participants(
                    SelfAccepted(),
                    Participant("Engineering Director", "director@example.com", CalendarParticipationStatus.Accepted))),

            CreateAllDayEvent(
                day,
                timeZone,
                "Review hiring scorecards",
                CalendarParticipationStatus.AwaitingResponse,
                CalendarEventKind.Task,
                "Synthetic all-day task block used to demonstrate the all-day strip and awaiting response styling.",
                "Recruiting Coordinator",
                "recruiting@example.com",
                Participants(
                    SelfAwaiting(),
                    Participant("Recruiting Coordinator", "recruiting@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Inbox triage and priorities",
                startHour: 8,
                startMinute: 15,
                endHour: 8,
                endMinute: 45,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.Task,
                "Synthetic morning admin block. Review unread items, assign owners, and confirm priorities for the day.",
                "Engineering Lead",
                "lead@example.com",
                joinPath: "inbox-triage",
                participants: Participants(SelfAccepted())),

            CreateTimedEvent(
                day,
                timeZone,
                "Platform architecture sync",
                startHour: 9,
                startMinute: 0,
                endHour: 10,
                endMinute: 0,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.Default,
                "Synthetic design review with platform stakeholders. Cover migration risks, decision log updates, and next actions.",
                "Staff Engineer",
                "staff.engineer@example.com",
                joinPath: "platform-architecture-sync",
                participants: Participants(
                    SelfAccepted(),
                    Participant("Staff Engineer", "staff.engineer@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Product Manager", "pm@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Vendor security follow-up",
                startHour: 9,
                startMinute: 0,
                endHour: 9,
                endMinute: 30,
                CalendarParticipationStatus.Declined,
                CalendarEventKind.Default,
                "Synthetic declined meeting used to demonstrate overlapping declined cards in the timeline.",
                "Security Program Manager",
                "security@example.com",
                participants: Participants(
                    SelfDeclined(),
                    Participant("Security Program Manager", "security@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Quick unblock: mobile release",
                startHour: 9,
                startMinute: 0,
                endHour: 9,
                endMinute: 20,
                CalendarParticipationStatus.AwaitingResponse,
                CalendarEventKind.Default,
                "Synthetic short overlap block. Used to show compact layout and awaiting response styling.",
                "Mobile Lead",
                "mobile.lead@example.com",
                participants: Participants(
                    SelfAwaiting(),
                    Participant("Mobile Lead", "mobile.lead@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "1:1 with Backend Lead",
                startHour: 10,
                startMinute: 15,
                endHour: 11,
                endMinute: 0,
                CalendarParticipationStatus.Tentative,
                CalendarEventKind.Default,
                "Synthetic 1:1 block used to demonstrate tentative styling and participant details.",
                "Backend Lead",
                "backend.lead@example.com",
                joinPath: "one-on-one-backend-lead",
                participants: Participants(
                    SelfTentative(),
                    Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Sprint scope review",
                startHour: 11,
                startMinute: 0,
                endHour: 12,
                endMinute: 30,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.Default,
                "Synthetic planning review with product and QA. Validate scope, sequencing, and capacity tradeoffs for the next sprint.",
                "Product Manager",
                "pm@example.com",
                joinPath: "sprint-scope-review",
                participants: Participants(
                    SelfAccepted(),
                    Participant("Product Manager", "pm@example.com", CalendarParticipationStatus.Accepted),
                    Participant("QA Lead", "qa.lead@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Frontend Lead", "frontend.lead@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Design sign-off: onboarding flow",
                startHour: 12,
                startMinute: 0,
                endHour: 12,
                endMinute: 30,
                CalendarParticipationStatus.Declined,
                CalendarEventKind.Default,
                "Synthetic overlap used to show a declined event sharing time with a longer confirmed review.",
                "Design Lead",
                "design.lead@example.com",
                participants: Participants(
                    SelfDeclined(),
                    Participant("Design Lead", "design.lead@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Weekly stakeholder update",
                startHour: 13,
                startMinute: 0,
                endHour: 13,
                endMinute: 45,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.Default,
                "Synthetic stakeholder check-in focused on delivery status, risks, and decisions blocked on leadership input.",
                "Engineering Director",
                "director@example.com",
                joinPath: "stakeholder-update",
                participants: Participants(
                    SelfAccepted(),
                    Participant("Engineering Director", "director@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Product Director", "product.director@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Roadmap review",
                startHour: 14,
                startMinute: 0,
                endHour: 15,
                endMinute: 15,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.Default,
                "Synthetic roadmap session covering sequencing, staffing constraints, and dependencies across teams.",
                "Engineering Director",
                "director@example.com",
                joinPath: "roadmap-review",
                participants: Participants(
                    SelfAccepted(),
                    Participant("Engineering Director", "director@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Product Manager", "pm@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Frontend Lead", "frontend.lead@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Focus time: architecture notes",
                startHour: 15,
                startMinute: 0,
                endHour: 15,
                endMinute: 45,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.FocusTime,
                "Synthetic focus block used to demonstrate event-kind icons and overlapping duration differences.",
                "Engineering Lead",
                "lead@example.com",
                participants: Participants(SelfAccepted())),

            CreateTimedEvent(
                day,
                timeZone,
                "Approval queue cleanup",
                startHour: 16,
                startMinute: 0,
                endHour: 16,
                endMinute: 15,
                CalendarParticipationStatus.AwaitingResponse,
                CalendarEventKind.Task,
                "Synthetic short task block to demonstrate very small card heights in the schedule.",
                "Engineering Lead",
                "lead@example.com",
                participants: Participants(SelfAwaiting())),

            CreateTimedEvent(
                day,
                timeZone,
                "Cross-team dependency check",
                startHour: 16,
                startMinute: 30,
                endHour: 17,
                endMinute: 30,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.Default,
                "Synthetic dependency review between platform, product, and frontend workstreams.",
                "Program Manager",
                "program.manager@example.com",
                joinPath: "dependency-check",
                participants: Participants(
                    SelfAccepted(),
                    Participant("Program Manager", "program.manager@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Platform Lead", "platform.lead@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Frontend Lead", "frontend.lead@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Candidate interview debrief",
                startHour: 17,
                startMinute: 45,
                endHour: 18,
                endMinute: 30,
                CalendarParticipationStatus.Cancelled,
                CalendarEventKind.Default,
                "Synthetic cancelled meeting included specifically to demonstrate cancelled event styling in the timeline.",
                "Recruiting Coordinator",
                "recruiting@example.com",
                participants: Participants(
                    SelfAccepted(),
                    Participant("Recruiting Coordinator", "recruiting@example.com", CalendarParticipationStatus.Accepted),
                    Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted))),

            CreateTimedEvent(
                day,
                timeZone,
                "Out of office",
                startHour: 18,
                startMinute: 30,
                endHour: 19,
                endMinute: 0,
                CalendarParticipationStatus.Accepted,
                CalendarEventKind.OutOfOffice,
                "Synthetic end-of-day block used to demonstrate out-of-office styling.",
                "Engineering Lead",
                "lead@example.com",
                participants: Participants(SelfAccepted()))
        ];
    }

    /// <summary>
    /// Creates a synthetic all-day calendar event.
    /// </summary>
    /// <param name="day">The day represented by the event.</param>
    /// <param name="timeZone">The time zone used to calculate event boundaries.</param>
    /// <param name="title">The event title.</param>
    /// <param name="participationStatus">The signed-in user's participation status.</param>
    /// <param name="eventKind">The synthetic event kind.</param>
    /// <param name="description">The event description.</param>
    /// <param name="organizerName">The organizer display name.</param>
    /// <param name="organizerEmail">The organizer email address.</param>
    /// <param name="participants">The event participants.</param>
    /// <returns>The synthetic all-day event.</returns>
    private static CalendarEvent CreateAllDayEvent(
        DateOnly day,
        TimeZoneInfo timeZone,
        string title,
        CalendarParticipationStatus participationStatus,
        CalendarEventKind eventKind,
        string description,
        string organizerName,
        string organizerEmail,
        IReadOnlyList<CalendarEventParticipant> participants)
    {
        var start = At(day, timeZone, 0, 0);
        var end = At(day.AddDays(1), timeZone, 0, 0);

        return new CalendarEvent(
            title,
            start,
            end,
            true,
            participationStatus,
            eventKind,
            organizerName,
            organizerEmail,
            description,
            null,
            participants);
    }

    /// <summary>
    /// Creates a synthetic timed calendar event.
    /// </summary>
    /// <param name="day">The day represented by the event.</param>
    /// <param name="timeZone">The time zone used to calculate event instants.</param>
    /// <param name="title">The event title.</param>
    /// <param name="startHour">The local start hour.</param>
    /// <param name="startMinute">The local start minute.</param>
    /// <param name="endHour">The local end hour.</param>
    /// <param name="endMinute">The local end minute.</param>
    /// <param name="participationStatus">The signed-in user's participation status.</param>
    /// <param name="eventKind">The synthetic event kind.</param>
    /// <param name="description">The event description.</param>
    /// <param name="organizerName">The organizer display name.</param>
    /// <param name="organizerEmail">The organizer email address.</param>
    /// <param name="participants">The event participants.</param>
    /// <param name="joinPath">The optional relative meeting path used to build a demo join URL.</param>
    /// <returns>The synthetic timed event.</returns>
    private static CalendarEvent CreateTimedEvent(
        DateOnly day,
        TimeZoneInfo timeZone,
        string title,
        int startHour,
        int startMinute,
        int endHour,
        int endMinute,
        CalendarParticipationStatus participationStatus,
        CalendarEventKind eventKind,
        string description,
        string organizerName,
        string organizerEmail,
        IReadOnlyList<CalendarEventParticipant> participants,
        string? joinPath = null)
    {
        return new CalendarEvent(
            title,
            At(day, timeZone, startHour, startMinute),
            At(day, timeZone, endHour, endMinute),
            false,
            participationStatus,
            eventKind,
            organizerName,
            organizerEmail,
            description,
            BuildJoinUri(joinPath),
            participants);
    }

    /// <summary>
    /// Creates a local instant for the requested day and time in the given time zone.
    /// </summary>
    /// <param name="day">The local day.</param>
    /// <param name="timeZone">The time zone that defines the offset.</param>
    /// <param name="hour">The local hour.</param>
    /// <param name="minute">The local minute.</param>
    /// <returns>The resulting instant.</returns>
    private static DateTimeOffset At(
        DateOnly day,
        TimeZoneInfo timeZone,
        int hour,
        int minute)
    {
        var localDateTime = day.ToDateTime(new TimeOnly(hour, minute));
        return new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime));
    }

    /// <summary>
    /// Builds a deterministic demo meeting URI from a relative path.
    /// </summary>
    /// <param name="joinPath">The relative meeting path.</param>
    /// <returns>The demo join URI, or <see langword="null"/> when no path is supplied.</returns>
    private static Uri? BuildJoinUri(string? joinPath)
    {
        if (string.IsNullOrWhiteSpace(joinPath))
        {
            return null;
        }

        return new Uri($"https://example.com/meet/{joinPath.Trim()}", UriKind.Absolute);
    }

    /// <summary>
    /// Packs the supplied participants into an immutable-friendly list for synthetic events.
    /// </summary>
    /// <param name="participants">The participants to include.</param>
    /// <returns>The participant list.</returns>
    private static IReadOnlyList<CalendarEventParticipant> Participants(
        params CalendarEventParticipant[] participants) => participants;

    /// <summary>
    /// Creates the signed-in user participant in the accepted state.
    /// </summary>
    /// <returns>The synthetic participant.</returns>
    private static CalendarEventParticipant SelfAccepted() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.Accepted, isSelf: true);

    /// <summary>
    /// Creates the signed-in user participant in the awaiting-response state.
    /// </summary>
    /// <returns>The synthetic participant.</returns>
    private static CalendarEventParticipant SelfAwaiting() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.AwaitingResponse, isSelf: true);

    /// <summary>
    /// Creates the signed-in user participant in the tentative state.
    /// </summary>
    /// <returns>The synthetic participant.</returns>
    private static CalendarEventParticipant SelfTentative() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.Tentative, isSelf: true);

    /// <summary>
    /// Creates the signed-in user participant in the declined state.
    /// </summary>
    /// <returns>The synthetic participant.</returns>
    private static CalendarEventParticipant SelfDeclined() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.Declined, isSelf: true);

    /// <summary>
    /// Creates a synthetic participant model for demo events.
    /// </summary>
    /// <param name="displayName">The participant display name.</param>
    /// <param name="email">The participant email address.</param>
    /// <param name="participationStatus">The participant response status.</param>
    /// <param name="isSelf">Whether the participant represents the signed-in user.</param>
    /// <returns>The synthetic participant.</returns>
    private static CalendarEventParticipant Participant(
        string displayName,
        string email,
        CalendarParticipationStatus participationStatus,
        bool isSelf = false)
    {
        return new CalendarEventParticipant(
            displayName,
            email,
            participationStatus,
            isSelf);
    }
}

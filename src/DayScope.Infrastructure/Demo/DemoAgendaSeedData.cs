using DayScope.Domain.Calendar;

namespace DayScope.Infrastructure.Demo;

/// <summary>
/// Stores the deterministic demo agenda definitions used by demo mode.
/// </summary>
internal static class DemoAgendaSeedData
{
    public static IReadOnlyList<DemoCalendarEventDefinition> EventDefinitions { get; } =
    [
        AllDay(
            "Office day: engineering leadership",
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.WorkingLocation,
            "Synthetic demo event for the UI preview. The day is anchored around office collaboration, planning, and unblock sessions.",
            "Engineering Lead",
            "lead@example.com",
            Participants(
                SelfAccepted(),
                Participant("Engineering Director", "director@example.com", CalendarParticipationStatus.Accepted))),

        AllDay(
            "Review hiring scorecards",
            CalendarParticipationStatus.AwaitingResponse,
            CalendarEventKind.Task,
            "Synthetic all-day task block used to demonstrate the all-day strip and awaiting response styling.",
            "Recruiting Coordinator",
            "recruiting@example.com",
            Participants(
                SelfAwaiting(),
                Participant("Recruiting Coordinator", "recruiting@example.com", CalendarParticipationStatus.Accepted))),

        Timed(
            "Inbox triage and priorities",
            8,
            15,
            8,
            45,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Task,
            "Synthetic morning admin block. Review unread items, assign owners, and confirm priorities for the day.",
            "Engineering Lead",
            "lead@example.com",
            Participants(SelfAccepted()),
            joinPath: "inbox-triage"),

        Timed(
            "Platform architecture sync",
            9,
            0,
            10,
            0,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            "Synthetic design review with platform stakeholders. Cover migration risks, decision log updates, and next actions.",
            "Staff Engineer",
            "staff.engineer@example.com",
            Participants(
                SelfAccepted(),
                Participant("Staff Engineer", "staff.engineer@example.com", CalendarParticipationStatus.Accepted),
                Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted),
                Participant("Product Manager", "pm@example.com", CalendarParticipationStatus.Accepted)),
            joinPath: "platform-architecture-sync"),

        Timed(
            "Vendor security follow-up",
            9,
            0,
            9,
            30,
            CalendarParticipationStatus.Declined,
            CalendarEventKind.Default,
            "Synthetic declined meeting used to demonstrate overlapping declined cards in the timeline.",
            "Security Program Manager",
            "security@example.com",
            Participants(
                SelfDeclined(),
                Participant("Security Program Manager", "security@example.com", CalendarParticipationStatus.Accepted))),

        Timed(
            "Quick unblock: mobile release",
            9,
            0,
            9,
            20,
            CalendarParticipationStatus.AwaitingResponse,
            CalendarEventKind.Default,
            "Synthetic short overlap block. Used to show compact layout and awaiting response styling.",
            "Mobile Lead",
            "mobile.lead@example.com",
            Participants(
                SelfAwaiting(),
                Participant("Mobile Lead", "mobile.lead@example.com", CalendarParticipationStatus.Accepted))),

        Timed(
            "1:1 with Backend Lead",
            10,
            15,
            11,
            0,
            CalendarParticipationStatus.Tentative,
            CalendarEventKind.Default,
            "Synthetic 1:1 block used to demonstrate tentative styling and participant details.",
            "Backend Lead",
            "backend.lead@example.com",
            Participants(
                SelfTentative(),
                Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted)),
            joinPath: "one-on-one-backend-lead"),

        Timed(
            "Sprint scope review",
            11,
            0,
            12,
            30,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            "Synthetic planning review with product and QA. Validate scope, sequencing, and capacity tradeoffs for the next sprint.",
            "Product Manager",
            "pm@example.com",
            Participants(
                SelfAccepted(),
                Participant("Product Manager", "pm@example.com", CalendarParticipationStatus.Accepted),
                Participant("QA Lead", "qa.lead@example.com", CalendarParticipationStatus.Accepted),
                Participant("Frontend Lead", "frontend.lead@example.com", CalendarParticipationStatus.Accepted)),
            joinPath: "sprint-scope-review"),

        Timed(
            "Design sign-off: onboarding flow",
            12,
            0,
            12,
            30,
            CalendarParticipationStatus.Declined,
            CalendarEventKind.Default,
            "Synthetic overlap used to show a declined event sharing time with a longer confirmed review.",
            "Design Lead",
            "design.lead@example.com",
            Participants(
                SelfDeclined(),
                Participant("Design Lead", "design.lead@example.com", CalendarParticipationStatus.Accepted))),

        Timed(
            "Weekly stakeholder update",
            13,
            0,
            13,
            45,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            "Synthetic stakeholder check-in focused on delivery status, risks, and decisions blocked on leadership input.",
            "Engineering Director",
            "director@example.com",
            Participants(
                SelfAccepted(),
                Participant("Engineering Director", "director@example.com", CalendarParticipationStatus.Accepted),
                Participant("Product Director", "product.director@example.com", CalendarParticipationStatus.Accepted)),
            joinPath: "stakeholder-update"),

        Timed(
            "Roadmap review",
            14,
            0,
            15,
            15,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            "Synthetic roadmap session covering sequencing, staffing constraints, and dependencies across teams.",
            "Engineering Director",
            "director@example.com",
            Participants(
                SelfAccepted(),
                Participant("Engineering Director", "director@example.com", CalendarParticipationStatus.Accepted),
                Participant("Product Manager", "pm@example.com", CalendarParticipationStatus.Accepted),
                Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted),
                Participant("Frontend Lead", "frontend.lead@example.com", CalendarParticipationStatus.Accepted)),
            joinPath: "roadmap-review"),

        Timed(
            "Focus time: architecture notes",
            15,
            0,
            15,
            45,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.FocusTime,
            "Synthetic focus block used to demonstrate event-kind icons and overlapping duration differences.",
            "Engineering Lead",
            "lead@example.com",
            Participants(SelfAccepted())),

        Timed(
            "Approval queue cleanup",
            16,
            0,
            16,
            15,
            CalendarParticipationStatus.AwaitingResponse,
            CalendarEventKind.Task,
            "Synthetic short task block to demonstrate very small card heights in the schedule.",
            "Engineering Lead",
            "lead@example.com",
            Participants(SelfAwaiting())),

        Timed(
            "Cross-team dependency check",
            16,
            30,
            17,
            30,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            "Synthetic dependency review between platform, product, and frontend workstreams.",
            "Program Manager",
            "program.manager@example.com",
            Participants(
                SelfAccepted(),
                Participant("Program Manager", "program.manager@example.com", CalendarParticipationStatus.Accepted),
                Participant("Platform Lead", "platform.lead@example.com", CalendarParticipationStatus.Accepted),
                Participant("Frontend Lead", "frontend.lead@example.com", CalendarParticipationStatus.Accepted)),
            joinPath: "dependency-check"),

        Timed(
            "Candidate interview debrief",
            17,
            45,
            18,
            30,
            CalendarParticipationStatus.Cancelled,
            CalendarEventKind.Default,
            "Synthetic cancelled meeting included specifically to demonstrate cancelled event styling in the timeline.",
            "Recruiting Coordinator",
            "recruiting@example.com",
            Participants(
                SelfAccepted(),
                Participant("Recruiting Coordinator", "recruiting@example.com", CalendarParticipationStatus.Accepted),
                Participant("Backend Lead", "backend.lead@example.com", CalendarParticipationStatus.Accepted))),

        Timed(
            "Out of office",
            18,
            30,
            19,
            0,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.OutOfOffice,
            "Synthetic end-of-day block used to demonstrate out-of-office styling.",
            "Engineering Lead",
            "lead@example.com",
            Participants(SelfAccepted()))
    ];

    private static DemoCalendarEventDefinition AllDay(
        string title,
        CalendarParticipationStatus participationStatus,
        CalendarEventKind eventKind,
        string description,
        string organizerName,
        string organizerEmail,
        IReadOnlyList<CalendarEventParticipant> participants)
    {
        return new DemoCalendarEventDefinition(
            title,
            participationStatus,
            eventKind,
            description,
            organizerName,
            organizerEmail,
            IsAllDay: true,
            StartHour: 0,
            StartMinute: 0,
            EndHour: 24,
            EndMinute: 0,
            JoinPath: null,
            participants);
    }

    private static DemoCalendarEventDefinition Timed(
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
        return new DemoCalendarEventDefinition(
            title,
            participationStatus,
            eventKind,
            description,
            organizerName,
            organizerEmail,
            IsAllDay: false,
            StartHour: startHour,
            StartMinute: startMinute,
            EndHour: endHour,
            EndMinute: endMinute,
            JoinPath: joinPath,
            participants);
    }

    private static IReadOnlyList<CalendarEventParticipant> Participants(
        params CalendarEventParticipant[] participants) => participants;

    private static CalendarEventParticipant SelfAccepted() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.Accepted, isSelf: true);

    private static CalendarEventParticipant SelfAwaiting() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.AwaitingResponse, isSelf: true);

    private static CalendarEventParticipant SelfTentative() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.Tentative, isSelf: true);

    private static CalendarEventParticipant SelfDeclined() =>
        Participant("Engineering Lead", "lead@example.com", CalendarParticipationStatus.Declined, isSelf: true);

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

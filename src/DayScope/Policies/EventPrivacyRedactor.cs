using DayScope.Application.DaySchedule;

namespace DayScope.Policies;

/// <summary>
/// Applies privacy-mode redaction to event display state.
/// </summary>
internal static class EventPrivacyRedactor
{
    private const string PRIVATE_EVENT_TITLE = "Private event";

    public static AllDayEventDisplayState RedactAllDayEvent(AllDayEventDisplayState eventState) =>
        eventState with
        {
            Title = PRIVATE_EVENT_TITLE,
            LeadingIcon = string.Empty,
            Details = RedactDetails(eventState.Details)
        };

    public static TimedEventDisplayState RedactTimedEvent(TimedEventDisplayState eventState) =>
        eventState with
        {
            Title = PRIVATE_EVENT_TITLE,
            LeadingIcon = string.Empty,
            Details = RedactDetails(eventState.Details)
        };

    public static EventDetailsDisplayState RedactDetails(EventDetailsDisplayState details) =>
        details with
        {
            Title = PRIVATE_EVENT_TITLE,
            LeadingIcon = string.Empty,
            Organizer = null,
            Description = null,
            JoinUrl = null,
            Participants = []
        };

    public static EventDetailsDisplayState? RedactDetailsOrDefault(EventDetailsDisplayState? details) =>
        details is null ? null : RedactDetails(details);
}

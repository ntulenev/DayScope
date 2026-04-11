using System.Globalization;

using DayScope.Domain.Calendar;

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Maps normalized calendar events into schedule-specific presentation models.
/// </summary>
internal static class DayScheduleEventPresentationFactory
{
    /// <summary>
    /// Creates the display state used for an all-day event chip.
    /// </summary>
    /// <param name="calendarEvent">The source event.</param>
    /// <param name="localZone">The local time zone used for formatting event details.</param>
    /// <returns>The all-day event display model.</returns>
    internal static AllDayEventDisplayState CreateAllDayEvent(
        CalendarEvent calendarEvent,
        TimeZoneInfo localZone)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        ArgumentNullException.ThrowIfNull(localZone);

        return new AllDayEventDisplayState(
            calendarEvent.SafeTitle,
            MapAppearance(calendarEvent.ParticipationStatus),
            GetStatusLabel(calendarEvent.ParticipationStatus),
            GetLeadingIcon(calendarEvent.EventKind),
            BuildEventDetails(calendarEvent, localZone));
    }

    /// <summary>
    /// Creates the intermediate layout candidate for a timed event when it intersects the visible timeline.
    /// </summary>
    /// <param name="calendarEvent">The source event.</param>
    /// <param name="timelineStart">The first visible instant on the schedule.</param>
    /// <param name="timelineEnd">The last visible instant on the schedule.</param>
    /// <param name="hourHeight">The rendered height of one hour in pixels.</param>
    /// <param name="localZone">The local time zone used for display formatting.</param>
    /// <returns>The candidate used by timeline layout, or <see langword="null"/> when the event is outside the visible range.</returns>
    internal static TimedEventLayoutCandidate? CreateTimedEventCandidate(
        CalendarEvent calendarEvent,
        DateTimeOffset timelineStart,
        DateTimeOffset timelineEnd,
        int hourHeight,
        TimeZoneInfo localZone)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        ArgumentNullException.ThrowIfNull(localZone);

        var start = TimeZoneInfo.ConvertTime(calendarEvent.Start, localZone);
        var end = TimeZoneInfo.ConvertTime(calendarEvent.EffectiveEnd, localZone);
        if (end <= timelineStart || start >= timelineEnd)
        {
            return null;
        }

        var clippedStart = start < timelineStart ? timelineStart : start;
        var clippedEnd = end > timelineEnd ? timelineEnd : end;

        return new TimedEventLayoutCandidate(
            calendarEvent.SafeTitle,
            clippedStart,
            clippedEnd,
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}",
                start.ToString("h:mm tt", _culture).Replace(" ", string.Empty, StringComparison.Ordinal),
                end.ToString("h:mm tt", _culture).Replace(" ", string.Empty, StringComparison.Ordinal)),
            hourHeight,
            MapAppearance(calendarEvent.ParticipationStatus),
            GetStatusLabel(calendarEvent.ParticipationStatus),
            GetLeadingIcon(calendarEvent.EventKind),
            BuildEventDetails(calendarEvent, localZone));
    }

    /// <summary>
    /// Builds the shared event-details payload consumed by schedule cards and flyouts.
    /// </summary>
    /// <param name="calendarEvent">The source event.</param>
    /// <param name="localZone">The local time zone used for formatting times.</param>
    /// <returns>The normalized event-details display state.</returns>
    private static EventDetailsDisplayState BuildEventDetails(
        CalendarEvent calendarEvent,
        TimeZoneInfo localZone)
    {
        var start = TimeZoneInfo.ConvertTime(calendarEvent.Start, localZone);
        var end = TimeZoneInfo.ConvertTime(calendarEvent.EffectiveEnd, localZone);
        var scheduleText = calendarEvent.IsAllDay
            ? "All day"
            : string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}",
                start.ToString("h:mm tt", _culture).Replace(" ", string.Empty, StringComparison.Ordinal),
                end.ToString("h:mm tt", _culture).Replace(" ", string.Empty, StringComparison.Ordinal));

        return new EventDetailsDisplayState(
            calendarEvent.SafeTitle,
            scheduleText,
            MapAppearance(calendarEvent.ParticipationStatus),
            GetStatusLabel(calendarEvent.ParticipationStatus),
            GetLeadingIcon(calendarEvent.EventKind),
            calendarEvent.OrganizerDisplayLabel,
            calendarEvent.Description,
            calendarEvent.JoinUrl,
            [
                .. calendarEvent.Participants
                    .Select(participant => new EventParticipantDisplayState(
                        participant.DisplayLabel,
                        GetStatusLabel(participant.ParticipationStatus),
                        participant.IsSelf))
            ]);
    }

    /// <summary>
    /// Maps the participation status to the visual appearance used by the schedule.
    /// </summary>
    /// <param name="participationStatus">The participation status to map.</param>
    /// <returns>The appearance associated with the status.</returns>
    internal static EventAppearance MapAppearance(
        CalendarParticipationStatus participationStatus)
    {
        return participationStatus switch
        {
            CalendarParticipationStatus.Accepted => EventAppearance.Accepted,
            CalendarParticipationStatus.AwaitingResponse => EventAppearance.AwaitingResponse,
            CalendarParticipationStatus.Tentative => EventAppearance.Tentative,
            CalendarParticipationStatus.Declined => EventAppearance.Declined,
            CalendarParticipationStatus.Cancelled => EventAppearance.Cancelled,
            _ => EventAppearance.Accepted
        };
    }

    /// <summary>
    /// Resolves the short status label shown in event badges and participant lists.
    /// </summary>
    /// <param name="participationStatus">The participation status to map.</param>
    /// <returns>The display label.</returns>
    internal static string GetStatusLabel(CalendarParticipationStatus participationStatus)
    {
        return participationStatus switch
        {
            CalendarParticipationStatus.Accepted => "Confirmed",
            CalendarParticipationStatus.AwaitingResponse => "Awaiting",
            CalendarParticipationStatus.Tentative => "Maybe",
            CalendarParticipationStatus.Declined => "Declined",
            CalendarParticipationStatus.Cancelled => "Cancelled",
            _ => "Confirmed"
        };
    }

    /// <summary>
    /// Resolves the leading icon prefix shown for special Google event kinds.
    /// </summary>
    /// <param name="eventKind">The event kind to map.</param>
    /// <returns>The icon prefix, or an empty string when no icon applies.</returns>
    internal static string GetLeadingIcon(CalendarEventKind eventKind)
    {
        return eventKind switch
        {
            CalendarEventKind.Default => string.Empty,
            CalendarEventKind.OutOfOffice => "⛔ ",
            CalendarEventKind.FocusTime => "🎯 ",
            CalendarEventKind.WorkingLocation => "📍 ",
            CalendarEventKind.Task => "✓ ",
            CalendarEventKind.AppointmentSchedule => "🗓 ",
            _ => string.Empty
        };
    }

    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");
}

/// <summary>
/// Represents a timed event after clipping and formatting but before collision layout is applied.
/// </summary>
internal sealed record TimedEventLayoutCandidate(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset End,
    string ScheduleText,
    int HourHeight,
    EventAppearance Appearance,
    string StatusLabel,
    string LeadingIcon,
    EventDetailsDisplayState Details);

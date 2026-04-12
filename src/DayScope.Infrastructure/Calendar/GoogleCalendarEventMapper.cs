using System.Globalization;

using Google.Apis.Calendar.v3.Data;

using DayScope.Domain.Calendar;

namespace DayScope.Infrastructure.Calendar;

/// <summary>
/// Maps Google Calendar SDK events to DayScope domain events.
/// </summary>
public sealed class GoogleCalendarEventMapper : IGoogleCalendarEventMapper
{
    /// <inheritdoc />
    public CalendarEvent? MapEvent(Event calendarEvent, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (calendarEvent.Start is null)
        {
            return null;
        }

        if (string.Equals(calendarEvent.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var isAllDay = !string.IsNullOrWhiteSpace(calendarEvent.Start.Date);
        var start = ResolveEventDateTime(calendarEvent.Start, timeZone);
        if (start is null)
        {
            return null;
        }

        var end = ResolveEventDateTime(calendarEvent.End, timeZone);
        return new CalendarEvent(
            string.IsNullOrWhiteSpace(calendarEvent.Summary)
                ? "Untitled event"
                : calendarEvent.Summary,
            start.Value,
            end,
            isAllDay,
            ResolveParticipationStatus(calendarEvent),
            ResolveEventKind(calendarEvent),
            calendarEvent.Organizer?.DisplayName,
            calendarEvent.Organizer?.Email,
            calendarEvent.Description,
            ResolveJoinUrl(calendarEvent),
            calendarEvent.Attendees?
                .Select(MapParticipant)
                .Where(participant => participant is not null)
                .Cast<CalendarEventParticipant>()
                .ToArray()
                ?? []);
    }

    private static CalendarEventParticipant? MapParticipant(EventAttendee? attendee)
    {
        if (attendee is null)
        {
            return null;
        }

        return new CalendarEventParticipant(
            attendee.DisplayName,
            attendee.Email,
            ResolveParticipationStatus(attendee.ResponseStatus),
            attendee.Self is true);
    }

    private static CalendarParticipationStatus ResolveParticipationStatus(string? responseStatus)
    {
        return responseStatus?.ToUpperInvariant() switch
        {
            "NEEDSACTION" => CalendarParticipationStatus.AwaitingResponse,
            "TENTATIVE" => CalendarParticipationStatus.Tentative,
            "DECLINED" => CalendarParticipationStatus.Declined,
            "ACCEPTED" => CalendarParticipationStatus.Accepted,
            _ => CalendarParticipationStatus.Accepted
        };
    }

    private static CalendarParticipationStatus ResolveParticipationStatus(Event calendarEvent)
    {
        if (string.Equals(calendarEvent.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return CalendarParticipationStatus.Cancelled;
        }

        var selfAttendee = calendarEvent.Attendees?.FirstOrDefault(attendee => attendee.Self is true);
        var selfResponseStatus = selfAttendee?.ResponseStatus;
        if (string.IsNullOrWhiteSpace(selfResponseStatus) && calendarEvent.Organizer?.Self is true)
        {
            selfResponseStatus = "accepted";
        }

        return ResolveParticipationStatus(selfResponseStatus);
    }

    private static Uri? ResolveJoinUrl(Event calendarEvent)
    {
        if (!string.IsNullOrWhiteSpace(calendarEvent.HangoutLink))
        {
            return Uri.TryCreate(calendarEvent.HangoutLink, UriKind.Absolute, out var joinUri)
                ? joinUri
                : null;
        }

        return null;
    }

    private static CalendarEventKind ResolveEventKind(Event calendarEvent)
    {
        return calendarEvent.EventType switch
        {
            "focusTime" => CalendarEventKind.FocusTime,
            "outOfOffice" => CalendarEventKind.OutOfOffice,
            "workingLocation" => CalendarEventKind.WorkingLocation,
            "task" => CalendarEventKind.Task,
            "appointmentSchedule" => CalendarEventKind.AppointmentSchedule,
            _ => CalendarEventKind.Default
        };
    }

    private static DateTimeOffset? ResolveEventDateTime(
        EventDateTime? eventDateTime,
        TimeZoneInfo timeZone)
    {
        if (eventDateTime?.DateTimeDateTimeOffset is DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset;
        }

        if (!string.IsNullOrWhiteSpace(eventDateTime?.DateTimeRaw) &&
            DateTimeOffset.TryParse(
                eventDateTime.DateTimeRaw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var parsedDateTimeOffset))
        {
            return parsedDateTimeOffset;
        }

        if (string.IsNullOrWhiteSpace(eventDateTime?.Date))
        {
            return null;
        }

        if (!DateOnly.TryParseExact(
                eventDateTime.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            return null;
        }

        var localDateTime = parsedDate.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime));
    }
}

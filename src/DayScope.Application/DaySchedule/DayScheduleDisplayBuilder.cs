using System.Globalization;

using DayScope.Application.Calendar;
using DayScope.Domain.Calendar;
using DayScope.Domain.Configuration;

namespace DayScope.Application.DaySchedule;

public sealed class DayScheduleDisplayBuilder
{
    public DayScheduleDisplayState Build(
        CalendarLoadResult loadResult,
        DayScheduleSettings settings,
        DateTimeOffset now,
        double? availableScheduleWidth = null)
    {
        ArgumentNullException.ThrowIfNull(loadResult);
        ArgumentNullException.ThrowIfNull(settings);

        var localZone = TimeZoneInfo.Local;
        var localNow = TimeZoneInfo.ConvertTime(now, localZone);
        var date = DateOnly.FromDateTime(localNow.DateTime);
        var timelineStart = CreateDateTimeOffset(localZone, date, settings.StartHour);
        var timelineEnd = CreateDateTimeOffset(localZone, date, settings.EndHour);
        var timelineHeight = (settings.EndHour - settings.StartHour) * settings.HourHeight;
        var secondaryZone = TryResolveTimeZone(settings.SecondaryTimeZoneId);
        var scheduleWidth = ResolveScheduleWidth(settings.ScheduleCanvasWidth, availableScheduleWidth);

        var allDayEvents = loadResult.Agenda.Events
            .Where(calendarEvent => calendarEvent.IsAllDay)
            .Select(calendarEvent => new AllDayEventDisplayState(
                calendarEvent.SafeTitle,
                MapAppearance(calendarEvent.ParticipationStatus),
                GetStatusLabel(calendarEvent.ParticipationStatus),
                GetLeadingIcon(calendarEvent.EventKind)))
            .ToArray();

        var timedEvents = BuildTimedEvents(
            loadResult.Agenda.Events,
            timelineStart,
            timelineEnd,
            scheduleWidth,
            settings.HourHeight)
            .ToArray();

        var statusText = GetStatusText(loadResult.Status);
        if (loadResult.Status == CalendarLoadStatus.Success &&
            allDayEvents.Length == 0 &&
            timedEvents.Length == 0)
        {
            statusText = "No events scheduled for today.";
        }

        return new DayScheduleDisplayState(
            localNow.ToString("MMMM yyyy", _culture),
            localNow.ToString("ddd", _culture),
            localNow.ToString("%d", _culture),
            localNow.ToString("dddd, d MMMM", _culture),
            ResolveTimeZoneLabel(localZone, settings.PrimaryTimeZoneLabel, localNow),
            secondaryZone is null
                ? null
                : ResolveTimeZoneLabel(
                    secondaryZone,
                    settings.SecondaryTimeZoneLabel,
                    TimeZoneInfo.ConvertTime(now, secondaryZone)),
            BuildTimelineHours(timelineStart, settings, localZone),
            secondaryZone is null
                ? []
                : BuildTimelineHours(timelineStart, settings, secondaryZone),
            allDayEvents,
            timedEvents,
            scheduleWidth,
            timelineHeight,
            statusText,
            !string.IsNullOrWhiteSpace(statusText),
            TryBuildNowLine(localNow, timelineStart, timelineEnd, settings.HourHeight),
            localNow.ToString("h:mm tt", _culture).Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    private static IReadOnlyList<TimelineHourDisplayState> BuildTimelineHours(
        DateTimeOffset timelineStart,
        DayScheduleSettings settings,
        TimeZoneInfo zone)
    {
        var hours = new List<TimelineHourDisplayState>();
        for (var hour = settings.StartHour; hour <= settings.EndHour; hour++)
        {
            var timelineHourOffset = hour - settings.StartHour;
            var instant = timelineStart.AddHours(timelineHourOffset);
            var convertedInstant = TimeZoneInfo.ConvertTime(instant, zone);
            hours.Add(new TimelineHourDisplayState(
                convertedInstant.ToString("h:mm tt", _culture)
                    .Replace(":00", string.Empty, StringComparison.Ordinal)
                    .Replace(" ", string.Empty, StringComparison.Ordinal),
                timelineHourOffset * settings.HourHeight));
        }

        return hours;
    }

    private static int ResolveScheduleWidth(
        int configuredWidth,
        double? availableScheduleWidth)
    {
        var width = availableScheduleWidth.HasValue
            ? (int)Math.Floor(availableScheduleWidth.Value)
            : configuredWidth;

        return Math.Max(420, width);
    }

    private static IEnumerable<TimedEventDisplayState> BuildTimedEvents(
        IReadOnlyList<CalendarEvent> events,
        DateTimeOffset timelineStart,
        DateTimeOffset timelineEnd,
        int canvasWidth,
        int hourHeight)
    {
        var candidates = events
            .Where(calendarEvent => !calendarEvent.IsAllDay)
            .Select(calendarEvent => CreateLayoutCandidate(
                calendarEvent,
                timelineStart,
                timelineEnd,
                hourHeight))
            .Where(candidate => candidate is not null)
            .Cast<LayoutCandidate>()
            .OrderBy(candidate => candidate.Start)
            .ThenBy(candidate => candidate.End)
            .ToArray();

        if (candidates.Length == 0)
        {
            yield break;
        }

        var groups = new List<List<LayoutCandidate>>();
        var currentGroup = new List<LayoutCandidate>();
        var groupMaxEnd = DateTimeOffset.MinValue;

        foreach (var candidate in candidates)
        {
            if (currentGroup.Count == 0 || candidate.Start < groupMaxEnd)
            {
                currentGroup.Add(candidate);
                if (candidate.End > groupMaxEnd)
                {
                    groupMaxEnd = candidate.End;
                }

                continue;
            }

            groups.Add(currentGroup);
            currentGroup = [candidate];
            groupMaxEnd = candidate.End;
        }

        if (currentGroup.Count > 0)
        {
            groups.Add(currentGroup);
        }

        foreach (var group in groups)
        {
            foreach (var calendarEvent in BuildEventGroup(group, timelineStart, canvasWidth))
            {
                yield return calendarEvent;
            }
        }
    }

    private static IEnumerable<TimedEventDisplayState> BuildEventGroup(
        IReadOnlyList<LayoutCandidate> group,
        DateTimeOffset timelineStart,
        int canvasWidth)
    {
        var columnEndTimes = new List<DateTimeOffset>();
        var assignments = new List<(LayoutCandidate Candidate, int Column)>();

        foreach (var candidate in group)
        {
            var assignedColumn = -1;
            for (var columnIndex = 0; columnIndex < columnEndTimes.Count; columnIndex++)
            {
                if (columnEndTimes[columnIndex] <= candidate.Start)
                {
                    assignedColumn = columnIndex;
                    columnEndTimes[columnIndex] = candidate.End;
                    break;
                }
            }

            if (assignedColumn < 0)
            {
                assignedColumn = columnEndTimes.Count;
                columnEndTimes.Add(candidate.End);
            }

            assignments.Add((candidate, assignedColumn));
        }

        const double gap = 10;
        var columnCount = Math.Max(1, columnEndTimes.Count);
        var availableWidth = canvasWidth - ((columnCount - 1) * gap);
        var columnWidth = availableWidth / columnCount;

        return assignments.Select(assignment =>
        {
            var top = (assignment.Candidate.Start - timelineStart).TotalMinutes
                / 60d
                * assignment.Candidate.HourHeight;
            var height = Math.Max(
                32,
                (assignment.Candidate.End - assignment.Candidate.Start).TotalMinutes
                / 60d
                * assignment.Candidate.HourHeight);

            return new TimedEventDisplayState(
                assignment.Candidate.Title,
                assignment.Candidate.ScheduleText,
                top,
                height,
                assignment.Column * (columnWidth + gap),
                columnWidth,
                columnCount > 1 || height < 54,
                assignment.Candidate.Appearance,
                assignment.Candidate.StatusLabel,
                assignment.Candidate.LeadingIcon);
        });
    }

    private static LayoutCandidate? CreateLayoutCandidate(
        CalendarEvent calendarEvent,
        DateTimeOffset timelineStart,
        DateTimeOffset timelineEnd,
        int hourHeight)
    {
        var start = calendarEvent.Start.ToLocalTime();
        var end = (calendarEvent.End ?? calendarEvent.Start.AddMinutes(30)).ToLocalTime();
        if (end <= start)
        {
            end = start.AddMinutes(30);
        }

        if (end <= timelineStart || start >= timelineEnd)
        {
            return null;
        }

        var clippedStart = start < timelineStart ? timelineStart : start;
        var clippedEnd = end > timelineEnd ? timelineEnd : end;

        return new LayoutCandidate(
            calendarEvent.SafeTitle,
            clippedStart,
            clippedEnd,
            $"{start:h:mm tt}-{end:h:mm tt}"
                .Replace(" ", string.Empty, StringComparison.Ordinal),
            hourHeight,
            MapAppearance(calendarEvent.ParticipationStatus),
            GetStatusLabel(calendarEvent.ParticipationStatus),
            GetLeadingIcon(calendarEvent.EventKind));
    }

    private static DateTimeOffset CreateDateTimeOffset(
        TimeZoneInfo timeZone,
        DateOnly date,
        int hour)
    {
        var dayOffset = hour / 24;
        var normalizedHour = hour % 24;
        var dateTime = date
            .AddDays(dayOffset)
            .ToDateTime(new TimeOnly(normalizedHour, 0));

        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }

    private static double? TryBuildNowLine(
        DateTimeOffset localNow,
        DateTimeOffset timelineStart,
        DateTimeOffset timelineEnd,
        int hourHeight)
    {
        if (localNow < timelineStart || localNow > timelineEnd)
        {
            return null;
        }

        return (localNow - timelineStart).TotalMinutes / 60d * hourHeight;
    }

    private static string ResolveTimeZoneLabel(
        TimeZoneInfo timeZone,
        string? configuredLabel,
        DateTimeOffset instant)
    {
        if (!string.IsNullOrWhiteSpace(configuredLabel))
        {
            return configuredLabel.Trim();
        }

        var offset = timeZone.GetUtcOffset(instant);
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var absoluteOffset = offset.Duration();
        return $"UTC{sign}{absoluteOffset:hh\\:mm}";
    }

    private static TimeZoneInfo? TryResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static string GetStatusText(CalendarLoadStatus status)
    {
        return status switch
        {
            CalendarLoadStatus.Loading => "Loading today's schedule...",
            CalendarLoadStatus.Disabled => "Google Calendar is disabled in appsettings.",
            CalendarLoadStatus.ClientSecretsMissing =>
                "Add Google OAuth client JSON to connect Google Calendar.",
            CalendarLoadStatus.AuthorizationRequired =>
                "Google Calendar sign-in is required to show today's schedule.",
            CalendarLoadStatus.AccessDenied =>
                "Calendar not found or access denied.",
            CalendarLoadStatus.Unavailable =>
                "Google Calendar is unavailable right now.",
            CalendarLoadStatus.NoEvents => "No events scheduled for today.",
            _ => string.Empty
        };
    }

    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    private static EventAppearance MapAppearance(
        CalendarParticipationStatus participationStatus)
    {
        return participationStatus switch
        {
            CalendarParticipationStatus.AwaitingResponse => EventAppearance.AwaitingResponse,
            CalendarParticipationStatus.Tentative => EventAppearance.Tentative,
            CalendarParticipationStatus.Declined => EventAppearance.Declined,
            CalendarParticipationStatus.Cancelled => EventAppearance.Cancelled,
            _ => EventAppearance.Accepted
        };
    }

    private static string GetStatusLabel(CalendarParticipationStatus participationStatus)
    {
        return participationStatus switch
        {
            CalendarParticipationStatus.AwaitingResponse => "Awaiting",
            CalendarParticipationStatus.Tentative => "Maybe",
            CalendarParticipationStatus.Declined => "Declined",
            CalendarParticipationStatus.Cancelled => "Cancelled",
            _ => "Confirmed"
        };
    }

    private static string GetLeadingIcon(CalendarEventKind eventKind)
    {
        return eventKind switch
        {
            CalendarEventKind.OutOfOffice => "⛔ ",
            CalendarEventKind.FocusTime => "🎯 ",
            CalendarEventKind.WorkingLocation => "📍 ",
            CalendarEventKind.Task => "✓ ",
            CalendarEventKind.AppointmentSchedule => "🗓 ",
            _ => string.Empty
        };
    }

    private sealed record LayoutCandidate(
        string Title,
        DateTimeOffset Start,
        DateTimeOffset End,
        string ScheduleText,
        int HourHeight,
        EventAppearance Appearance,
        string StatusLabel,
        string LeadingIcon);
}

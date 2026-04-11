using DayScope.Domain.Calendar;

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Lays out timed event candidates into non-overlapping schedule cards.
/// </summary>
internal static class DayScheduleTimelineLayout
{
    /// <summary>
    /// Builds the positioned timed event cards for the visible schedule range.
    /// </summary>
    /// <param name="events">The normalized calendar events to render.</param>
    /// <param name="timelineStart">The first visible instant on the schedule.</param>
    /// <param name="timelineEnd">The last visible instant on the schedule.</param>
    /// <param name="canvasWidth">The available timeline canvas width.</param>
    /// <param name="hourHeight">The rendered height of one hour in pixels.</param>
    /// <param name="localZone">The local time zone used for clipping and formatting.</param>
    /// <returns>The positioned timed event display states.</returns>
    internal static IReadOnlyList<TimedEventDisplayState> BuildTimedEvents(
        IReadOnlyList<CalendarEvent> events,
        DateTimeOffset timelineStart,
        DateTimeOffset timelineEnd,
        int canvasWidth,
        int hourHeight,
        TimeZoneInfo localZone)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(localZone);

        var candidates = events
            .Where(calendarEvent => !calendarEvent.IsAllDay)
            .Select(calendarEvent => DayScheduleEventPresentationFactory.CreateTimedEventCandidate(
                calendarEvent,
                timelineStart,
                timelineEnd,
                hourHeight,
                localZone))
            .Where(candidate => candidate is not null)
            .Cast<TimedEventLayoutCandidate>()
            .OrderBy(candidate => candidate.Start)
            .ThenBy(candidate => candidate.End)
            .ToArray();

        if (candidates.Length == 0)
        {
            return [];
        }

        var groups = new List<List<TimedEventLayoutCandidate>>();
        var currentGroup = new List<TimedEventLayoutCandidate>();
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

        return
        [
            .. groups.SelectMany(group => BuildEventGroup(group, timelineStart, canvasWidth))
        ];
    }

    /// <summary>
    /// Assigns columns within one overlapping event group and produces the final card layout.
    /// </summary>
    /// <param name="group">The overlapping timed events in one collision group.</param>
    /// <param name="timelineStart">The first visible instant on the schedule.</param>
    /// <param name="canvasWidth">The available timeline canvas width.</param>
    /// <returns>The positioned cards for the group.</returns>
    private static IEnumerable<TimedEventDisplayState> BuildEventGroup(
        IReadOnlyList<TimedEventLayoutCandidate> group,
        DateTimeOffset timelineStart,
        int canvasWidth)
    {
        var columnEndTimes = new List<DateTimeOffset>();
        var assignments = new List<(TimedEventLayoutCandidate Candidate, int Column)>();

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
                20,
                (assignment.Candidate.End - assignment.Candidate.Start).TotalMinutes
                / 60d
                * assignment.Candidate.HourHeight);
            var isMicro = height < 26;
            var isCompact = columnCount > 1 || height < 52;
            var showScheduleText = !isCompact && !isMicro;
            var showStatusBadge = !isMicro && height >= 28 && columnWidth >= 150;

            return new TimedEventDisplayState(
                assignment.Candidate.Title,
                assignment.Candidate.ScheduleText,
                top,
                height,
                assignment.Column * (columnWidth + gap),
                columnWidth,
                isCompact,
                isMicro,
                showScheduleText,
                showStatusBadge,
                assignment.Candidate.Appearance,
                assignment.Candidate.StatusLabel,
                assignment.Candidate.LeadingIcon,
                assignment.Candidate.Details);
        });
    }
}

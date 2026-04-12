using DayScope.Domain.Calendar;

namespace DayScope.Infrastructure.Demo;

/// <summary>
/// Builds deterministic synthetic calendar agendas for demo mode.
/// </summary>
public sealed class DemoAgendaFactory : IDemoAgendaFactory
{
    /// <inheritdoc />
    public CalendarAgenda BuildAgenda(DateOnly day, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        return new CalendarAgenda(
        [
            ..DemoAgendaSeedData.EventDefinitions
                .Select(definition => CreateEvent(definition, day, timeZone))
        ]);
    }

    private static CalendarEvent CreateEvent(
        DemoCalendarEventDefinition definition,
        DateOnly day,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(timeZone);

        return new CalendarEvent(
            definition.Title,
            At(day, timeZone, definition.StartHour, definition.StartMinute),
            At(day, timeZone, definition.EndHour, definition.EndMinute),
            definition.IsAllDay,
            definition.ParticipationStatus,
            definition.EventKind,
            definition.OrganizerName,
            definition.OrganizerEmail,
            definition.Description,
            BuildJoinUri(definition.JoinPath),
            definition.Participants);
    }

    private static DateTimeOffset At(
        DateOnly day,
        TimeZoneInfo timeZone,
        int hour,
        int minute)
    {
        var dayOffset = hour / 24;
        var normalizedHour = hour % 24;
        var localDateTime = day
            .AddDays(dayOffset)
            .ToDateTime(new TimeOnly(normalizedHour, minute));

        return new DateTimeOffset(localDateTime, timeZone.GetUtcOffset(localDateTime));
    }

    private static Uri? BuildJoinUri(string? joinPath)
    {
        if (string.IsNullOrWhiteSpace(joinPath))
        {
            return null;
        }

        return new Uri($"https://example.com/meet/{joinPath.Trim()}", UriKind.Absolute);
    }
}

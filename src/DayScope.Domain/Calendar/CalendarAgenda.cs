namespace DayScope.Domain.Calendar;

/// <summary>
/// Represents the ordered set of events loaded for a calendar day.
/// </summary>
public sealed class CalendarAgenda
{
    public static CalendarAgenda Empty { get; } = new([]);

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarAgenda"/> class.
    /// </summary>
    /// <param name="events">The events to include in the agenda.</param>
    public CalendarAgenda(IReadOnlyList<CalendarEvent>? events)
    {
        Events = events?
            .OfType<CalendarEvent>()
            .OrderBy(calendarEvent => calendarEvent.Start)
            .ThenBy(calendarEvent => calendarEvent.EffectiveEnd)
            .ToArray()
            ?? [];
    }

    public IReadOnlyList<CalendarEvent> Events { get; }
}

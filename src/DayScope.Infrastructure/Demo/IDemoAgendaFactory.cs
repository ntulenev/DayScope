using DayScope.Domain.Calendar;

namespace DayScope.Infrastructure.Demo;

/// <summary>
/// Builds deterministic demo agendas for the requested day.
/// </summary>
public interface IDemoAgendaFactory
{
    /// <summary>
    /// Builds the demo agenda for the requested date and time zone.
    /// </summary>
    /// <param name="day">The day to generate.</param>
    /// <param name="timeZone">The local time zone used for event instants.</param>
    /// <returns>The generated demo agenda.</returns>
    CalendarAgenda BuildAgenda(DateOnly day, TimeZoneInfo timeZone);
}

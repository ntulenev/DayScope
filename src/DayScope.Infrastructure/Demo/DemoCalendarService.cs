using DayScope.Application.Abstractions;
using DayScope.Application.Calendar;

namespace DayScope.Infrastructure.Demo;

/// <summary>
/// Provides deterministic synthetic calendar data for demo mode.
/// </summary>
public sealed class DemoCalendarService : ICalendarService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DemoCalendarService"/> class.
    /// </summary>
    /// <param name="demoAgendaFactory">The factory used to build demo agendas.</param>
    public DemoCalendarService(IDemoAgendaFactory demoAgendaFactory)
    {
        ArgumentNullException.ThrowIfNull(demoAgendaFactory);

        _demoAgendaFactory = demoAgendaFactory;
    }

    /// <inheritdoc />
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
                _demoAgendaFactory.BuildAgenda(day, timeZone)));
    }

    private readonly IDemoAgendaFactory _demoAgendaFactory;
}

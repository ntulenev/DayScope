using DayScope.Domain.Calendar;

namespace DayScope.Application.Calendar;

/// <summary>
/// Represents the result of loading a calendar agenda.
/// </summary>
/// <param name="Agenda">The agenda that was loaded.</param>
/// <param name="Status">The outcome of the load operation.</param>
public sealed record CalendarLoadResult(CalendarAgenda Agenda, CalendarLoadStatus Status)
{
    /// <summary>
    /// Creates a successful load result.
    /// </summary>
    /// <param name="agenda">The loaded agenda.</param>
    /// <returns>A successful result.</returns>
    public static CalendarLoadResult Success(CalendarAgenda agenda) =>
        new(agenda, CalendarLoadStatus.Success);

    /// <summary>
    /// Creates a result for a non-success status with an empty agenda.
    /// </summary>
    /// <param name="status">The load status to report.</param>
    /// <returns>A result with an empty agenda and the requested status.</returns>
    public static CalendarLoadResult FromStatus(CalendarLoadStatus status) =>
        new(CalendarAgenda.Empty, status);
}

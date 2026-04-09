using DayScope.Domain.Calendar;

namespace DayScope.Application.Calendar;

public sealed record CalendarLoadResult(CalendarAgenda Agenda, CalendarLoadStatus Status)
{
    public static CalendarLoadResult Success(CalendarAgenda agenda) =>
        new(agenda, CalendarLoadStatus.Success);

    public static CalendarLoadResult FromStatus(CalendarLoadStatus status) =>
        new(CalendarAgenda.Empty, status);
}

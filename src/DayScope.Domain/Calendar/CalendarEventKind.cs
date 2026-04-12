namespace DayScope.Domain.Calendar;

/// <summary>
/// Represents the source event kind returned by Google Calendar.
/// </summary>
public enum CalendarEventKind
{
    Default = 0,
    FocusTime = 1,
    OutOfOffice = 2,
    WorkingLocation = 3,
    Task = 4,
    AppointmentSchedule = 5
}

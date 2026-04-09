namespace DayScope.Domain.Configuration;

public sealed class DayScheduleSettings
{
    public int StartHour { get; set; } = 6;

    public int EndHour { get; set; } = 20;

    public int HourHeight { get; set; } = 76;

    public int ScheduleCanvasWidth { get; set; } = 860;

    public string? PrimaryTimeZoneLabel { get; set; }

    public string? SecondaryTimeZoneId { get; set; }

    public string? SecondaryTimeZoneLabel { get; set; }
}

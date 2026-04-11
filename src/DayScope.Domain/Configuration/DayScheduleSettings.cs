namespace DayScope.Domain.Configuration;

/// <summary>
/// Represents settings used to render the day schedule dashboard.
/// </summary>
public sealed class DayScheduleSettings
{
    public int StartHour { get; set; } = 6;

    public int EndHour { get; set; } = 20;

    public int HourHeight { get; set; } = 76;

    public int ScheduleCanvasWidth { get; set; } = 860;

    public string? PrimaryTimeZoneLabel { get; set; }

    public string? SecondaryTimeZoneId { get; set; }

    public string? SecondaryTimeZoneLabel { get; set; }

    /// <summary>
    /// Normalizes the settings into supported ranges and formats.
    /// </summary>
    public void Normalize()
    {
        StartHour = Math.Clamp(StartHour, 0, 23);
        EndHour = Math.Clamp(EndHour, 1, 24);
        if (EndHour <= StartHour)
        {
            StartHour = 6;
            EndHour = 20;
        }

        HourHeight = Math.Clamp(HourHeight, 40, 160);
        ScheduleCanvasWidth = Math.Clamp(ScheduleCanvasWidth, 480, 1200);
        PrimaryTimeZoneLabel = NormalizeOptionalText(PrimaryTimeZoneLabel);
        SecondaryTimeZoneId = NormalizeOptionalText(SecondaryTimeZoneId);
        SecondaryTimeZoneLabel = NormalizeOptionalText(SecondaryTimeZoneLabel);
    }

    /// <summary>
    /// Validates the current settings and returns any failures.
    /// </summary>
    /// <returns>A list of validation failures.</returns>
    public IReadOnlyList<string> Validate()
    {
        List<string> failures = [];

        if (EndHour <= StartHour)
        {
            failures.Add("DaySchedule:EndHour must be greater than StartHour.");
        }

        return failures;
    }

    /// <summary>
    /// Normalizes optional configuration text into trimmed nullable values.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <returns>The trimmed value, or <see langword="null"/> when blank.</returns>
    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Formats display labels for time zones used in the schedule header.
/// </summary>
internal static class DayScheduleTimeZoneLabelFormatter
{
    /// <summary>
    /// Formats the label shown for a schedule time zone.
    /// </summary>
    /// <param name="timeZone">The time zone being labeled.</param>
    /// <param name="configuredLabel">The optional configured label override.</param>
    /// <param name="instant">The reference instant used to calculate the UTC offset.</param>
    /// <returns>The configured label, or a generated UTC offset label.</returns>
    public static string Format(
        TimeZoneInfo timeZone,
        string? configuredLabel,
        DateTimeOffset instant)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (!string.IsNullOrWhiteSpace(configuredLabel))
        {
            return configuredLabel.Trim();
        }

        var offset = timeZone.GetUtcOffset(instant);
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var absoluteOffset = offset.Duration();
        return $"UTC{sign}{absoluteOffset:hh\\:mm}";
    }
}

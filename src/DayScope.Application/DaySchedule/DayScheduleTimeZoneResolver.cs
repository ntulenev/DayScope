namespace DayScope.Application.DaySchedule;

/// <summary>
/// Resolves optional configured time zones for the schedule display.
/// </summary>
internal static class DayScheduleTimeZoneResolver
{
    /// <summary>
    /// Resolves a configured time-zone identifier when it is valid on the current machine.
    /// </summary>
    /// <param name="timeZoneId">The configured time-zone identifier.</param>
    /// <returns>The resolved time zone, or <see langword="null"/> when the identifier is blank or invalid.</returns>
    public static TimeZoneInfo? TryResolve(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}

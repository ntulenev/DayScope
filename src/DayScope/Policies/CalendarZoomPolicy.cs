namespace DayScope.Policies;

/// <summary>
/// Defines calendar zoom bounds and normalization rules shared by UI state and persistence.
/// </summary>
internal static class CalendarZoomPolicy
{
    public const double DEFAULT_SCALE = 1;
    public const double MINIMUM_SCALE = 0.85;
    public const double MAXIMUM_SCALE = 1.15;
    public const double STEP = 0.01;

    public static double Increase(double scale) => Normalize(scale + STEP);

    public static double Decrease(double scale) => Normalize(scale - STEP);

    public static double Normalize(double scale) =>
        Math.Round(
            Math.Clamp(scale, MINIMUM_SCALE, MAXIMUM_SCALE),
            2);

    public static string FormatPercent(double scale) => $"{(int)Math.Round(Normalize(scale) * 100)}%";
}

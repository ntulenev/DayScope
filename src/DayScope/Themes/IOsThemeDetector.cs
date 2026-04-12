namespace DayScope.Themes;

/// <summary>
/// Detects the current OS app-theme preference.
/// </summary>
public interface IOsThemeDetector
{
    /// <summary>
    /// Detects the concrete application theme preferred by the OS.
    /// </summary>
    /// <returns>The concrete theme mode preferred by the OS.</returns>
    AppThemeMode DetectThemeMode();
}

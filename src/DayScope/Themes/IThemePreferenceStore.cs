namespace DayScope.Themes;

/// <summary>
/// Persists and restores the selected application theme mode.
/// </summary>
public interface IThemePreferenceStore
{
    /// <summary>
    /// Loads the last saved theme mode.
    /// </summary>
    /// <returns>The saved theme mode, or <see cref="AppThemeMode.Os"/> when no preference is available.</returns>
    AppThemeMode LoadThemeMode();

    /// <summary>
    /// Saves the selected theme mode for future launches.
    /// </summary>
    /// <param name="themeMode">The theme mode to persist.</param>
    void SaveThemeMode(AppThemeMode themeMode);
}

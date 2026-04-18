namespace DayScope.Themes;

/// <summary>
/// Represents the serialized user-preference payload.
/// </summary>
internal sealed class ThemePreferencesDocument
{
    /// <summary>
    /// Gets the persisted theme mode.
    /// </summary>
    public AppThemeMode ThemeMode { get; init; } = AppThemeMode.Os;

    /// <summary>
    /// Gets the persisted secondary-time-zone visibility preference.
    /// </summary>
    public bool? ShowSecondaryTimeZone { get; init; }
}

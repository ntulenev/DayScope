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

    /// <summary>
    /// Gets the persisted calendar body zoom scale.
    /// </summary>
    public double? CalendarZoomScale { get; init; }

    /// <summary>
    /// Gets the persisted privacy-mode preference.
    /// </summary>
    public bool? IsPrivacyModeEnabled { get; init; }
}

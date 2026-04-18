namespace DayScope.Themes;

/// <summary>
/// Persists and restores whether the secondary time zone should be shown in the schedule UI.
/// </summary>
public interface ISecondaryTimeZonePreferenceStore
{
    /// <summary>
    /// Loads whether the secondary time zone should be shown.
    /// </summary>
    /// <returns><see langword="true"/> when the secondary time zone should be visible.</returns>
    bool LoadShowSecondaryTimeZone();

    /// <summary>
    /// Saves whether the secondary time zone should be shown.
    /// </summary>
    /// <param name="showSecondaryTimeZone">Whether the secondary time zone should be visible.</param>
    void SaveShowSecondaryTimeZone(bool showSecondaryTimeZone);
}

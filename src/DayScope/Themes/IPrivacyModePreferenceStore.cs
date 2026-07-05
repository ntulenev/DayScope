namespace DayScope.Themes;

/// <summary>
/// Persists and restores whether sensitive schedule and email details should be hidden.
/// </summary>
public interface IPrivacyModePreferenceStore
{
    /// <summary>
    /// Loads whether privacy mode should be enabled.
    /// </summary>
    /// <returns><see langword="true"/> when sensitive details should be hidden.</returns>
    bool LoadPrivacyModeEnabled();

    /// <summary>
    /// Saves whether privacy mode should be enabled.
    /// </summary>
    /// <param name="isPrivacyModeEnabled">Whether sensitive details should be hidden.</param>
    void SavePrivacyModeEnabled(bool isPrivacyModeEnabled);
}

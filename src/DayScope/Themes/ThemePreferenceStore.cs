using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

using DayScope.Policies;

namespace DayScope.Themes;

/// <summary>
/// Persists user interface preferences in local app data.
/// </summary>
public sealed class ThemePreferenceStore :
    IThemePreferenceStore,
    ISecondaryTimeZonePreferenceStore,
    ICalendarZoomPreferenceStore,
    IPrivacyModePreferenceStore
{
    /// <summary>
    /// Loads the last saved theme mode.
    /// </summary>
    /// <returns>
    /// The saved theme mode, or <see cref="AppThemeMode.Os"/> when no preference is available.
    /// </returns>
    public AppThemeMode LoadThemeMode() => ReadPreferences().ThemeMode;

    /// <summary>
    /// Saves the selected theme mode for future launches.
    /// </summary>
    /// <param name="themeMode">The theme mode to persist.</param>
    public void SaveThemeMode(AppThemeMode themeMode)
    {
        var existingPreferences = ReadPreferences();
        WritePreferences(new ThemePreferencesDocument
        {
            ThemeMode = themeMode,
            ShowSecondaryTimeZone = existingPreferences.ShowSecondaryTimeZone,
            CalendarZoomScale = existingPreferences.CalendarZoomScale,
            IsPrivacyModeEnabled = existingPreferences.IsPrivacyModeEnabled
        });
    }

    /// <summary>
    /// Loads whether the secondary time zone should be shown.
    /// </summary>
    /// <returns><see langword="true"/> when the secondary time zone should be visible.</returns>
    public bool LoadShowSecondaryTimeZone() => ReadPreferences().ShowSecondaryTimeZone ?? true;

    /// <summary>
    /// Saves whether the secondary time zone should be shown.
    /// </summary>
    /// <param name="showSecondaryTimeZone">Whether the secondary time zone should be visible.</param>
    public void SaveShowSecondaryTimeZone(bool showSecondaryTimeZone)
    {
        var existingPreferences = ReadPreferences();
        WritePreferences(new ThemePreferencesDocument
        {
            ThemeMode = existingPreferences.ThemeMode,
            ShowSecondaryTimeZone = showSecondaryTimeZone,
            CalendarZoomScale = existingPreferences.CalendarZoomScale,
            IsPrivacyModeEnabled = existingPreferences.IsPrivacyModeEnabled
        });
    }

    /// <summary>
    /// Loads the persisted calendar zoom scale.
    /// </summary>
    /// <returns>The saved zoom scale, or 1 when no preference is available.</returns>
    public double LoadCalendarZoomScale() =>
        CalendarZoomPolicy.Normalize(ReadPreferences().CalendarZoomScale ?? CalendarZoomPolicy.DEFAULT_SCALE);

    /// <summary>
    /// Saves the calendar zoom scale.
    /// </summary>
    /// <param name="calendarZoomScale">The calendar zoom scale to persist.</param>
    public void SaveCalendarZoomScale(double calendarZoomScale)
    {
        var existingPreferences = ReadPreferences();
        WritePreferences(new ThemePreferencesDocument
        {
            ThemeMode = existingPreferences.ThemeMode,
            ShowSecondaryTimeZone = existingPreferences.ShowSecondaryTimeZone,
            CalendarZoomScale = CalendarZoomPolicy.Normalize(calendarZoomScale),
            IsPrivacyModeEnabled = existingPreferences.IsPrivacyModeEnabled
        });
    }

    /// <summary>
    /// Loads whether privacy mode should be enabled.
    /// </summary>
    /// <returns><see langword="true"/> when sensitive details should be hidden.</returns>
    public bool LoadPrivacyModeEnabled() => ReadPreferences().IsPrivacyModeEnabled ?? false;

    /// <summary>
    /// Saves whether privacy mode should be enabled.
    /// </summary>
    /// <param name="isPrivacyModeEnabled">Whether sensitive details should be hidden.</param>
    public void SavePrivacyModeEnabled(bool isPrivacyModeEnabled)
    {
        var existingPreferences = ReadPreferences();
        WritePreferences(new ThemePreferencesDocument
        {
            ThemeMode = existingPreferences.ThemeMode,
            ShowSecondaryTimeZone = existingPreferences.ShowSecondaryTimeZone,
            CalendarZoomScale = existingPreferences.CalendarZoomScale,
            IsPrivacyModeEnabled = isPrivacyModeEnabled
        });
    }

    private ThemePreferencesDocument ReadPreferences()
    {
        try
        {
            if (!File.Exists(_preferencesPath))
            {
                return new ThemePreferencesDocument();
            }

            var json = File.ReadAllText(_preferencesPath);
            return JsonSerializer.Deserialize<ThemePreferencesDocument>(json, _jsonSerializerOptions)
                ?? new ThemePreferencesDocument();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return new ThemePreferencesDocument();
        }
    }

    private void WritePreferences(ThemePreferencesDocument preferences)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencesPath)!);

            var json = JsonSerializer.Serialize(preferences, _jsonSerializerOptions);
            File.WriteAllText(_preferencesPath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Ignore persistence failures and keep the app usable.
        }
    }

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly string _preferencesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DayScope",
        "preferences.json");
}

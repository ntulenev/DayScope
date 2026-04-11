using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DayScope.Themes;

/// <summary>
/// Persists the user's selected application theme in local app data.
/// </summary>
public sealed class ThemePreferenceStore
{
    /// <summary>
    /// Loads the last saved theme mode.
    /// </summary>
    /// <returns>
    /// The saved theme mode, or <see cref="AppThemeMode.Os"/> when no preference is available.
    /// </returns>
    public AppThemeMode LoadThemeMode()
    {
        try
        {
            if (!File.Exists(_preferencesPath))
            {
                return AppThemeMode.Os;
            }

            var json = File.ReadAllText(_preferencesPath);
            var preferences = JsonSerializer.Deserialize<ThemePreferencesDocument>(json, _jsonSerializerOptions);
            return preferences?.ThemeMode ?? AppThemeMode.Os;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return AppThemeMode.Os;
        }
    }

    /// <summary>
    /// Saves the selected theme mode for future launches.
    /// </summary>
    /// <param name="themeMode">The theme mode to persist.</param>
    public void SaveThemeMode(AppThemeMode themeMode)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencesPath)!);

            var json = JsonSerializer.Serialize(
                new ThemePreferencesDocument(themeMode),
                _jsonSerializerOptions);
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

    private sealed record ThemePreferencesDocument(AppThemeMode ThemeMode);
}

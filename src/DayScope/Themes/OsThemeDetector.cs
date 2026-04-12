using System.IO;
using System.Security;

using Microsoft.Win32;

namespace DayScope.Themes;

/// <summary>
/// Detects the current Windows app-theme preference from the registry.
/// </summary>
public sealed class WindowsOsThemeDetector : IOsThemeDetector
{
    /// <inheritdoc />
    public AppThemeMode DetectThemeMode()
    {
        try
        {
            var registryValue = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                0);

            return registryValue is int intValue && intValue > 0
                ? AppThemeMode.Light
                : AppThemeMode.Dark;
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            return AppThemeMode.Dark;
        }
    }
}

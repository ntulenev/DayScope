using System.Windows;

namespace DayScope.Platform;

/// <summary>
/// Applies platform-specific window chrome behavior.
/// </summary>
public interface IWindowChromeController
{
    /// <summary>
    /// Applies the title-bar theme for the provided window.
    /// </summary>
    /// <param name="window">The target window.</param>
    /// <param name="useDarkChrome">Whether dark title-bar chrome should be enabled.</param>
    void ApplyTitleBarTheme(Window window, bool useDarkChrome);
}

using System.ComponentModel;
using System.Windows;

using DayScope.Domain.Configuration;

namespace DayScope.Views;

/// <summary>
/// Handles window-shell lifecycle concerns for the main dashboard window.
/// </summary>
internal sealed class MainWindowShellController
{
    /// <summary>
    /// Applies configured size constraints to the window.
    /// </summary>
    /// <param name="window">The window to configure.</param>
    /// <param name="settings">The configured window settings.</param>
    public static void ApplyWindowSettings(Window window, WindowSettings settings)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(settings);

        window.Width = settings.Width;
        window.Height = settings.Height;
        window.MinWidth = settings.MinWidth;
        window.MinHeight = settings.MinHeight;
    }

    /// <summary>
    /// Shows the window from the tray and invokes the provided callback after it is visible.
    /// </summary>
    /// <param name="window">The window to show.</param>
    /// <param name="onShown">The callback invoked after the window is restored.</param>
    public static void ShowFromTray(Window window, Action onShown)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(onShown);

        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();

        onShown();
    }

    /// <summary>
    /// Hides the window to the system tray.
    /// </summary>
    /// <param name="window">The window to hide.</param>
    public static void HideToTray(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.Hide();
    }

    /// <summary>
    /// Requests a real close instead of minimizing the window to the tray.
    /// </summary>
    /// <param name="window">The window to close.</param>
    public void CloseFromTray(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _allowClose = true;
        window.Close();
    }

    /// <summary>
    /// Intercepts the closing event and hides the window unless tray-driven close was requested.
    /// </summary>
    /// <param name="window">The window that is closing.</param>
    /// <param name="e">The closing event arguments.</param>
    public void HandleClosing(Window window, CancelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(e);

        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        HideToTray(window);
    }

    private bool _allowClose;
}

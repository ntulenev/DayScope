using System.Windows;

using DayScope.Platform;
using DayScope.Themes;

namespace DayScope.Views;

/// <summary>
/// Applies native title-bar theming for the main window and tracks theme changes.
/// </summary>
internal sealed class MainWindowThemeController
{
    private readonly ThemeManager _themeManager;
    private readonly IWindowChromeController _windowChromeController;
    private Window? _window;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowThemeController"/> class.
    /// </summary>
    /// <param name="themeManager">The theme manager used to read the active theme.</param>
    /// <param name="windowChromeController">The controller used to update native window chrome.</param>
    public MainWindowThemeController(
        ThemeManager themeManager,
        IWindowChromeController windowChromeController)
    {
        ArgumentNullException.ThrowIfNull(themeManager);
        ArgumentNullException.ThrowIfNull(windowChromeController);

        _themeManager = themeManager;
        _windowChromeController = windowChromeController;
    }

    /// <summary>
    /// Starts applying theme updates to the provided window.
    /// </summary>
    /// <param name="window">The window to manage.</param>
    public void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _window = window;
        _window.SourceInitialized += OnWindowSourceInitialized;
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    /// <summary>
    /// Stops applying theme updates to the provided window.
    /// </summary>
    /// <param name="window">The window to detach.</param>
    public void Detach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        _themeManager.ThemeChanged -= OnThemeChanged;
        window.SourceInitialized -= OnWindowSourceInitialized;
        if (ReferenceEquals(_window, window))
        {
            _window = null;
        }
    }

    private void OnWindowSourceInitialized(object? sender, EventArgs e)
    {
        ApplyTitleBarTheme();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_window is null)
        {
            return;
        }

        _window.Dispatcher.Invoke(ApplyTitleBarTheme);
    }

    private void ApplyTitleBarTheme()
    {
        if (_window is null)
        {
            return;
        }

        _windowChromeController.ApplyTitleBarTheme(_window, _themeManager.IsDarkTheme);
    }
}

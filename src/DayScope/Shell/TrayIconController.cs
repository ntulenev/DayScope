using DayScope.Themes;
using DayScope.Views;

namespace DayScope.Shell;

/// <summary>
/// Owns the system-tray icon and its menu lifecycle.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrayIconController"/> class.
    /// </summary>
    /// <param name="mainWindow">The main window controlled by the tray icon.</param>
    /// <param name="themeManager">The theme manager used to switch themes from the tray menu.</param>
    public TrayIconController(
        MainWindow mainWindow,
        ThemeManager themeManager)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);
        ArgumentNullException.ThrowIfNull(themeManager);

        _mainWindow = mainWindow;
        _themeManager = themeManager;
    }

    /// <summary>
    /// Creates and shows the tray icon and its context menu.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        CreateTrayIcon();
        _themeManager.ThemeChanged += OnThemeChanged;
        UpdateThemeMenuSelection();
        _isInitialized = true;
    }

    /// <summary>
    /// Disposes the tray icon and unregisters menu-related event handlers.
    /// </summary>
    public void Dispose()
    {
        if (!_isInitialized)
        {
            return;
        }

        _themeManager.ThemeChanged -= OnThemeChanged;

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _contextMenu?.Dispose();
        _contextMenu = null;
        _themeMenuController = null;

        _isInitialized = false;
    }

    private void CreateTrayIcon()
    {
        var (menu, themeMenuController) = TrayMenuBuilder.Build(
            () => _mainWindow.Dispatcher.Invoke(ShowMainWindow),
            () => _mainWindow.Dispatcher.Invoke(BeginRefreshFromTray),
            () => _mainWindow.Dispatcher.Invoke(CopyScheduleFromTray),
            themeMode => _mainWindow.Dispatcher.Invoke(() => SetThemeMode(themeMode)),
            () => _mainWindow.Dispatcher.Invoke(ExitFromTray));
        _contextMenu = menu;
        _themeMenuController = themeMenuController;

        _trayIcon = new NotifyIcon
        {
            Text = APP_NAME,
            Visible = true,
            ContextMenuStrip = menu,
            Icon = ResolveTrayIcon()
        };
        _trayIcon.DoubleClick += (_, _) => _mainWindow.Dispatcher.Invoke(
            ToggleMainWindowVisibility);
    }

    private void ShowMainWindow()
    {
        _mainWindow.ShowFromTray();
    }

    private void BeginRefreshFromTray()
    {
        _ = RefreshFromTrayAsync();
    }

    private void CopyScheduleFromTray()
    {
        _ = _mainWindow.CopyScheduleToClipboard();
    }

    private async Task RefreshFromTrayAsync()
    {
        await _mainWindow.RefreshNowAsync();
    }

    private void ToggleMainWindowVisibility()
    {
        if (_mainWindow.IsVisible)
        {
            _mainWindow.HideToTray();
            return;
        }

        _mainWindow.ShowFromTray();
    }

    private void ExitFromTray()
    {
        _mainWindow.CloseFromTray();
        System.Windows.Application.Current?.Shutdown();
    }

    private void SetThemeMode(AppThemeMode themeMode)
    {
        _themeManager.SetThemeMode(themeMode);
        UpdateThemeMenuSelection();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        _mainWindow.Dispatcher.Invoke(UpdateThemeMenuSelection);
    }

    private void UpdateThemeMenuSelection()
    {
        _themeMenuController?.UpdateSelection(_themeManager.SelectedMode);
    }

    private static System.Drawing.Icon ResolveTrayIcon()
    {
        return !string.IsNullOrWhiteSpace(Environment.ProcessPath)
            ? System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath)
                ?? System.Drawing.SystemIcons.Application
            : System.Drawing.SystemIcons.Application;
    }

    private const string APP_NAME = "DayScope";
    private readonly MainWindow _mainWindow;
    private readonly ThemeManager _themeManager;
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _contextMenu;
    private TrayThemeMenuController? _themeMenuController;
    private bool _isInitialized;
}

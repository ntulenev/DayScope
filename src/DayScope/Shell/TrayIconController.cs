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

        DisposeMenuItem(ref _themeOsMenuItem);
        DisposeMenuItem(ref _themeLightMenuItem);
        DisposeMenuItem(ref _themeDarkMenuItem);
        DisposeMenuItem(ref _themeForestMenuItem);
        DisposeMenuItem(ref _themeAutumnMenuItem);
        DisposeMenuItem(ref _themeDarkPinkMenuItem);
        DisposeMenuItem(ref _themeMatrixMenuItem);

        _isInitialized = false;
    }

    private void CreateTrayIcon()
    {
        var openItem = new ToolStripMenuItem("Open");
        openItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(ShowMainWindow);

        var refreshItem = new ToolStripMenuItem("Refresh now");
        refreshItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(BeginRefreshFromTray);

        _themeOsMenuItem = new ToolStripMenuItem("OS");
        _themeOsMenuItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(
            () => SetThemeMode(AppThemeMode.Os));

        _themeLightMenuItem = new ToolStripMenuItem("Light");
        _themeLightMenuItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(
            () => SetThemeMode(AppThemeMode.Light));

        _themeDarkMenuItem = new ToolStripMenuItem("Dark");
        _themeDarkMenuItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(
            () => SetThemeMode(AppThemeMode.Dark));

        _themeForestMenuItem = new ToolStripMenuItem("Forest");
        _themeForestMenuItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(
            () => SetThemeMode(AppThemeMode.Forest));

        _themeAutumnMenuItem = new ToolStripMenuItem("Autumn");
        _themeAutumnMenuItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(
            () => SetThemeMode(AppThemeMode.Autumn));

        _themeDarkPinkMenuItem = new ToolStripMenuItem("Dark Pink");
        _themeDarkPinkMenuItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(
            () => SetThemeMode(AppThemeMode.DarkPink));

        _themeMatrixMenuItem = new ToolStripMenuItem("Matrix");
        _themeMatrixMenuItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(
            () => SetThemeMode(AppThemeMode.Matrix));

        var themeMenuItem = new ToolStripMenuItem("Theme");
        themeMenuItem.DropDownItems.Add(_themeOsMenuItem);
        themeMenuItem.DropDownItems.Add(_themeLightMenuItem);
        themeMenuItem.DropDownItems.Add(_themeDarkMenuItem);
        themeMenuItem.DropDownItems.Add(_themeForestMenuItem);
        themeMenuItem.DropDownItems.Add(_themeAutumnMenuItem);
        themeMenuItem.DropDownItems.Add(_themeDarkPinkMenuItem);
        themeMenuItem.DropDownItems.Add(_themeMatrixMenuItem);

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => _mainWindow.Dispatcher.Invoke(ExitFromTray);

        var menu = new ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(refreshItem);
        menu.Items.Add(themeMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

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
        if (_themeOsMenuItem is null ||
            _themeLightMenuItem is null ||
            _themeDarkMenuItem is null ||
            _themeForestMenuItem is null ||
            _themeAutumnMenuItem is null ||
            _themeDarkPinkMenuItem is null ||
            _themeMatrixMenuItem is null)
        {
            return;
        }

        _themeOsMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Os;
        _themeLightMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Light;
        _themeDarkMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Dark;
        _themeForestMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Forest;
        _themeAutumnMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Autumn;
        _themeDarkPinkMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.DarkPink;
        _themeMatrixMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Matrix;
    }

    private static void DisposeMenuItem(ref ToolStripMenuItem? menuItem)
    {
        menuItem?.Dispose();
        menuItem = null;
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
    private ToolStripMenuItem? _themeOsMenuItem;
    private ToolStripMenuItem? _themeLightMenuItem;
    private ToolStripMenuItem? _themeDarkMenuItem;
    private ToolStripMenuItem? _themeForestMenuItem;
    private ToolStripMenuItem? _themeAutumnMenuItem;
    private ToolStripMenuItem? _themeDarkPinkMenuItem;
    private ToolStripMenuItem? _themeMatrixMenuItem;
    private bool _isInitialized;
}

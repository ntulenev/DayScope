using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DayScope.Application.DependencyInjection;
using DayScope.DependencyInjection;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;
using DayScope.Themes;
using DayScope.Views;

namespace DayScope;

/// <summary>
/// Bootstraps the WPF application, host container, and tray integration.
/// </summary>
public partial class App : System.Windows.Application
{
    /// <summary>
    /// Starts the host, resolves the main window, and initializes tray integration.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override async void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = CreateHost();
        await _host.StartAsync();

        _themeManager = _host.Services.GetRequiredService<ThemeManager>();
        _themeManager.Initialize();
        _mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = _mainWindow;

        CreateTrayIcon();
        _mainWindow.ShowFromTray();
        await _mainWindow.InitializeAsync();
    }

    /// <summary>
    /// Disposes tray and host resources when the application exits.
    /// </summary>
    /// <param name="e">The exit event arguments.</param>
    protected override void OnExit(ExitEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        if (_themeManager is not null)
        {
            _themeManager.ThemeChanged -= OnThemeChanged;
            _themeManager.Dispose();
            _themeManager = null;
        }

        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    /// <summary>
    /// Creates and configures the application host.
    /// </summary>
    /// <returns>The configured application host.</returns>
    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        builder.Services.AddDayScopeApplication();
        builder.Services.AddDayScopeInfrastructure(builder.Configuration);
        var useDemoInfrastructure =
            builder.Configuration.GetSection("DemoMode").Get<DemoModeSettings>()?.Enabled is true;
        if (useDemoInfrastructure)
        {
            builder.Services.AddDayScopeDemoInfrastructure();
        }
        else
        {
            builder.Services.AddDayScopeGoogleInfrastructure();
        }

        builder.Services.AddDayScopePresentation();

        return builder.Build();
    }

    /// <summary>
    /// Creates the notify icon and tray menu.
    /// </summary>
    private void CreateTrayIcon()
    {
        var openItem = new System.Windows.Forms.ToolStripMenuItem("Open");
        openItem.Click += (_, _) => Dispatcher.Invoke(ShowMainWindow);

        var refreshItem = new System.Windows.Forms.ToolStripMenuItem("Refresh now");
        refreshItem.Click += (_, _) => Dispatcher.InvokeAsync(RefreshFromTrayAsync);

        _themeOsMenuItem = new System.Windows.Forms.ToolStripMenuItem("OS");
        _themeOsMenuItem.Click += (_, _) => Dispatcher.Invoke(() => SetThemeMode(AppThemeMode.Os));

        _themeLightMenuItem = new System.Windows.Forms.ToolStripMenuItem("Light");
        _themeLightMenuItem.Click += (_, _) => Dispatcher.Invoke(() => SetThemeMode(AppThemeMode.Light));

        _themeDarkMenuItem = new System.Windows.Forms.ToolStripMenuItem("Dark");
        _themeDarkMenuItem.Click += (_, _) => Dispatcher.Invoke(() => SetThemeMode(AppThemeMode.Dark));

        _themeForestMenuItem = new System.Windows.Forms.ToolStripMenuItem("Forest");
        _themeForestMenuItem.Click += (_, _) => Dispatcher.Invoke(() => SetThemeMode(AppThemeMode.Forest));

        _themeAutumnMenuItem = new System.Windows.Forms.ToolStripMenuItem("Autumn");
        _themeAutumnMenuItem.Click += (_, _) => Dispatcher.Invoke(() => SetThemeMode(AppThemeMode.Autumn));

        _themeDarkPinkMenuItem = new System.Windows.Forms.ToolStripMenuItem("Dark Pink");
        _themeDarkPinkMenuItem.Click += (_, _) => Dispatcher.Invoke(() => SetThemeMode(AppThemeMode.DarkPink));

        var themeMenuItem = new System.Windows.Forms.ToolStripMenuItem("Theme");
        themeMenuItem.DropDownItems.Add(_themeOsMenuItem);
        themeMenuItem.DropDownItems.Add(_themeLightMenuItem);
        themeMenuItem.DropDownItems.Add(_themeDarkMenuItem);
        themeMenuItem.DropDownItems.Add(_themeForestMenuItem);
        themeMenuItem.DropDownItems.Add(_themeAutumnMenuItem);
        themeMenuItem.DropDownItems.Add(_themeDarkPinkMenuItem);

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Dispatcher.Invoke(ExitFromTray);

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(refreshItem);
        menu.Items.Add(themeMenuItem);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = APP_NAME,
            Visible = true,
            ContextMenuStrip = menu,
            Icon = ResolveTrayIcon()
        };

        _themeManager?.ThemeChanged += OnThemeChanged;
        UpdateThemeMenuSelection();
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ToggleMainWindowVisibility);
    }

    /// <summary>
    /// Shows the main window if it has been created.
    /// </summary>
    private void ShowMainWindow()
    {
        _mainWindow?.ShowFromTray();
    }

    /// <summary>
    /// Triggers a manual refresh from the tray menu.
    /// </summary>
    /// <returns>A task that completes when the refresh finishes.</returns>
    private async Task RefreshFromTrayAsync()
    {
        if (_mainWindow is null)
        {
            return;
        }

        await _mainWindow.RefreshNowAsync();
    }

    /// <summary>
    /// Toggles the main window between visible and hidden states.
    /// </summary>
    private void ToggleMainWindowVisibility()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible)
        {
            _mainWindow.HideToTray();
            return;
        }

        _mainWindow.ShowFromTray();
    }

    /// <summary>
    /// Closes the application from the tray menu.
    /// </summary>
    private void ExitFromTray()
    {
        _mainWindow?.CloseFromTray();

        Shutdown();
    }

    /// <summary>
    /// Updates the selected theme mode through the theme manager.
    /// </summary>
    /// <param name="themeMode">The theme mode to apply.</param>
    private void SetThemeMode(AppThemeMode themeMode)
    {
        if (_themeManager is null)
        {
            return;
        }

        _themeManager.SetThemeMode(themeMode);
        UpdateThemeMenuSelection();
    }

    /// <summary>
    /// Refreshes tray menu checks when the theme changes.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnThemeChanged(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(UpdateThemeMenuSelection);
    }

    /// <summary>
    /// Synchronizes tray menu checkmarks with the current theme selection.
    /// </summary>
    private void UpdateThemeMenuSelection()
    {
        if (_themeManager is null ||
            _themeOsMenuItem is null ||
            _themeLightMenuItem is null ||
            _themeDarkMenuItem is null ||
            _themeForestMenuItem is null ||
            _themeAutumnMenuItem is null ||
            _themeDarkPinkMenuItem is null)
        {
            return;
        }

        _themeOsMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Os;
        _themeLightMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Light;
        _themeDarkMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Dark;
        _themeForestMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Forest;
        _themeAutumnMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.Autumn;
        _themeDarkPinkMenuItem.Checked = _themeManager.SelectedMode == AppThemeMode.DarkPink;
    }

    /// <summary>
    /// Resolves the icon shown in the system tray.
    /// </summary>
    /// <returns>The tray icon to display.</returns>
    private static System.Drawing.Icon ResolveTrayIcon()
    {
        return !string.IsNullOrWhiteSpace(Environment.ProcessPath)
            ? System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath)
                ?? System.Drawing.SystemIcons.Application
            : System.Drawing.SystemIcons.Application;
    }

    private const string APP_NAME = "DayScope";
    private IHost? _host;
    private MainWindow? _mainWindow;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private ThemeManager? _themeManager;
    private System.Windows.Forms.ToolStripMenuItem? _themeOsMenuItem;
    private System.Windows.Forms.ToolStripMenuItem? _themeLightMenuItem;
    private System.Windows.Forms.ToolStripMenuItem? _themeDarkMenuItem;
    private System.Windows.Forms.ToolStripMenuItem? _themeForestMenuItem;
    private System.Windows.Forms.ToolStripMenuItem? _themeAutumnMenuItem;
    private System.Windows.Forms.ToolStripMenuItem? _themeDarkPinkMenuItem;
}

using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DayScope.Application.DependencyInjection;
using DayScope.DependencyInjection;
using DayScope.Infrastructure.Configuration;
using DayScope.Views;

namespace DayScope;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = CreateHost();
        await _host.StartAsync();

        _mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = _mainWindow;

        CreateTrayIcon();
        _mainWindow.ShowFromTray();
        await _mainWindow.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        if (_host is not null)
        {
            _host.StopAsync().GetAwaiter().GetResult();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        builder.Services.AddDayScopeApplication();
        builder.Services.AddDayScopeInfrastructure(builder.Configuration);
        builder.Services.AddDayScopePresentation();

        return builder.Build();
    }

    private void CreateTrayIcon()
    {
        var openItem = new System.Windows.Forms.ToolStripMenuItem("Open");
        openItem.Click += (_, _) => Dispatcher.Invoke(ShowMainWindow);

        var refreshItem = new System.Windows.Forms.ToolStripMenuItem("Refresh now");
        refreshItem.Click += (_, _) => Dispatcher.InvokeAsync(RefreshFromTrayAsync);

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => Dispatcher.Invoke(ExitFromTray);

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add(openItem);
        menu.Items.Add(refreshItem);
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);

        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = APP_NAME,
            Visible = true,
            ContextMenuStrip = menu,
            Icon = ResolveTrayIcon()
        };

        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ToggleMainWindowVisibility);
    }

    private void ShowMainWindow()
    {
        _mainWindow?.ShowFromTray();
    }

    private async Task RefreshFromTrayAsync()
    {
        if (_mainWindow is null)
        {
            return;
        }

        await _mainWindow.RefreshNowAsync();
    }

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

    private void ExitFromTray()
    {
        _mainWindow?.CloseFromTray();

        Shutdown();
    }

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
}

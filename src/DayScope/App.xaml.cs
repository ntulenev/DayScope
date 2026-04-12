using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using DayScope.Application.DependencyInjection;
using DayScope.DependencyInjection;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;
using DayScope.Shell;
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
    protected async override void OnStartup(StartupEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = CreateHost();
        await _host.StartAsync();

        _themeManager = _host.Services.GetRequiredService<ThemeManager>();
        _themeManager.Initialize();
        _mainWindow = _host.Services.GetRequiredService<MainWindow>();
        _trayIconController = _host.Services.GetRequiredService<TrayIconController>();
        MainWindow = _mainWindow;

        _trayIconController.Initialize();
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

        _trayIconController?.Dispose();
        _trayIconController = null;

        _themeManager?.Dispose();
        _themeManager = null;

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

    private IHost? _host;
    private MainWindow? _mainWindow;
    private TrayIconController? _trayIconController;
    private ThemeManager? _themeManager;
}

using Microsoft.Extensions.DependencyInjection;

using DayScope.Platform;
using DayScope.Shell;
using DayScope.Themes;
using DayScope.Threading;
using DayScope.ViewModels;
using DayScope.Views;

namespace DayScope.DependencyInjection;

/// <summary>
/// Registers presentation-layer services for the WPF application.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds view, view model, theme, and UI timer services used by the presentation layer.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDayScopePresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IThemePreferenceStore, ThemePreferenceStore>();
        services.AddSingleton<IOsThemeDetector, WindowsOsThemeDetector>();
        services.AddSingleton<IThemeResourceApplier, ApplicationThemeResourceApplier>();
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<TrayIconController>();
        services.AddSingleton<IUriLauncher, ShellUriLauncher>();
        services.AddSingleton<IClipboardService, WpfClipboardService>();
        services.AddSingleton<IWindowChromeController, WindowChromeController>();
        services.AddSingleton<IUiDispatcherTimerFactory, DispatcherTimerFactory>();
        services.AddSingleton<MainWindowDashboardCoordinator>();
        services.AddSingleton<MainWindowInboxState>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

using DayScope.Themes;
using DayScope.ViewModels;
using DayScope.Views;

namespace DayScope.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDayScopePresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ThemePreferenceStore>();
        services.AddSingleton<ThemeManager>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services;
    }
}

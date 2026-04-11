using Microsoft.Extensions.DependencyInjection;

using DayScope.Application.Abstractions;
using DayScope.Application.Dashboard;
using DayScope.Application.Google;

namespace DayScope.Application.DependencyInjection;

/// <summary>
/// Registers application-layer services.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services used by the dashboard.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDayScopeApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<IGoogleWorkspaceUriBuilder, GoogleWorkspaceUriBuilder>()
            .AddSingleton<IDayScheduleDashboardService, DayScheduleDashboardService>();
    }
}

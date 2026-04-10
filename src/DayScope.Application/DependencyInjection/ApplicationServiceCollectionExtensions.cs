using Microsoft.Extensions.DependencyInjection;

using DayScope.Application.Dashboard;

namespace DayScope.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDayScopeApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .AddSingleton<DayScheduleDashboardService>();
    }
}

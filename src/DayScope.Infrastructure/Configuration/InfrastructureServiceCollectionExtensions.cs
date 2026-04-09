using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Calendar;
using DayScope.Infrastructure.Clock;

namespace DayScope.Infrastructure.Configuration;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddDayScopeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IPostConfigureOptions<DayScheduleSettings>, DayScheduleSettingsConfiguration>();
        services.AddSingleton<IValidateOptions<DayScheduleSettings>, DayScheduleSettingsConfiguration>();
        services.AddSingleton<IPostConfigureOptions<GoogleCalendarSettings>, GoogleCalendarSettingsConfiguration>();
        services.AddSingleton<IValidateOptions<GoogleCalendarSettings>, GoogleCalendarSettingsConfiguration>();
        services.AddOptions<WindowSettings>()
            .Bind(configuration.GetSection("Window"))
            .ValidateOnStart();

        services.AddOptions<DayScheduleSettings>()
            .Bind(configuration.GetSection("DaySchedule"))
            .ValidateOnStart();
        services.AddOptions<GoogleCalendarSettings>()
            .Bind(configuration.GetSection("GoogleCalendar"))
            .ValidateOnStart();

        services.AddSingleton<IClockService, SystemClockService>();
        services.AddSingleton<ICalendarService, GoogleCalendarService>();

        return services;
    }
}

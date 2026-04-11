using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Calendar;
using DayScope.Infrastructure.Clock;
using DayScope.Infrastructure.Demo;
using DayScope.Infrastructure.Google;
using DayScope.Infrastructure.Mail;

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

        services.AddOptions<DemoModeSettings>()
            .Bind(configuration.GetSection("DemoMode"))
            .ValidateOnStart();
        services.AddOptions<DayScheduleSettings>()
            .Bind(configuration.GetSection("DaySchedule"))
            .ValidateOnStart();
        services.AddOptions<GoogleCalendarSettings>()
            .Bind(configuration.GetSection("GoogleCalendar"))
            .ValidateOnStart();

        services.AddSingleton<IClockService, SystemClockService>();

        var demoModeSettings = configuration
            .GetSection("DemoMode")
            .Get<DemoModeSettings>() ?? new DemoModeSettings();

        if (demoModeSettings.Enabled)
        {
            services.AddSingleton<ICalendarService, DemoCalendarService>();
            services.AddSingleton<IEmailInboxService, DemoEmailInboxService>();
        }
        else
        {
            services.AddSingleton<GoogleCredentialProvider>();
            services.AddSingleton<ICalendarService, GoogleCalendarService>();
            services.AddSingleton<IEmailInboxService, GoogleMailInboxService>();
        }

        return services;
    }
}

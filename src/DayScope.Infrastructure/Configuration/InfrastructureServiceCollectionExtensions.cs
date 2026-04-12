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

/// <summary>
/// Registers infrastructure-layer services and configuration.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Adds shared infrastructure services and option configuration.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="configuration">The application configuration root.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDayScopeInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<DayScheduleSettingsConfiguration>();
        services.AddSingleton<IPostConfigureOptions<DayScheduleSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<DayScheduleSettingsConfiguration>());
        services.AddSingleton<IValidateOptions<DayScheduleSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<DayScheduleSettingsConfiguration>());

        services.AddSingleton<GoogleCalendarSettingsConfiguration>();
        services.AddSingleton<IPostConfigureOptions<GoogleCalendarSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<GoogleCalendarSettingsConfiguration>());
        services.AddSingleton<IValidateOptions<GoogleCalendarSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<GoogleCalendarSettingsConfiguration>());

        services.AddSingleton<WindowSettingsConfiguration>();
        services.AddSingleton<IPostConfigureOptions<WindowSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<WindowSettingsConfiguration>());
        services.AddSingleton<IValidateOptions<WindowSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<WindowSettingsConfiguration>());

        services.AddSingleton<DemoModeSettingsConfiguration>();
        services.AddSingleton<IPostConfigureOptions<DemoModeSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<DemoModeSettingsConfiguration>());
        services.AddSingleton<IValidateOptions<DemoModeSettings>>(serviceProvider =>
            serviceProvider.GetRequiredService<DemoModeSettingsConfiguration>());

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

        services.AddSingleton<IPathResolver, PathResolver>();
        services.AddSingleton<IClockService, SystemClockService>();
        services.AddSingleton<ILocalTimeZoneProvider, SystemTimeZoneProvider>();

        return services;
    }

    /// <summary>
    /// Adds demo implementations for calendar and inbox services.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDayScopeDemoInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ICalendarService, DemoCalendarService>();
        services.AddSingleton<IEmailInboxService, DemoEmailInboxService>();

        return services;
    }

    /// <summary>
    /// Adds Google-backed implementations for calendar and inbox services.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddDayScopeGoogleInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IGoogleClientSecretsLoader, GoogleClientSecretsLoader>();
        services.AddSingleton<IGoogleTokenStoreDirectoryProvider, GoogleTokenStoreDirectoryProvider>();
        services.AddSingleton<IGoogleAuthorizationCodeFlowFactory, GoogleAuthorizationCodeFlowFactory>();
        services.AddSingleton<IGoogleStoredCredentialLoader, GoogleStoredCredentialLoader>();
        services.AddSingleton<IGoogleInteractiveCredentialAuthorizer, GoogleInstalledAppAuthorizer>();
        services.AddSingleton<IGoogleApiClientFactory, GoogleApiClientFactory>();
        services.AddSingleton<IGoogleCredentialProvider, GoogleCredentialProvider>();
        services.AddSingleton<IGoogleCalendarGateway, GoogleCalendarGateway>();
        services.AddSingleton<IGoogleCalendarEventMapper, GoogleCalendarEventMapper>();
        services.AddSingleton<IGoogleCalendarFailureMapper, GoogleCalendarFailureMapper>();
        services.AddSingleton<IGoogleMailInboxGateway, GoogleMailInboxGateway>();
        services.AddSingleton<ICalendarService, GoogleCalendarService>();
        services.AddSingleton<IEmailInboxService, GoogleMailInboxService>();

        return services;
    }
}

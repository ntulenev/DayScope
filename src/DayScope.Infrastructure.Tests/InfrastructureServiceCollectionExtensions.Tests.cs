using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Calendar;
using DayScope.Infrastructure.Clock;
using DayScope.Infrastructure.Configuration;
using DayScope.Infrastructure.Demo;
using DayScope.Infrastructure.Google;
using DayScope.Infrastructure.Mail;

namespace DayScope.Infrastructure.Tests;

public sealed class InfrastructureServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Shared infrastructure registration adds expected options and singleton services.")]
    [Trait("Category", "Unit")]
    public void AddDayScopeInfrastructureShouldRegisterExpectedOptionsAndSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        // Act
        services.AddDayScopeInfrastructure(configuration);

        // Assert
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IPathResolver) &&
            descriptor.ImplementationType == typeof(PathResolver) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IClockService) &&
            descriptor.ImplementationType == typeof(SystemClockService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ILocalTimeZoneProvider) &&
            descriptor.ImplementationType == typeof(SystemTimeZoneProvider) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPostConfigureOptions<DayScheduleSettings>));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IValidateOptions<DayScheduleSettings>));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IPostConfigureOptions<GoogleCalendarSettings>));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IValidateOptions<GoogleCalendarSettings>));
    }

    [Fact(DisplayName = "Demo infrastructure registration adds demo calendar and inbox services.")]
    [Trait("Category", "Unit")]
    public void AddDayScopeDemoInfrastructureShouldRegisterExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDayScopeDemoInfrastructure();

        // Assert
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IDemoAgendaFactory) &&
            descriptor.ImplementationType == typeof(DemoAgendaFactory));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICalendarService) &&
            descriptor.ImplementationType == typeof(DemoCalendarService));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IEmailInboxService) &&
            descriptor.ImplementationType == typeof(DemoEmailInboxService));
    }

    [Fact(DisplayName = "Google infrastructure registration adds Google-backed services.")]
    [Trait("Category", "Unit")]
    public void AddDayScopeGoogleInfrastructureShouldRegisterExpectedServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDayScopeGoogleInfrastructure();

        // Assert
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IGoogleCredentialProvider) &&
            descriptor.ImplementationType == typeof(GoogleCredentialProvider));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IGoogleCalendarGateway) &&
            descriptor.ImplementationType == typeof(GoogleCalendarGateway));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IGoogleMailInboxGateway) &&
            descriptor.ImplementationType == typeof(GoogleMailInboxGateway));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ICalendarService) &&
            descriptor.ImplementationType == typeof(GoogleCalendarService));
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IEmailInboxService) &&
            descriptor.ImplementationType == typeof(GoogleMailInboxService));
    }
}

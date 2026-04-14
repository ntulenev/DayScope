using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using DayScope.Application.Abstractions;
using DayScope.Application.Dashboard;
using DayScope.Application.DependencyInjection;
using DayScope.Application.Google;

namespace DayScope.Application.Tests;

public sealed class ApplicationServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Application service registration adds the expected singleton services.")]
    [Trait("Category", "Unit")]
    public void AddDayScopeApplicationShouldRegisterExpectedSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDayScopeApplication();

        // Assert
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IGoogleWorkspaceUriBuilder) &&
            descriptor.ImplementationType == typeof(GoogleWorkspaceUriBuilder) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IDayScheduleDashboardService) &&
            descriptor.ImplementationType == typeof(DayScheduleDashboardService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using DayScope.DependencyInjection;
using DayScope.Platform;
using DayScope.Shell;
using DayScope.Themes;
using DayScope.Threading;
using DayScope.ViewModels;
using DayScope.Views;

namespace DayScope.Tests;

public sealed class PresentationServiceCollectionExtensionsTests
{
    [Fact(DisplayName = "Presentation service registration adds expected singleton services.")]
    [Trait("Category", "Unit")]
    public void AddDayScopePresentationShouldRegisterExpectedSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddDayScopePresentation();

        // Assert
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IThemePreferenceStore) &&
            descriptor.ImplementationType == typeof(ThemePreferenceStore) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IOsThemeDetector) &&
            descriptor.ImplementationType == typeof(WindowsOsThemeDetector) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IThemeResourceApplier) &&
            descriptor.ImplementationType == typeof(ApplicationThemeResourceApplier) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IUriLauncher) &&
            descriptor.ImplementationType == typeof(ShellUriLauncher) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IClipboardService) &&
            descriptor.ImplementationType == typeof(WpfClipboardService) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IWindowChromeController) &&
            descriptor.ImplementationType == typeof(WindowChromeController) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IUiDispatcherTimerFactory) &&
            descriptor.ImplementationType == typeof(DispatcherTimerFactory) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(TrayIconController) &&
            descriptor.ImplementationType == typeof(TrayIconController) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(MainWindowDashboardCoordinator) &&
            descriptor.ImplementationType == typeof(MainWindowDashboardCoordinator) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(MainWindowInboxState) &&
            descriptor.ImplementationType == typeof(MainWindowInboxState) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(MainWindowViewModel) &&
            descriptor.ImplementationType == typeof(MainWindowViewModel) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(MainWindow) &&
            descriptor.ImplementationType == typeof(MainWindow) &&
            descriptor.Lifetime == ServiceLifetime.Singleton);
    }
}

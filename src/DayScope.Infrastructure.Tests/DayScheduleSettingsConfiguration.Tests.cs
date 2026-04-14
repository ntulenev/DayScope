using FluentAssertions;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;

namespace DayScope.Infrastructure.Tests;

public sealed class DayScheduleSettingsConfigurationTests
{
    [Fact(DisplayName = "PostConfigure normalizes schedule settings.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldNormalizeScheduleSettings()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 20,
            EndHour = 10,
            PrimaryTimeZoneLabel = " UTC "
        };
        var configuration = new DayScheduleSettingsConfiguration();

        // Act
        configuration.PostConfigure(Options.DefaultName, settings);

        // Assert
        settings.StartHour.Should().Be(6);
        settings.EndHour.Should().Be(20);
        settings.PrimaryTimeZoneLabel.Should().Be("UTC");
    }

    [Fact(DisplayName = "Validate returns success for valid schedule settings.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldReturnSuccessForValidScheduleSettings()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 6,
            EndHour = 20
        };
        var configuration = new DayScheduleSettingsConfiguration();

        // Act
        var result = configuration.Validate(Options.DefaultName, settings);

        // Assert
        result.Should().BeSameAs(ValidateOptionsResult.Success);
    }

    [Fact(DisplayName = "Validate returns failures for invalid schedule settings.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldReturnFailuresForInvalidScheduleSettings()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 12,
            EndHour = 12
        };
        var configuration = new DayScheduleSettingsConfiguration();

        // Act
        var result = configuration.Validate(Options.DefaultName, settings);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Be("DaySchedule:EndHour must be greater than StartHour.");
    }
}

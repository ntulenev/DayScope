using FluentAssertions;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;

namespace DayScope.Infrastructure.Tests;

public sealed class GoogleCalendarSettingsConfigurationTests
{
    [Fact(DisplayName = "PostConfigure normalizes Google Calendar settings.")]
    [Trait("Category", "Unit")]
    public void PostConfigureShouldNormalizeGoogleCalendarSettings()
    {
        // Arrange
        var settings = new GoogleCalendarSettings
        {
            CalendarId = "  ",
            RefreshMinutes = 100,
            LoginHint = " user@example.com "
        };
        var configuration = new GoogleCalendarSettingsConfiguration();

        // Act
        configuration.PostConfigure(Options.DefaultName, settings);

        // Assert
        settings.CalendarId.Should().Be("primary");
        settings.RefreshMinutes.Should().Be(60);
        settings.LoginHint.Should().Be("user@example.com");
    }

    [Fact(DisplayName = "Validate returns success for valid Google Calendar settings.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldReturnSuccessForValidGoogleCalendarSettings()
    {
        // Arrange
        var settings = new GoogleCalendarSettings
        {
            Enabled = true,
            CalendarId = "primary"
        };
        var configuration = new GoogleCalendarSettingsConfiguration();

        // Act
        var result = configuration.Validate(Options.DefaultName, settings);

        // Assert
        result.Should().BeSameAs(ValidateOptionsResult.Success);
    }

    [Fact(DisplayName = "Validate returns failures for invalid Google Calendar settings.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldReturnFailuresForInvalidGoogleCalendarSettings()
    {
        // Arrange
        var settings = new GoogleCalendarSettings
        {
            Enabled = true,
            CalendarId = " "
        };
        var configuration = new GoogleCalendarSettingsConfiguration();

        // Act
        var result = configuration.Validate(Options.DefaultName, settings);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().ContainSingle()
            .Which.Should().Be("GoogleCalendar:CalendarId must be configured.");
    }
}

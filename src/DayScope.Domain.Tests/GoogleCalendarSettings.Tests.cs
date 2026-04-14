using FluentAssertions;

using DayScope.Domain.Configuration;

namespace DayScope.Domain.Tests;

public sealed class GoogleCalendarSettingsTests
{
    [Fact(DisplayName = "Normalization restores defaults, clamps refresh minutes, and trims configured values.")]
    [Trait("Category", "Unit")]
    public void NormalizeShouldRestoreDefaultsClampRefreshMinutesAndTrimConfiguredValues()
    {
        // Arrange
        var settings = new GoogleCalendarSettings
        {
            CalendarId = "  ",
            RefreshMinutes = 100,
            ClientSecretsPath = "  secrets.json  ",
            TokenStoreDirectory = "  tokens  ",
            LoginHint = "  user@example.com  "
        };

        // Act
        settings.Normalize();

        // Assert
        settings.CalendarId.Should().Be("primary");
        settings.RefreshMinutes.Should().Be(60);
        settings.ClientSecretsPath.Should().Be("secrets.json");
        settings.TokenStoreDirectory.Should().Be("tokens");
        settings.LoginHint.Should().Be("user@example.com");
    }

    [Fact(DisplayName = "Validation reports a failure when Google Calendar is enabled without a calendar identifier.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldReportFailureWhenGoogleCalendarIsEnabledWithoutCalendarId()
    {
        // Arrange
        var settings = new GoogleCalendarSettings
        {
            Enabled = true,
            CalendarId = " "
        };

        // Act
        var failures = settings.Validate();

        // Assert
        failures.Should().ContainSingle()
            .Which.Should().Be("GoogleCalendar:CalendarId must be configured.");
    }
}

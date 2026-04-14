using FluentAssertions;

using DayScope.Domain.Configuration;

namespace DayScope.Domain.Tests;

public sealed class DayScheduleSettingsTests
{
    [Fact(DisplayName = "Normalization clamps numeric values and trims optional labels.")]
    [Trait("Category", "Unit")]
    public void NormalizeShouldClampNumericValuesAndTrimOptionalLabels()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = -3,
            EndHour = 30,
            HourHeight = 200,
            ScheduleCanvasWidth = 200,
            PrimaryTimeZoneLabel = " Local ",
            SecondaryTimeZoneId = " UTC ",
            SecondaryTimeZoneLabel = " Secondary "
        };

        // Act
        settings.Normalize();

        // Assert
        settings.StartHour.Should().Be(0);
        settings.EndHour.Should().Be(24);
        settings.HourHeight.Should().Be(160);
        settings.ScheduleCanvasWidth.Should().Be(480);
        settings.PrimaryTimeZoneLabel.Should().Be("Local");
        settings.SecondaryTimeZoneId.Should().Be("UTC");
        settings.SecondaryTimeZoneLabel.Should().Be("Secondary");
    }

    [Fact(DisplayName = "Normalization restores the default schedule bounds when the end hour does not follow the start hour.")]
    [Trait("Category", "Unit")]
    public void NormalizeShouldRestoreDefaultBoundsWhenEndHourDoesNotFollowStartHour()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 20,
            EndHour = 10
        };

        // Act
        settings.Normalize();

        // Assert
        settings.StartHour.Should().Be(6);
        settings.EndHour.Should().Be(20);
    }

    [Fact(DisplayName = "Validation reports a failure when the end hour does not follow the start hour.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldReportFailureWhenEndHourDoesNotFollowStartHour()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 12,
            EndHour = 12
        };

        // Act
        var failures = settings.Validate();

        // Assert
        failures.Should().ContainSingle()
            .Which.Should().Be("DaySchedule:EndHour must be greater than StartHour.");
    }

    [Fact(DisplayName = "Validation succeeds when the schedule bounds are valid.")]
    [Trait("Category", "Unit")]
    public void ValidateShouldSucceedWhenTheScheduleBoundsAreValid()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 6,
            EndHour = 20
        };

        // Act
        var failures = settings.Validate();

        // Assert
        failures.Should().BeEmpty();
    }

    [Fact(DisplayName = "Normalization converts blank optional labels to null.")]
    [Trait("Category", "Unit")]
    public void NormalizeShouldConvertBlankOptionalLabelsToNull()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            PrimaryTimeZoneLabel = " ",
            SecondaryTimeZoneId = " ",
            SecondaryTimeZoneLabel = " "
        };

        // Act
        settings.Normalize();

        // Assert
        settings.PrimaryTimeZoneLabel.Should().BeNull();
        settings.SecondaryTimeZoneId.Should().BeNull();
        settings.SecondaryTimeZoneLabel.Should().BeNull();
    }
}

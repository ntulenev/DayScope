using FluentAssertions;

using DayScope.Application.DaySchedule;
using DayScope.Domain.Configuration;

namespace DayScope.Application.Tests;

public sealed class DayScheduleTimelineMetricsFactoryTests
{
    [Fact(DisplayName = "Timeline metrics use the measured width and configured hour bounds when available.")]
    [Trait("Category", "Unit")]
    public void CreateShouldUseMeasuredWidthAndConfiguredHourBounds()
    {
        // Arrange
        var selectedDate = new DateOnly(2026, 4, 14);
        var settings = new DayScheduleSettings
        {
            StartHour = 6,
            EndHour = 20,
            HourHeight = 80,
            ScheduleCanvasWidth = 860
        };

        // Act
        var metrics = DayScheduleTimelineMetricsFactory.Create(
            TimeZoneInfo.Utc,
            selectedDate,
            settings,
            531.9);

        // Assert
        metrics.LabelReferenceInstant.Should().Be(new DateTimeOffset(2026, 4, 14, 12, 0, 0, TimeSpan.Zero));
        metrics.TimelineStart.Should().Be(new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero));
        metrics.TimelineEnd.Should().Be(new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero));
        metrics.TimelineHeight.Should().Be(1138);
        metrics.ScheduleWidth.Should().Be(531);
    }

    [Fact(DisplayName = "Timeline metrics enforce a minimum schedule width.")]
    [Trait("Category", "Unit")]
    public void CreateShouldEnforceMinimumScheduleWidth()
    {
        // Arrange
        var settings = new DayScheduleSettings
        {
            StartHour = 6,
            EndHour = 20,
            HourHeight = 76,
            ScheduleCanvasWidth = 300
        };

        // Act
        var metrics = DayScheduleTimelineMetricsFactory.Create(
            TimeZoneInfo.Utc,
            new DateOnly(2026, 4, 14),
            settings,
            300);

        // Assert
        metrics.ScheduleWidth.Should().Be(420);
    }
}

using FluentAssertions;

using DayScope.Application.DaySchedule;

namespace DayScope.Application.Tests;

public sealed class DayScheduleNowLineCalculatorTests
{
    [Fact(DisplayName = "The now-line is hidden when the selected date does not match the local date.")]
    [Trait("Category", "Unit")]
    public void TryCalculateShouldReturnNullWhenSelectedDateDoesNotMatchLocalDate()
    {
        // Arrange
        var localNow = new DateTimeOffset(2026, 4, 14, 9, 30, 0, TimeSpan.Zero);
        var timelineStart = new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero);
        var timelineEnd = new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero);

        // Act
        var offset = DayScheduleNowLineCalculator.TryCalculate(
            localNow,
            new DateOnly(2026, 4, 15),
            timelineStart,
            timelineEnd,
            80);

        // Assert
        offset.Should().BeNull();
    }

    [Fact(DisplayName = "The now-line is hidden when the current time falls outside the visible timeline.")]
    [Trait("Category", "Unit")]
    public void TryCalculateShouldReturnNullWhenCurrentTimeFallsOutsideTimeline()
    {
        // Arrange
        var localNow = new DateTimeOffset(2026, 4, 14, 5, 59, 0, TimeSpan.Zero);
        var timelineStart = new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero);
        var timelineEnd = new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero);

        // Act
        var offset = DayScheduleNowLineCalculator.TryCalculate(
            localNow,
            new DateOnly(2026, 4, 14),
            timelineStart,
            timelineEnd,
            80);

        // Assert
        offset.Should().BeNull();
    }

    [Fact(DisplayName = "The now-line offset is calculated from the visible timeline start.")]
    [Trait("Category", "Unit")]
    public void TryCalculateShouldReturnOffsetWhenCurrentTimeFallsInsideTimeline()
    {
        // Arrange
        var localNow = new DateTimeOffset(2026, 4, 14, 7, 30, 0, TimeSpan.Zero);
        var timelineStart = new DateTimeOffset(2026, 4, 14, 6, 0, 0, TimeSpan.Zero);
        var timelineEnd = new DateTimeOffset(2026, 4, 14, 20, 0, 0, TimeSpan.Zero);

        // Act
        var offset = DayScheduleNowLineCalculator.TryCalculate(
            localNow,
            new DateOnly(2026, 4, 14),
            timelineStart,
            timelineEnd,
            80);

        // Assert
        offset.Should().Be(120);
    }
}

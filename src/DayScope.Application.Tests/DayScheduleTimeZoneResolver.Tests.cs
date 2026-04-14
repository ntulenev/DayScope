using FluentAssertions;

using DayScope.Application.DaySchedule;

namespace DayScope.Application.Tests;

public sealed class DayScheduleTimeZoneResolverTests
{
    [Fact(DisplayName = "Blank time-zone identifiers resolve to null.")]
    [Trait("Category", "Unit")]
    public void TryResolveShouldReturnNullWhenIdentifierIsBlank()
    {
        // Arrange

        // Act
        var result = DayScheduleTimeZoneResolver.TryResolve("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Invalid time-zone identifiers resolve to null.")]
    [Trait("Category", "Unit")]
    public void TryResolveShouldReturnNullWhenIdentifierIsInvalid()
    {
        // Arrange

        // Act
        var result = DayScheduleTimeZoneResolver.TryResolve($"invalid-{Guid.NewGuid():N}");

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Existing time-zone identifiers resolve to the matching system time zone.")]
    [Trait("Category", "Unit")]
    public void TryResolveShouldReturnTimeZoneWhenIdentifierExists()
    {
        // Arrange
        var expected = TimeZoneInfo.Local;

        // Act
        var result = DayScheduleTimeZoneResolver.TryResolve(expected.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expected.Id);
    }
}

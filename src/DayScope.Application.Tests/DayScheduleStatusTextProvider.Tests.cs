using FluentAssertions;

using DayScope.Application.Calendar;
using DayScope.Application.DaySchedule;

namespace DayScope.Application.Tests;

public sealed class DayScheduleStatusTextProviderTests
{
    [Theory(DisplayName = "Status text matches the current load state and day context.")]
    [Trait("Category", "Unit")]
    [InlineData(CalendarLoadStatus.Success, true, true, "No events scheduled for today.")]
    [InlineData(CalendarLoadStatus.Success, false, true, "No events scheduled for this day.")]
    [InlineData(CalendarLoadStatus.Success, true, false, "")]
    [InlineData(CalendarLoadStatus.Loading, true, false, "Loading today's schedule...")]
    [InlineData(CalendarLoadStatus.Loading, false, false, "Loading schedule...")]
    [InlineData(CalendarLoadStatus.Disabled, true, false, "Google Calendar is disabled in appsettings.")]
    [InlineData(CalendarLoadStatus.ClientSecretsMissing, true, false, "Add Google OAuth client JSON to connect Google Calendar.")]
    [InlineData(CalendarLoadStatus.AuthorizationRequired, true, false, "Google Calendar sign-in is required to show today's schedule.")]
    [InlineData(CalendarLoadStatus.AuthorizationRequired, false, false, "Google Calendar sign-in is required to show this day's schedule.")]
    [InlineData(CalendarLoadStatus.AccessDenied, true, false, "Calendar not found or access denied.")]
    [InlineData(CalendarLoadStatus.Unavailable, true, false, "Google Calendar is unavailable right now.")]
    [InlineData(CalendarLoadStatus.NoEvents, true, false, "No events scheduled for today.")]
    [InlineData(CalendarLoadStatus.NoEvents, false, false, "No events scheduled for this day.")]
    [InlineData((CalendarLoadStatus)999, true, false, "")]
    public void GetStatusTextShouldReturnExpectedText(
        CalendarLoadStatus status,
        bool isToday,
        bool hasNoEvents,
        string expected)
    {
        // Act
        var result = DayScheduleStatusTextProvider.GetStatusText(status, isToday, hasNoEvents);

        // Assert
        result.Should().Be(expected);
    }
}

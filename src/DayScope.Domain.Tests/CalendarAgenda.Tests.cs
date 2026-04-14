using FluentAssertions;

using DayScope.Domain.Calendar;

namespace DayScope.Domain.Tests;

public sealed class CalendarAgendaTests
{
    [Fact(DisplayName = "The agenda stores an empty event collection when the source collection is null.")]
    [Trait("Category", "Unit")]
    public void CtorShouldStoreEmptyEventsWhenSourceCollectionIsNull()
    {
        // Arrange

        // Act
        var agenda = new CalendarAgenda(null);

        // Assert
        agenda.Events.Should().BeEmpty();
    }

    [Fact(DisplayName = "The agenda orders events by start time and then by effective end time.")]
    [Trait("Category", "Unit")]
    public void CtorShouldOrderEventsByStartAndEffectiveEnd()
    {
        // Arrange
        var laterEvent = CreateEvent(11, 12);
        var longerEvent = CreateEvent(9, 11);
        var earlierEndingEvent = CreateEvent(9, 10);

        // Act
        var agenda = new CalendarAgenda([laterEvent, longerEvent, earlierEndingEvent]);

        // Assert
        agenda.Events.Should().ContainInOrder(earlierEndingEvent, longerEvent, laterEvent);
    }

    private static CalendarEvent CreateEvent(int startHour, int endHour)
    {
        return new CalendarEvent(
            $"Event {startHour}",
            new DateTimeOffset(2026, 4, 14, startHour, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 14, endHour, 0, 0, TimeSpan.Zero),
            false,
            CalendarParticipationStatus.Accepted,
            CalendarEventKind.Default,
            null,
            null,
            null,
            null,
            []);
    }
}

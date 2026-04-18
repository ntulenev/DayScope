using DayScope.Application.DaySchedule;
using DayScope.ViewModels;

using FluentAssertions;

namespace DayScope.Tests;

public sealed class MainWindowScheduleStateTests
{
    [Fact(DisplayName = "Applying display state populates schedule properties, widths, and visible collections.")]
    [Trait("Category", "Unit")]
    public void ApplyShouldPopulateSchedulePropertiesWidthsAndVisibleCollections()
    {
        // Arrange
        var state = new MainWindowScheduleState();
        var displayState = CreateDisplayState(
            primaryTimeZoneLabel: "UTC+09:30",
            secondaryTimeZoneLabel: "UTC+01:00",
            nowLineTop: 144);

        // Act
        state.Apply(displayState);

        // Assert
        state.DisplayDate.Should().Be(displayState.DisplayDate);
        state.MonthTitle.Should().Be("April 2026");
        state.DayTitle.Should().Be("Tue");
        state.DayNumberText.Should().Be("14");
        state.DateText.Should().Be("Tuesday, 14 April");
        state.PrimaryTimeZoneLabel.Should().Be("UTC+09:30");
        state.SecondaryTimeZoneLabel.Should().Be("UTC+01:00");
        state.HasConfiguredSecondaryTimeZone.Should().BeTrue();
        state.ShowSecondaryTimeZone.Should().BeTrue();
        state.HasSecondaryTimeZone.Should().BeTrue();
        state.PrimaryTimeColumnWidth.Value.Should().Be(73);
        state.SecondaryTimeZoneLeadingGapWidth.Value.Should().Be(8);
        state.SecondaryTimeColumnWidth.Value.Should().Be(73);
        state.SecondaryTimeZoneTrailingGapWidth.Value.Should().Be(8);
        state.ScheduleCanvasWidth.Should().Be(860);
        state.TimelineHeight.Should().Be(1000);
        state.StatusText.Should().Be("Ready");
        state.ShowStatus.Should().BeTrue();
        state.NowLineTop.Should().Be(144);
        state.NowLineText.Should().Be("9:00AM");
        state.ShowNowLine.Should().BeTrue();
        state.PrimaryTimelineHours.Select(hour => hour.Text).Should().ContainInOrder("8:00AM", "9:00AM");
        state.SecondaryTimelineHours.Select(hour => hour.Text).Should().ContainSingle()
            .Which.Should().Be("5:00PM");
        state.AllDayEvents.Should().ContainSingle();
        state.TimedEvents.Should().ContainSingle();
    }

    [Fact(DisplayName = "Applying a new display state replaces collections and resets hidden secondary and now-line state.")]
    [Trait("Category", "Unit")]
    public void ApplyShouldReplaceCollectionsAndResetHiddenSecondaryAndNowLineState()
    {
        // Arrange
        var state = new MainWindowScheduleState();
        state.Apply(CreateDisplayState(
            primaryTimeZoneLabel: "UTC+09:30",
            secondaryTimeZoneLabel: "UTC+01:00",
            nowLineTop: 144));

        var updatedState = CreateDisplayState(
            primaryTimeZoneLabel: "UTC",
            secondaryTimeZoneLabel: null,
            nowLineTop: null,
            primaryHours: [new TimelineHourDisplayState("10:00AM", 10)],
            secondaryHours: [],
            allDayEvents:
            [
                new AllDayEventDisplayState(
                    "Offsite",
                    EventAppearance.Accepted,
                    "Confirmed",
                    string.Empty,
                    CreateDetails())
            ],
            timedEvents: []);

        // Act
        state.Apply(updatedState);

        // Assert
        state.HasConfiguredSecondaryTimeZone.Should().BeFalse();
        state.HasSecondaryTimeZone.Should().BeFalse();
        state.SecondaryTimeZoneLeadingGapWidth.Value.Should().Be(0);
        state.SecondaryTimeZoneLabel.Should().BeNull();
        state.SecondaryTimeColumnWidth.Value.Should().Be(0);
        state.SecondaryTimeZoneTrailingGapWidth.Value.Should().Be(4);
        state.NowLineTop.Should().Be(-1);
        state.ShowNowLine.Should().BeFalse();
        state.PrimaryTimeColumnWidth.Value.Should().Be(72);
        state.PrimaryTimelineHours.Should().ContainSingle();
        state.PrimaryTimelineHours[0].Text.Should().Be("10:00AM");
        state.SecondaryTimelineHours.Should().BeEmpty();
        state.AllDayEvents.Should().ContainSingle();
        state.AllDayEvents[0].Title.Should().Be("Offsite");
        state.TimedEvents.Should().BeEmpty();
    }

    [Fact(DisplayName = "Hiding the secondary time zone preserves the configured label while collapsing its layout.")]
    [Trait("Category", "Unit")]
    public void SetShowSecondaryTimeZoneShouldCollapseSecondaryTimeZoneLayout()
    {
        // Arrange
        var state = new MainWindowScheduleState();
        state.Apply(CreateDisplayState(
            primaryTimeZoneLabel: "UTC+09:30",
            secondaryTimeZoneLabel: "UTC+01:00",
            nowLineTop: 144));

        // Act
        var changed = state.SetShowSecondaryTimeZone(false);

        // Assert
        changed.Should().BeTrue();
        state.HasConfiguredSecondaryTimeZone.Should().BeTrue();
        state.ShowSecondaryTimeZone.Should().BeFalse();
        state.HasSecondaryTimeZone.Should().BeFalse();
        state.SecondaryTimeZoneLabel.Should().Be("UTC+01:00");
        state.SecondaryTimeZoneLeadingGapWidth.Value.Should().Be(0);
        state.SecondaryTimeColumnWidth.Value.Should().Be(0);
        state.SecondaryTimeZoneTrailingGapWidth.Value.Should().Be(4);
    }

    private static DayScheduleDisplayState CreateDisplayState(
        string primaryTimeZoneLabel,
        string? secondaryTimeZoneLabel,
        double? nowLineTop,
        IReadOnlyList<TimelineHourDisplayState>? primaryHours = null,
        IReadOnlyList<TimelineHourDisplayState>? secondaryHours = null,
        IReadOnlyList<AllDayEventDisplayState>? allDayEvents = null,
        IReadOnlyList<TimedEventDisplayState>? timedEvents = null)
    {
        var details = CreateDetails();
        return new DayScheduleDisplayState(
            new DateOnly(2026, 4, 14),
            "April 2026",
            "Tue",
            "14",
            "Tuesday, 14 April",
            primaryTimeZoneLabel,
            secondaryTimeZoneLabel,
            primaryHours ?? [new TimelineHourDisplayState("8:00AM", 0), new TimelineHourDisplayState("9:00AM", 76)],
            secondaryHours ?? [new TimelineHourDisplayState("5:00PM", 0)],
            allDayEvents ??
            [
                new AllDayEventDisplayState(
                    "Planning",
                    EventAppearance.Accepted,
                    "Confirmed",
                    string.Empty,
                    details)
            ],
            timedEvents ??
            [
                new TimedEventDisplayState(
                    "Standup",
                    "9:00AM-9:30AM",
                    0,
                    40,
                    0,
                    200,
                    false,
                    false,
                    true,
                    true,
                    EventAppearance.Accepted,
                    "Confirmed",
                    string.Empty,
                    details)
            ],
            860,
            1000,
            "Ready",
            true,
            nowLineTop,
            "9:00AM");
    }

    private static EventDetailsDisplayState CreateDetails()
    {
        return new EventDetailsDisplayState(
            "Standup",
            "9:00AM-9:30AM",
            EventAppearance.Accepted,
            "Confirmed",
            string.Empty,
            "Alice",
            "Notes",
            new Uri("https://meet.google.com/abc-defg-hij"),
            []);
    }
}

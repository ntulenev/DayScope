using System.Globalization;

using DayScope.Domain.Configuration;

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Builds hour labels for a rendered schedule timeline.
/// </summary>
internal static class DayScheduleTimelineLabelBuilder
{
    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");

    /// <summary>
    /// Builds the vertical timeline labels for the provided time zone.
    /// </summary>
    /// <param name="timelineStart">The first visible instant in the timeline.</param>
    /// <param name="settings">The schedule display settings.</param>
    /// <param name="zone">The time zone used to format the labels.</param>
    /// <returns>The ordered list of rendered hour labels.</returns>
    public static IReadOnlyList<TimelineHourDisplayState> Build(
        DateTimeOffset timelineStart,
        DayScheduleSettings settings,
        TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(zone);

        var hours = new List<TimelineHourDisplayState>();
        for (var hour = settings.StartHour; hour <= settings.EndHour; hour++)
        {
            var timelineHourOffset = hour - settings.StartHour;
            var instant = timelineStart.AddHours(timelineHourOffset);
            var convertedInstant = TimeZoneInfo.ConvertTime(instant, zone);
            hours.Add(new TimelineHourDisplayState(
                convertedInstant.ToString("h:mm tt", _culture)
                    .Replace(":00", string.Empty, StringComparison.Ordinal)
                    .Replace(" ", string.Empty, StringComparison.Ordinal),
                timelineHourOffset * settings.HourHeight));
        }

        return hours;
    }
}

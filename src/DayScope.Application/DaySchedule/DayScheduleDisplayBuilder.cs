using System.Globalization;

using DayScope.Application.Calendar;
using DayScope.Domain.Configuration;

namespace DayScope.Application.DaySchedule;

/// <summary>
/// Builds the display model shown by the day schedule dashboard.
/// </summary>
public static class DayScheduleDisplayBuilder
{
    /// <summary>
    /// Converts the current calendar load result into a display state for the dashboard.
    /// </summary>
    /// <param name="loadResult">The current calendar load result.</param>
    /// <param name="settings">The schedule display settings.</param>
    /// <param name="now">The current time.</param>
    /// <param name="selectedDate">The selected date to display.</param>
    /// <param name="localZone">The local time zone used for display calculations.</param>
    /// <param name="availableScheduleWidth">The available schedule width, if known.</param>
    /// <returns>The display state shown by the dashboard.</returns>
    public static DayScheduleDisplayState Build(
        CalendarLoadResult loadResult,
        DayScheduleSettings settings,
        DateTimeOffset now,
        DateOnly selectedDate,
        TimeZoneInfo localZone,
        double? availableScheduleWidth = null)
    {
        ArgumentNullException.ThrowIfNull(loadResult);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(localZone);

        var localNow = TimeZoneInfo.ConvertTime(now, localZone);
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var dateDisplay = selectedDate.ToDateTime(TimeOnly.MinValue);
        var labelReferenceInstant = CreateDateTimeOffset(localZone, selectedDate, 12);
        var timelineStart = CreateDateTimeOffset(localZone, selectedDate, settings.StartHour);
        var timelineEnd = CreateDateTimeOffset(localZone, selectedDate, settings.EndHour);
        var timelineHeight = ((settings.EndHour - settings.StartHour) * settings.HourHeight)
            + TIMELINE_BOTTOM_PADDING;
        var secondaryZone = TryResolveTimeZone(settings.SecondaryTimeZoneId);
        var scheduleWidth = ResolveScheduleWidth(settings.ScheduleCanvasWidth, availableScheduleWidth);

        var allDayEvents = loadResult.Agenda.Events
            .Where(calendarEvent => calendarEvent.IsAllDay)
            .Select(calendarEvent => DayScheduleEventPresentationFactory.CreateAllDayEvent(
                calendarEvent,
                localZone))
            .ToArray();

        var timedEvents = DayScheduleTimelineLayout.BuildTimedEvents(
            loadResult.Agenda.Events,
            timelineStart,
            timelineEnd,
            scheduleWidth,
            settings.HourHeight,
            localZone);

        var statusText = GetStatusText(loadResult.Status, selectedDate == today);
        if (loadResult.Status == CalendarLoadStatus.Success &&
            allDayEvents.Length == 0 &&
            timedEvents.Count == 0)
        {
            statusText = selectedDate == today
                ? "No events scheduled for today."
                : "No events scheduled for this day.";
        }

        return new DayScheduleDisplayState(
            selectedDate,
            dateDisplay.ToString("MMMM yyyy", _culture),
            dateDisplay.ToString("ddd", _culture),
            selectedDate.ToString("%d", _culture),
            dateDisplay.ToString("dddd, d MMMM", _culture),
            ResolveTimeZoneLabel(localZone, settings.PrimaryTimeZoneLabel, labelReferenceInstant),
            secondaryZone is null
                ? null
                : ResolveTimeZoneLabel(
                    secondaryZone,
                    settings.SecondaryTimeZoneLabel,
                    TimeZoneInfo.ConvertTime(labelReferenceInstant, secondaryZone)),
            BuildTimelineHours(timelineStart, settings, localZone),
            secondaryZone is null
                ? []
                : BuildTimelineHours(timelineStart, settings, secondaryZone),
            allDayEvents,
            timedEvents,
            scheduleWidth,
            timelineHeight,
            statusText,
            !string.IsNullOrWhiteSpace(statusText),
            TryBuildNowLine(localNow, selectedDate, timelineStart, timelineEnd, settings.HourHeight),
            localNow.ToString("h:mm tt", _culture).Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>
    /// Builds hour markers for the timeline in the requested time zone.
    /// </summary>
    /// <param name="timelineStart">The start instant of the rendered timeline.</param>
    /// <param name="settings">The schedule settings that define the visible range.</param>
    /// <param name="zone">The time zone used to label the markers.</param>
    /// <returns>The ordered timeline hour markers.</returns>
    private static List<TimelineHourDisplayState> BuildTimelineHours(
        DateTimeOffset timelineStart,
        DayScheduleSettings settings,
        TimeZoneInfo zone)
    {
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

    /// <summary>
    /// Resolves the effective schedule canvas width from configuration and layout measurements.
    /// </summary>
    /// <param name="configuredWidth">The configured fallback width.</param>
    /// <param name="availableScheduleWidth">The width measured by the view, if available.</param>
    /// <returns>The clamped schedule width used for layout.</returns>
    private static int ResolveScheduleWidth(
        int configuredWidth,
        double? availableScheduleWidth)
    {
        var width = availableScheduleWidth.HasValue
            ? (int)Math.Floor(availableScheduleWidth.Value)
            : configuredWidth;

        return Math.Max(420, width);
    }

    /// <summary>
    /// Creates a local instant for the specified date and hour in the target time zone.
    /// </summary>
    /// <param name="timeZone">The time zone that defines the offset.</param>
    /// <param name="date">The local date.</param>
    /// <param name="hour">The local hour, including values beyond 23 for next-day rollover.</param>
    /// <returns>The resulting <see cref="DateTimeOffset"/>.</returns>
    private static DateTimeOffset CreateDateTimeOffset(
        TimeZoneInfo timeZone,
        DateOnly date,
        int hour)
    {
        var dayOffset = hour / 24;
        var normalizedHour = hour % 24;
        var dateTime = date
            .AddDays(dayOffset)
            .ToDateTime(new TimeOnly(normalizedHour, 0));

        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }

    /// <summary>
    /// Calculates the vertical position of the current-time indicator when it falls inside the visible day.
    /// </summary>
    /// <param name="localNow">The current local instant.</param>
    /// <param name="selectedDate">The day being displayed.</param>
    /// <param name="timelineStart">The first visible instant.</param>
    /// <param name="timelineEnd">The last visible instant.</param>
    /// <param name="hourHeight">The rendered height of one hour block.</param>
    /// <returns>The vertical offset for the now line, or <see langword="null"/> when it should be hidden.</returns>
    private static double? TryBuildNowLine(
        DateTimeOffset localNow,
        DateOnly selectedDate,
        DateTimeOffset timelineStart,
        DateTimeOffset timelineEnd,
        int hourHeight)
    {
        if (selectedDate != DateOnly.FromDateTime(localNow.DateTime) ||
            localNow < timelineStart ||
            localNow > timelineEnd)
        {
            return null;
        }

        return (localNow - timelineStart).TotalMinutes / 60d * hourHeight;
    }

    /// <summary>
    /// Resolves the label shown for a time zone, preferring explicit configuration when present.
    /// </summary>
    /// <param name="timeZone">The time zone being labeled.</param>
    /// <param name="configuredLabel">The optional configured label override.</param>
    /// <param name="instant">The instant used to calculate the current UTC offset.</param>
    /// <returns>The label displayed in the UI.</returns>
    private static string ResolveTimeZoneLabel(
        TimeZoneInfo timeZone,
        string? configuredLabel,
        DateTimeOffset instant)
    {
        if (!string.IsNullOrWhiteSpace(configuredLabel))
        {
            return configuredLabel.Trim();
        }

        var offset = timeZone.GetUtcOffset(instant);
        var sign = offset >= TimeSpan.Zero ? "+" : "-";
        var absoluteOffset = offset.Duration();
        return $"UTC{sign}{absoluteOffset:hh\\:mm}";
    }

    /// <summary>
    /// Resolves a system time zone identifier when it is valid on the current machine.
    /// </summary>
    /// <param name="timeZoneId">The configured time zone identifier.</param>
    /// <returns>The resolved time zone, or <see langword="null"/> when the identifier is missing or invalid.</returns>
    private static TimeZoneInfo? TryResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    /// <summary>
    /// Maps the calendar load status to the status text shown in the dashboard.
    /// </summary>
    /// <param name="status">The current calendar load status.</param>
    /// <param name="isToday">Whether the selected date is the current local day.</param>
    /// <returns>The user-facing status message.</returns>
    private static string GetStatusText(
        CalendarLoadStatus status,
        bool isToday)
    {
        return status switch
        {
            CalendarLoadStatus.Loading => isToday
                ? "Loading today's schedule..."
                : "Loading schedule...",
            CalendarLoadStatus.Success => string.Empty,
            CalendarLoadStatus.Disabled => "Google Calendar is disabled in appsettings.",
            CalendarLoadStatus.ClientSecretsMissing =>
                "Add Google OAuth client JSON to connect Google Calendar.",
            CalendarLoadStatus.AuthorizationRequired =>
                isToday
                    ? "Google Calendar sign-in is required to show today's schedule."
                    : "Google Calendar sign-in is required to show this day's schedule.",
            CalendarLoadStatus.AccessDenied =>
                "Calendar not found or access denied.",
            CalendarLoadStatus.Unavailable =>
                "Google Calendar is unavailable right now.",
            CalendarLoadStatus.NoEvents => isToday
                ? "No events scheduled for today."
                : "No events scheduled for this day.",
            _ => string.Empty
        };
    }

    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");
    private const int TIMELINE_BOTTOM_PADDING = 18;
}

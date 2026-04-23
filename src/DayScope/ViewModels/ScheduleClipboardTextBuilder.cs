using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

using DayScope.Application.DaySchedule;

namespace DayScope.ViewModels;

/// <summary>
/// Builds plain-text schedule summaries for clipboard sharing.
/// </summary>
public static class ScheduleClipboardTextBuilder
{
    /// <summary>
    /// Builds a formatted text summary for the active schedule day.
    /// </summary>
    /// <param name="schedule">The schedule state to summarize.</param>
    /// <returns>The formatted clipboard text.</returns>
    public static string Build(MainWindowScheduleState schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        var builder = new StringBuilder();
        builder.AppendLine(schedule.DisplayDate.ToString("dddd, MMMM d, yyyy", _culture));

        if (!string.IsNullOrWhiteSpace(schedule.PrimaryTimeZoneLabel))
        {
            builder.Append("Time zone: ").AppendLine(schedule.PrimaryTimeZoneLabel.Trim());
        }

        if (schedule.AllDayEvents.Count == 0 && schedule.TimedEvents.Count == 0)
        {
            builder.AppendLine();
            builder.AppendLine("No events scheduled.");
            return builder.ToString().TrimEnd();
        }

        AppendAllDayEvents(builder, schedule.AllDayEvents);
        AppendTimedEvents(builder, schedule.TimedEvents);
        return builder.ToString().TrimEnd();
    }

    private static void AppendAllDayEvents(
        StringBuilder builder,
        ReadOnlyObservableCollection<AllDayEventDisplayState> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("All day");
        foreach (var scheduleEvent in events)
        {
            AppendEventLine(
                builder,
                prefix: null,
                scheduleEvent.LeadingIcon,
                scheduleEvent.Title,
                scheduleEvent.StatusLabel);
        }
    }

    private static void AppendTimedEvents(
        StringBuilder builder,
        ReadOnlyObservableCollection<TimedEventDisplayState> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("Schedule");
        foreach (var scheduleEvent in events.OrderBy(static item => item.Top).ThenBy(static item => item.Left))
        {
            AppendEventLine(
                builder,
                scheduleEvent.ScheduleText,
                scheduleEvent.LeadingIcon,
                scheduleEvent.Title,
                scheduleEvent.StatusLabel);
        }
    }

    private static void AppendEventLine(
        StringBuilder builder,
        string? prefix,
        string leadingIcon,
        string title,
        string statusLabel)
    {
        builder.Append("- ");
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            builder.Append(prefix.Trim()).Append(": ");
        }

        builder.Append(FormatTitle(leadingIcon, title));

        if (!string.IsNullOrWhiteSpace(statusLabel))
        {
            builder.Append(" (").Append(statusLabel.Trim()).Append(')');
        }

        builder.AppendLine();
    }

    private static string FormatTitle(string leadingIcon, string title)
    {
        var normalizedIcon = string.IsNullOrWhiteSpace(leadingIcon)
            ? string.Empty
            : leadingIcon.Trim();
        var normalizedTitle = string.IsNullOrWhiteSpace(title)
            ? "Untitled event"
            : title.Trim();

        return string.IsNullOrEmpty(normalizedIcon)
            ? normalizedTitle
            : string.Concat(normalizedIcon, " ", normalizedTitle);
    }

    private static readonly CultureInfo _culture = CultureInfo.GetCultureInfo("en-US");
}

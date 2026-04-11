using System.Globalization;

using DayScope.Application.Abstractions;

namespace DayScope.Application.Google;

/// <summary>
/// Builds account-aware links into Google Calendar and Gmail.
/// </summary>
public sealed class GoogleWorkspaceUriBuilder : IGoogleWorkspaceUriBuilder
{
    /// <summary>
    /// Builds a Google Calendar day view URI for the specified date.
    /// </summary>
    /// <param name="displayDate">The date to show in Calendar.</param>
    /// <param name="emailAddress">The signed-in email address, if known.</param>
    /// <returns>A Google Calendar URI.</returns>
    public Uri BuildCalendarDayUri(DateOnly displayDate, string? emailAddress)
    {
        var dayPath = string.Format(
            CultureInfo.InvariantCulture,
            "https://calendar.google.com/calendar/r/day/{0}/{1}/{2}",
            displayDate.Year,
            displayDate.Month,
            displayDate.Day);

        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return new Uri(dayPath, UriKind.Absolute);
        }

        // Inference from current Google web behavior: Calendar accepts authuser with the signed-in account email.
        return new Uri(
            string.Format(
                CultureInfo.InvariantCulture,
                "{0}?authuser={1}",
                dayPath,
                Uri.EscapeDataString(emailAddress.Trim())),
            UriKind.Absolute);
    }

    /// <summary>
    /// Builds a Gmail inbox URI for the specified account.
    /// </summary>
    /// <param name="emailAddress">The signed-in email address, if known.</param>
    /// <returns>A Gmail inbox URI.</returns>
    public Uri BuildInboxUri(string? emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return _defaultInboxUri;
        }

        // Inference from current Google web behavior: Gmail accepts authuser with the signed-in account email.
        return new Uri(
            string.Format(
                CultureInfo.InvariantCulture,
                "https://mail.google.com/mail/u/?authuser={0}#inbox",
                Uri.EscapeDataString(emailAddress.Trim())),
            UriKind.Absolute);
    }

    private static readonly Uri _defaultInboxUri = new("https://mail.google.com/mail/");
}

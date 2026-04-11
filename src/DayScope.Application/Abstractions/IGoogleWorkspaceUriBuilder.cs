namespace DayScope.Application.Abstractions;

/// <summary>
/// Builds deep links into Google Calendar and Gmail for the current account context.
/// </summary>
public interface IGoogleWorkspaceUriBuilder
{
    /// <summary>
    /// Builds a link to the Google Calendar day view.
    /// </summary>
    /// <param name="displayDate">The date to show in Calendar.</param>
    /// <param name="emailAddress">The signed-in email address, if known.</param>
    /// <returns>A Google Calendar URI.</returns>
    Uri BuildCalendarDayUri(DateOnly displayDate, string? emailAddress);

    /// <summary>
    /// Builds a link to the Gmail inbox.
    /// </summary>
    /// <param name="emailAddress">The signed-in email address, if known.</param>
    /// <returns>A Gmail inbox URI.</returns>
    Uri BuildInboxUri(string? emailAddress);
}

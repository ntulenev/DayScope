using System.Globalization;

using DayScope.Application.Abstractions;

namespace DayScope.ViewModels;

/// <summary>
/// Stores inbox counters and Google workspace links shown by the main window.
/// </summary>
public sealed class MainWindowInboxState : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowInboxState"/> class.
    /// </summary>
    /// <param name="workspaceUriBuilder">The builder used to create Gmail and Calendar links.</param>
    public MainWindowInboxState(IGoogleWorkspaceUriBuilder workspaceUriBuilder)
    {
        ArgumentNullException.ThrowIfNull(workspaceUriBuilder);

        _workspaceUriBuilder = workspaceUriBuilder;
        RebuildGoogleCalendarUri();
    }

    public int? UnreadEmailCount => _unreadEmailCount;

    public Uri UnreadEmailInboxUri => _unreadEmailInboxUri;

    public Uri GoogleCalendarUri => _googleCalendarUri;

    public string UnreadEmailCountText => _unreadEmailCount switch
    {
        null => "--",
        > 99 => "99+",
        _ => _unreadEmailCount.Value.ToString(CultureInfo.InvariantCulture)
    };

    public bool HasUnreadEmails => _unreadEmailCount is > 0;

    public string UnreadEmailSummaryText => _unreadEmailCount switch
    {
        null => "Open Gmail inbox",
        0 => "Inbox is clear",
        1 => "1 unread email",
        _ => string.Format(
            CultureInfo.InvariantCulture,
            "{0} unread emails",
            _unreadEmailCount.Value)
    };

    /// <summary>
    /// Updates the selected display date used to build the Google Calendar deep link.
    /// </summary>
    /// <param name="displayDate">The selected date displayed by the dashboard.</param>
    public void ApplyDisplayDate(DateOnly displayDate)
    {
        if (_displayDate == displayDate)
        {
            return;
        }

        _displayDate = displayDate;
        RebuildGoogleCalendarUri();
    }

    /// <summary>
    /// Updates the inbox summary from the latest unread-email snapshot.
    /// </summary>
    /// <param name="snapshot">The inbox snapshot to apply.</param>
    public void ApplySnapshot(EmailInboxSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        SetUnreadEmailCount(snapshot.UnreadCount);
        SetUnreadEmailInboxUri(snapshot.InboxUri);
        SetGoogleAccountEmail(snapshot.EmailAddress);
    }

    private void SetUnreadEmailCount(int? unreadEmailCount)
    {
        if (!SetProperty(ref _unreadEmailCount, unreadEmailCount, nameof(UnreadEmailCount)))
        {
            return;
        }

        OnPropertyChanged(nameof(UnreadEmailCountText));
        OnPropertyChanged(nameof(HasUnreadEmails));
        OnPropertyChanged(nameof(UnreadEmailSummaryText));
    }

    private void SetUnreadEmailInboxUri(Uri inboxUri)
    {
        ArgumentNullException.ThrowIfNull(inboxUri);

        if (!SetProperty(ref _unreadEmailInboxUri, inboxUri, nameof(UnreadEmailInboxUri)))
        {
            return;
        }
    }

    private void SetGoogleAccountEmail(string? emailAddress)
    {
        var normalizedEmailAddress = string.IsNullOrWhiteSpace(emailAddress)
            ? null
            : emailAddress.Trim();

        if (string.Equals(
            _googleAccountEmail,
            normalizedEmailAddress,
            StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _googleAccountEmail = normalizedEmailAddress;
        RebuildGoogleCalendarUri();
    }

    private void RebuildGoogleCalendarUri()
    {
        SetGoogleCalendarUri(_workspaceUriBuilder.BuildCalendarDayUri(_displayDate, _googleAccountEmail));
    }

    private void SetGoogleCalendarUri(Uri calendarUri)
    {
        ArgumentNullException.ThrowIfNull(calendarUri);

        if (!SetProperty(ref _googleCalendarUri, calendarUri, nameof(GoogleCalendarUri)))
        {
            return;
        }
    }

    private readonly IGoogleWorkspaceUriBuilder _workspaceUriBuilder;
    private DateOnly _displayDate = DateOnly.FromDateTime(DateTime.Today);
    private string? _googleAccountEmail;
    private int? _unreadEmailCount;
    private Uri _googleCalendarUri = new("https://calendar.google.com/calendar/r/day", UriKind.Absolute);
    private Uri _unreadEmailInboxUri = new("https://mail.google.com/mail/", UriKind.Absolute);
}

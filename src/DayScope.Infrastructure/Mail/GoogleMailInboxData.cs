namespace DayScope.Infrastructure.Mail;

/// <summary>
/// Carries raw unread-count and profile data returned by the Gmail API.
/// </summary>
/// <param name="UnreadCount">The unread inbox thread count.</param>
/// <param name="EmailAddress">The signed-in Gmail address, if available.</param>
public sealed record GoogleMailInboxData(
    int? UnreadCount,
    string? EmailAddress);

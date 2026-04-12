namespace DayScope.Application.Abstractions;

/// <summary>
/// Represents the unread Gmail state shown by the dashboard.
/// </summary>
/// <param name="UnreadCount">The unread message count, if known.</param>
/// <param name="EmailAddress">The signed-in email address, if known.</param>
/// <param name="InboxUri">The Gmail inbox URI for the current account context.</param>
public sealed record EmailInboxSnapshot(
    int? UnreadCount,
    string? EmailAddress,
    Uri InboxUri);

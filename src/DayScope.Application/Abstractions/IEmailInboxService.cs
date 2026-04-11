namespace DayScope.Application.Abstractions;

/// <summary>
/// Loads unread email data for the current user.
/// </summary>
public interface IEmailInboxService
{
    bool IsEnabled { get; }

    /// <summary>
    /// Loads the current inbox snapshot.
    /// </summary>
    /// <param name="allowInteractiveAuthentication">Whether the implementation may prompt the user to sign in.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>The current inbox snapshot.</returns>
    Task<EmailInboxSnapshot> GetInboxSnapshotAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken);
}

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

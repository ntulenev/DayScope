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

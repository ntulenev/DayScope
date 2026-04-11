namespace DayScope.Application.Abstractions;

public interface IEmailInboxService
{
    bool IsEnabled { get; }

    Task<EmailInboxSnapshot> GetInboxSnapshotAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken);
}

public sealed record EmailInboxSnapshot(
    int? UnreadCount,
    string? EmailAddress,
    Uri InboxUri);

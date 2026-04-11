namespace DayScope.Application.Abstractions;

public interface IEmailInboxService
{
    bool IsEnabled { get; }

    Task<int?> GetUnreadEmailCountAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken);
}

using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Demo;

public sealed class DemoEmailInboxService : IEmailInboxService
{
    public DemoEmailInboxService(IOptions<DemoModeSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings.Value;
    }

    public bool IsEnabled => true;

    public Task<EmailInboxSnapshot> GetInboxSnapshotAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken)
    {
        _ = allowInteractiveAuthentication;
        _ = cancellationToken;

        return Task.FromResult(
            new EmailInboxSnapshot(
                Math.Max(0, _settings.UnreadEmailCount),
                null,
                _demoInboxUri));
    }

    private static readonly Uri _demoInboxUri = new("https://mail.google.com/mail/");

    private readonly DemoModeSettings _settings;
}

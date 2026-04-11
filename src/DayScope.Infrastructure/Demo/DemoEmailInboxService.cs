using Microsoft.Extensions.Options;

using DayScope.Application.Abstractions;
using DayScope.Domain.Configuration;

namespace DayScope.Infrastructure.Demo;

/// <summary>
/// Provides deterministic unread inbox data for demo mode.
/// </summary>
public sealed class DemoEmailInboxService : IEmailInboxService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DemoEmailInboxService"/> class.
    /// </summary>
    /// <param name="settings">The configured demo mode settings.</param>
    public DemoEmailInboxService(IOptions<DemoModeSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings.Value;
    }

    public bool IsEnabled => true;

    /// <summary>
    /// Returns a synthetic inbox snapshot for demo mode.
    /// </summary>
    /// <param name="allowInteractiveAuthentication">Whether interactive authentication is allowed.</param>
    /// <param name="cancellationToken">The cancellation token for the request.</param>
    /// <returns>A demo inbox snapshot.</returns>
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

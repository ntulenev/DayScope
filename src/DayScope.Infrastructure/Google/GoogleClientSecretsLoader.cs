using Google.Apis.Auth.OAuth2;

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Loads the configured Google OAuth client secrets from disk.
/// </summary>
public sealed class GoogleClientSecretsLoader : IGoogleClientSecretsLoader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleClientSecretsLoader"/> class.
    /// </summary>
    /// <param name="settings">The configured Google integration settings.</param>
    /// <param name="pathResolver">The path resolver used to normalize the secrets path.</param>
    public GoogleClientSecretsLoader(
        IOptions<GoogleCalendarSettings> settings,
        IPathResolver pathResolver)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pathResolver);

        _settings = settings.Value;
        _pathResolver = pathResolver;
    }

    /// <inheritdoc />
    public GoogleClientSecretsLoadResult LoadClientSecrets()
    {
        var clientSecretsPath = _pathResolver.ResolvePath(_settings.ClientSecretsPath);
        if (string.IsNullOrWhiteSpace(clientSecretsPath) || !File.Exists(clientSecretsPath))
        {
            return GoogleClientSecretsLoadResult.Missing();
        }

        using var stream = File.OpenRead(clientSecretsPath);
        var clientSecrets = GoogleClientSecrets.FromStream(stream).Secrets;
        return GoogleClientSecretsLoadResult.Success(clientSecrets);
    }

    private readonly GoogleCalendarSettings _settings;
    private readonly IPathResolver _pathResolver;
}

using Microsoft.Extensions.Options;

using DayScope.Domain.Configuration;
using DayScope.Infrastructure.Configuration;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Resolves and creates the Google OAuth token-store directory.
/// </summary>
public sealed class GoogleTokenStoreDirectoryProvider : IGoogleTokenStoreDirectoryProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GoogleTokenStoreDirectoryProvider"/> class.
    /// </summary>
    /// <param name="settings">The configured Google integration settings.</param>
    /// <param name="pathResolver">The path resolver used to normalize configured paths.</param>
    public GoogleTokenStoreDirectoryProvider(
        IOptions<GoogleCalendarSettings> settings,
        IPathResolver pathResolver)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pathResolver);

        _settings = settings.Value;
        _pathResolver = pathResolver;
    }

    /// <inheritdoc />
    public string GetTokenStoreDirectory()
    {
        var configuredPath = _pathResolver.ResolvePath(_settings.TokenStoreDirectory);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            Directory.CreateDirectory(configuredPath);
            return configuredPath;
        }

        var fallbackPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DayScope",
            "GoogleCalendarToken");
        Directory.CreateDirectory(fallbackPath);
        return fallbackPath;
    }

    private readonly GoogleCalendarSettings _settings;
    private readonly IPathResolver _pathResolver;
}

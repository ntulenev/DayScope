namespace DayScope.Infrastructure.Google;

/// <summary>
/// Loads Google OAuth client secrets from the configured file-system location.
/// </summary>
public interface IGoogleClientSecretsLoader
{
    /// <summary>
    /// Loads the configured Google OAuth client secrets.
    /// </summary>
    /// <returns>The load result for the configured client secrets.</returns>
    GoogleClientSecretsLoadResult LoadClientSecrets();
}

using Google.Apis.Auth.OAuth2;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Represents the outcome of loading Google OAuth client secrets.
/// </summary>
/// <param name="ClientSecrets">The loaded client secrets, when available.</param>
public sealed record GoogleClientSecretsLoadResult(ClientSecrets? ClientSecrets)
{
    /// <summary>
    /// Creates a successful load result.
    /// </summary>
    /// <param name="clientSecrets">The loaded client secrets.</param>
    /// <returns>The successful load result.</returns>
    public static GoogleClientSecretsLoadResult Success(ClientSecrets clientSecrets)
    {
        ArgumentNullException.ThrowIfNull(clientSecrets);

        return new GoogleClientSecretsLoadResult(clientSecrets);
    }

    /// <summary>
    /// Creates a result that indicates the configured secrets file is unavailable.
    /// </summary>
    /// <returns>The missing-secrets result.</returns>
    public static GoogleClientSecretsLoadResult Missing() =>
        new((ClientSecrets?)null);
}

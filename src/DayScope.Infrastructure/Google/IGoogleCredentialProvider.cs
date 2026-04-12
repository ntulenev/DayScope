namespace DayScope.Infrastructure.Google;

/// <summary>
/// Loads and refreshes Google OAuth credentials used by infrastructure services.
/// </summary>
public interface IGoogleCredentialProvider
{
    /// <summary>
    /// Gets a value indicating whether Google-backed infrastructure is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Loads a cached Google credential and optionally performs interactive authorization.
    /// </summary>
    /// <param name="allowInteractiveAuthentication">Whether the user may be prompted to sign in.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The credential load result.</returns>
    Task<GoogleCredentialLoadResult> GetCredentialAsync(
        bool allowInteractiveAuthentication,
        CancellationToken cancellationToken);
}

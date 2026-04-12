using Google.Apis.Auth.OAuth2;

namespace DayScope.Infrastructure.Google;

/// <summary>
/// Carries the result of a Google credential load attempt.
/// </summary>
/// <param name="Status">The credential load status.</param>
/// <param name="Credential">The loaded credential, when available.</param>
public sealed record GoogleCredentialLoadResult(
    GoogleCredentialLoadStatus Status,
    UserCredential? Credential)
{
    /// <summary>
    /// Creates a successful result that contains a usable credential.
    /// </summary>
    /// <param name="credential">The loaded credential.</param>
    /// <returns>The successful load result.</returns>
    public static GoogleCredentialLoadResult Success(UserCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);

        return new GoogleCredentialLoadResult(GoogleCredentialLoadStatus.Success, credential);
    }

    /// <summary>
    /// Creates a result that indicates Google integration is disabled.
    /// </summary>
    /// <returns>The disabled result.</returns>
    public static GoogleCredentialLoadResult Disabled() =>
        new(GoogleCredentialLoadStatus.Disabled, null);

    /// <summary>
    /// Creates a result that indicates the OAuth client secrets file is missing.
    /// </summary>
    /// <returns>The missing-client-secrets result.</returns>
    public static GoogleCredentialLoadResult ClientSecretsMissing() =>
        new(GoogleCredentialLoadStatus.ClientSecretsMissing, null);

    /// <summary>
    /// Creates a result that indicates the user must authorize the application.
    /// </summary>
    /// <returns>The authorization-required result.</returns>
    public static GoogleCredentialLoadResult AuthorizationRequired() =>
        new(GoogleCredentialLoadStatus.AuthorizationRequired, null);
}

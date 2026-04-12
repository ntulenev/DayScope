namespace DayScope.Infrastructure.Google;

/// <summary>
/// Resolves the directory used to persist Google OAuth tokens.
/// </summary>
public interface IGoogleTokenStoreDirectoryProvider
{
    /// <summary>
    /// Gets the absolute directory used for the Google OAuth token store.
    /// </summary>
    /// <returns>The absolute token-store directory path.</returns>
    string GetTokenStoreDirectory();
}

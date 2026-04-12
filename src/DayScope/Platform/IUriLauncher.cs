namespace DayScope.Platform;

/// <summary>
/// Opens URIs through the operating-system shell.
/// </summary>
public interface IUriLauncher
{
    /// <summary>
    /// Attempts to open the provided URI.
    /// </summary>
    /// <param name="uri">The URI to open.</param>
    void Open(Uri uri);
}

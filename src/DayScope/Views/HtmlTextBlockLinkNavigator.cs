using DayScope.Platform;

namespace DayScope.Views;

/// <summary>
/// Holds the URI launcher used by HTML-rendered hyperlinks.
/// </summary>
public static class HtmlTextBlockLinkNavigator
{
    /// <summary>
    /// Configures the URI launcher used for hyperlink navigation.
    /// </summary>
    /// <param name="uriLauncher">The launcher used to open external links.</param>
    public static void Configure(IUriLauncher uriLauncher)
    {
        _uriLauncher = uriLauncher ?? throw new ArgumentNullException(nameof(uriLauncher));
    }

    /// <summary>
    /// Attempts to open the provided URI through the configured launcher.
    /// </summary>
    /// <param name="uri">The URI to open.</param>
    public static void Open(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        _uriLauncher?.Open(uri);
    }

    private static IUriLauncher? _uriLauncher;
}

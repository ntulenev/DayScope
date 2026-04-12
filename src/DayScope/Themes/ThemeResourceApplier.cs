namespace DayScope.Themes;

/// <summary>
/// Applies concrete theme resource dictionaries to the current WPF application resources.
/// </summary>
public sealed class ApplicationThemeResourceApplier : IThemeResourceApplier
{
    /// <inheritdoc />
    public bool ApplyTheme(AppThemeMode themeMode)
    {
        if (System.Windows.Application.Current is not { Resources: var resources })
        {
            return false;
        }

        if (_themeDictionary is not null)
        {
            resources.MergedDictionaries.Remove(_themeDictionary);
        }

        _themeDictionary = new System.Windows.ResourceDictionary
        {
            Source = ResolveThemeUri(themeMode)
        };

        resources.MergedDictionaries.Insert(0, _themeDictionary);
        return true;
    }

    private Uri ResolveThemeUri(AppThemeMode themeMode)
    {
        return themeMode switch
        {
            AppThemeMode.Os => _darkThemeUri,
            AppThemeMode.Light => _lightThemeUri,
            AppThemeMode.Forest => _forestThemeUri,
            AppThemeMode.Autumn => _autumnThemeUri,
            AppThemeMode.DarkPink => _darkPinkThemeUri,
            AppThemeMode.Matrix => _matrixThemeUri,
            AppThemeMode.Dark => _darkThemeUri,
            _ => _darkThemeUri
        };
    }

    private readonly Uri _darkThemeUri = new("Themes/DarkTheme.xaml", UriKind.Relative);
    private readonly Uri _lightThemeUri = new("Themes/LightTheme.xaml", UriKind.Relative);
    private readonly Uri _forestThemeUri = new("Themes/ForestTheme.xaml", UriKind.Relative);
    private readonly Uri _autumnThemeUri = new("Themes/AutumnTheme.xaml", UriKind.Relative);
    private readonly Uri _darkPinkThemeUri = new("Themes/DarkPinkTheme.xaml", UriKind.Relative);
    private readonly Uri _matrixThemeUri = new("Themes/MatrixTheme.xaml", UriKind.Relative);
    private System.Windows.ResourceDictionary? _themeDictionary;
}

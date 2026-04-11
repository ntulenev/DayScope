using System.Windows;
using System.IO;
using System.Security;

using Microsoft.Win32;

namespace DayScope.Themes;

public sealed class ThemeManager : IDisposable
{
    public event EventHandler? ThemeChanged;

    public AppThemeMode SelectedMode { get; private set; } = AppThemeMode.Os;

    public bool IsDarkTheme => _effectiveTheme != EffectiveTheme.Light;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        SelectedMode = _preferenceStore.LoadThemeMode();
        ApplyTheme(force: true);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        _isInitialized = true;
    }

    public void SetThemeMode(AppThemeMode themeMode)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        if (SelectedMode == themeMode)
        {
            return;
        }

        SelectedMode = themeMode;
        _preferenceStore.SaveThemeMode(themeMode);
        ApplyTheme(force: true);
    }

    public void Dispose()
    {
        if (!_isInitialized)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _isInitialized = false;
    }

    public ThemeManager(ThemePreferenceStore preferenceStore)
    {
        ArgumentNullException.ThrowIfNull(preferenceStore);

        _preferenceStore = preferenceStore;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (SelectedMode != AppThemeMode.Os)
        {
            return;
        }

        if (System.Windows.Application.Current is not { } application)
        {
            return;
        }

        application.Dispatcher.Invoke(() => ApplyTheme(force: false));
    }

    private void ApplyTheme(bool force)
    {
        if (System.Windows.Application.Current is not { Resources: var resources })
        {
            return;
        }

        var effectiveTheme = ResolveEffectiveTheme();
        if (!force &&
            _themeDictionary is not null &&
            effectiveTheme == _effectiveTheme)
        {
            return;
        }

        if (_themeDictionary is not null)
        {
            resources.MergedDictionaries.Remove(_themeDictionary);
        }

        _themeDictionary = new ResourceDictionary
        {
            Source = effectiveTheme switch
            {
                EffectiveTheme.Light => _lightThemeUri,
                EffectiveTheme.Forest => _forestThemeUri,
                EffectiveTheme.DarkPink => _darkPinkThemeUri,
                EffectiveTheme.Dark => _darkThemeUri,
                _ => _darkThemeUri
            }
        };

        resources.MergedDictionaries.Insert(0, _themeDictionary);
        _effectiveTheme = effectiveTheme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private EffectiveTheme ResolveEffectiveTheme()
    {
        return SelectedMode switch
        {
            AppThemeMode.Os => ResolveOsTheme(),
            AppThemeMode.Light => EffectiveTheme.Light,
            AppThemeMode.Dark => EffectiveTheme.Dark,
            AppThemeMode.Forest => EffectiveTheme.Forest,
            AppThemeMode.DarkPink => EffectiveTheme.DarkPink,
            _ => ResolveOsTheme()
        };
    }

    private static EffectiveTheme ResolveOsTheme()
    {
        try
        {
            var registryValue = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                0);

            return registryValue is int intValue && intValue > 0
                ? EffectiveTheme.Light
                : EffectiveTheme.Dark;
        }
        catch (Exception ex) when (ex is IOException or SecurityException or UnauthorizedAccessException)
        {
            return EffectiveTheme.Dark;
        }
    }

    private readonly ThemePreferenceStore _preferenceStore;
    private readonly Uri _darkThemeUri = new("Themes/DarkTheme.xaml", UriKind.Relative);
    private readonly Uri _lightThemeUri = new("Themes/LightTheme.xaml", UriKind.Relative);
    private readonly Uri _forestThemeUri = new("Themes/ForestTheme.xaml", UriKind.Relative);
    private readonly Uri _darkPinkThemeUri = new("Themes/DarkPinkTheme.xaml", UriKind.Relative);
    private ResourceDictionary? _themeDictionary;
    private EffectiveTheme _effectiveTheme = EffectiveTheme.Dark;
    private bool _isInitialized;

    private enum EffectiveTheme
    {
        Light = 0,
        Dark = 1,
        Forest = 2,
        DarkPink = 3
    }
}

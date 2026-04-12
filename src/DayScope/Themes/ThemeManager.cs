using System.Windows;
using System.IO;
using System.Security;

using Microsoft.Win32;

namespace DayScope.Themes;

/// <summary>
/// Applies the active application theme and reacts to theme mode changes.
/// </summary>
public sealed class ThemeManager : IDisposable
{
    /// <summary>
    /// Raised after the active theme resources have been updated.
    /// </summary>
    public event EventHandler? ThemeChanged;

    /// <summary>
    /// Gets the theme mode selected by the user.
    /// </summary>
    public AppThemeMode SelectedMode { get; private set; } = AppThemeMode.Os;

    /// <summary>
    /// Gets a value indicating whether the currently applied theme should use dark window chrome.
    /// </summary>
    public bool IsDarkTheme => _effectiveTheme != EffectiveTheme.Light;

    /// <summary>
    /// Initializes the theme manager and applies the persisted theme selection.
    /// </summary>
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

    /// <summary>
    /// Updates the selected theme mode and applies its resources.
    /// </summary>
    /// <param name="themeMode">The theme mode to activate.</param>
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

    /// <summary>
    /// Stops listening for system theme changes.
    /// </summary>
    public void Dispose()
    {
        if (!_isInitialized)
        {
            return;
        }

        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _isInitialized = false;
    }

    /// <summary>
    /// Creates a theme manager that uses the provided preference store.
    /// </summary>
    /// <param name="preferenceStore">The store used to load and save the selected theme.</param>
    public ThemeManager(ThemePreferenceStore preferenceStore)
    {
        ArgumentNullException.ThrowIfNull(preferenceStore);

        _preferenceStore = preferenceStore;
    }

    /// <summary>
    /// Reapplies the OS-selected theme when Windows theme preferences change.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The preference change arguments.</param>
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

    /// <summary>
    /// Applies the currently selected theme to the application resources.
    /// </summary>
    /// <param name="force">Whether the theme should be applied even when unchanged.</param>
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
                EffectiveTheme.Autumn => _autumnThemeUri,
                EffectiveTheme.DarkPink => _darkPinkThemeUri,
                EffectiveTheme.Matrix => _matrixThemeUri,
                EffectiveTheme.Dark => _darkThemeUri,
                _ => _darkThemeUri
            }
        };

        resources.MergedDictionaries.Insert(0, _themeDictionary);
        _effectiveTheme = effectiveTheme;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resolves the effective theme after considering OS mode and explicit selection.
    /// </summary>
    /// <returns>The theme that should be applied.</returns>
    private EffectiveTheme ResolveEffectiveTheme()
    {
        return SelectedMode switch
        {
            AppThemeMode.Os => ResolveOsTheme(),
            AppThemeMode.Light => EffectiveTheme.Light,
            AppThemeMode.Dark => EffectiveTheme.Dark,
            AppThemeMode.Forest => EffectiveTheme.Forest,
            AppThemeMode.Autumn => EffectiveTheme.Autumn,
            AppThemeMode.DarkPink => EffectiveTheme.DarkPink,
            AppThemeMode.Matrix => EffectiveTheme.Matrix,
            _ => ResolveOsTheme()
        };
    }

    /// <summary>
    /// Resolves the current Windows app theme preference.
    /// </summary>
    /// <returns>The effective theme derived from Windows settings.</returns>
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
    private readonly Uri _autumnThemeUri = new("Themes/AutumnTheme.xaml", UriKind.Relative);
    private readonly Uri _darkPinkThemeUri = new("Themes/DarkPinkTheme.xaml", UriKind.Relative);
    private readonly Uri _matrixThemeUri = new("Themes/MatrixTheme.xaml", UriKind.Relative);
    private ResourceDictionary? _themeDictionary;
    private EffectiveTheme _effectiveTheme = EffectiveTheme.Dark;
    private bool _isInitialized;

    /// <summary>
    /// Represents the concrete resource dictionary theme applied to the app.
    /// </summary>
    private enum EffectiveTheme
    {
        Light = 0,
        Dark = 1,
        Forest = 2,
        Autumn = 3,
        DarkPink = 4,
        Matrix = 5
    }
}

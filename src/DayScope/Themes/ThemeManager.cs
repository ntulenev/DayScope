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
    public bool IsDarkTheme => _appliedThemeMode != AppThemeMode.Light;

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
    /// <param name="osThemeDetector">The detector used to resolve the current OS theme.</param>
    /// <param name="themeResourceApplier">The applier used to update WPF theme resources.</param>
    public ThemeManager(
        IThemePreferenceStore preferenceStore,
        IOsThemeDetector osThemeDetector,
        IThemeResourceApplier themeResourceApplier)
    {
        ArgumentNullException.ThrowIfNull(preferenceStore);
        ArgumentNullException.ThrowIfNull(osThemeDetector);
        ArgumentNullException.ThrowIfNull(themeResourceApplier);

        _preferenceStore = preferenceStore;
        _osThemeDetector = osThemeDetector;
        _themeResourceApplier = themeResourceApplier;
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
        var appliedThemeMode = ResolveAppliedThemeMode();
        if (!force &&
            appliedThemeMode == _appliedThemeMode)
        {
            return;
        }

        if (!_themeResourceApplier.ApplyTheme(appliedThemeMode))
        {
            return;
        }

        _appliedThemeMode = appliedThemeMode;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resolves the effective theme after considering OS mode and explicit selection.
    /// </summary>
    /// <returns>The theme that should be applied.</returns>
    private AppThemeMode ResolveAppliedThemeMode()
    {
        return SelectedMode switch
        {
            AppThemeMode.Os => _osThemeDetector.DetectThemeMode(),
            AppThemeMode.Light => AppThemeMode.Light,
            AppThemeMode.Dark => AppThemeMode.Dark,
            AppThemeMode.Forest => AppThemeMode.Forest,
            AppThemeMode.Autumn => AppThemeMode.Autumn,
            AppThemeMode.DarkPink => AppThemeMode.DarkPink,
            AppThemeMode.Matrix => AppThemeMode.Matrix,
            AppThemeMode.Code => AppThemeMode.Code,
            AppThemeMode.Cyberpunk => AppThemeMode.Cyberpunk,
            AppThemeMode.DeepSea => AppThemeMode.DeepSea,
            _ => _osThemeDetector.DetectThemeMode()
        };
    }

    private readonly IThemePreferenceStore _preferenceStore;
    private readonly IOsThemeDetector _osThemeDetector;
    private readonly IThemeResourceApplier _themeResourceApplier;
    private AppThemeMode _appliedThemeMode = AppThemeMode.Dark;
    private bool _isInitialized;
}

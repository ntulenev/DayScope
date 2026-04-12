namespace DayScope.Themes;

/// <summary>
/// Applies concrete theme resource dictionaries to the running WPF application.
/// </summary>
public interface IThemeResourceApplier
{
    /// <summary>
    /// Applies the resource dictionary for the provided concrete theme mode.
    /// </summary>
    /// <param name="themeMode">The concrete theme mode to apply.</param>
    /// <returns><see langword="true"/> when resources were applied; otherwise, <see langword="false"/>.</returns>
    bool ApplyTheme(AppThemeMode themeMode);
}

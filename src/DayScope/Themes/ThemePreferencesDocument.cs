namespace DayScope.Themes;

/// <summary>
/// Represents the serialized theme preference payload.
/// </summary>
/// <param name="ThemeMode">The persisted theme mode.</param>
internal sealed record ThemePreferencesDocument(AppThemeMode ThemeMode);

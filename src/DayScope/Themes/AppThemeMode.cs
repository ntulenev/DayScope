namespace DayScope.Themes;

/// <summary>
/// Defines the available visual theme modes for the application.
/// </summary>
public enum AppThemeMode
{
    /// <summary>
    /// Follows the current Windows app theme preference.
    /// </summary>
    Os = 0,

    /// <summary>
    /// Uses the light DayScope palette.
    /// </summary>
    Light = 1,

    /// <summary>
    /// Uses the default dark DayScope palette.
    /// </summary>
    Dark = 2,

    /// <summary>
    /// Uses the muted forest-inspired dark palette.
    /// </summary>
    Forest = 3,

    /// <summary>
    /// Uses a warm autumn-inspired dark palette.
    /// </summary>
    Autumn = 4,

    /// <summary>
    /// Uses the dark pink bordo-inspired palette.
    /// </summary>
    DarkPink = 5,

    /// <summary>
    /// Uses a Matrix-inspired neon green dark palette.
    /// </summary>
    Matrix = 6,

    /// <summary>
    /// Uses a Visual Studio-inspired coding palette.
    /// </summary>
    Code = 7,

    /// <summary>
    /// Uses a neon cyberpunk palette with violet surfaces.
    /// </summary>
    Cyberpunk = 8
}

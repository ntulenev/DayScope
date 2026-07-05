using System.Windows.Input;

namespace DayScope.Views;

/// <summary>
/// Resolves keyboard input into privacy-mode shortcut actions.
/// </summary>
internal static class PrivacyModeKeyboardShortcut
{
    /// <summary>
    /// Determines whether the provided key and modifier combination toggles privacy mode.
    /// </summary>
    /// <param name="key">The keyboard key reported by WPF.</param>
    /// <param name="modifiers">The active keyboard modifiers.</param>
    /// <returns><see langword="true"/> when the input toggles privacy mode; otherwise <see langword="false"/>.</returns>
    public static bool IsToggle(Key key, ModifierKeys modifiers) =>
        key == Key.P &&
        (modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
        (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift &&
        (modifiers & ~(ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None;
}

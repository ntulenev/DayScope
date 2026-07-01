using System.Windows.Input;

namespace DayScope.Views;

/// <summary>
/// Resolves keyboard input into calendar zoom shortcut actions.
/// </summary>
internal static class CalendarZoomKeyboardShortcut
{
    /// <summary>
    /// Attempts to resolve a key and modifier combination into a calendar zoom action.
    /// </summary>
    /// <param name="key">The keyboard key reported by WPF.</param>
    /// <param name="modifiers">The active keyboard modifiers.</param>
    /// <param name="action">The resolved calendar zoom action when the shortcut is recognized.</param>
    /// <returns><see langword="true"/> when the input is a supported calendar zoom shortcut; otherwise <see langword="false"/>.</returns>
    public static bool TryResolve(
        Key key,
        ModifierKeys modifiers,
        out CalendarZoomKeyboardShortcutAction action)
    {
        action = default;
        if (!HasControlWithOptionalShift(modifiers))
        {
            return false;
        }

        if (key is Key.OemMinus or Key.Subtract)
        {
            action = CalendarZoomKeyboardShortcutAction.Decrease;
            return true;
        }

        if (key is Key.OemPlus or Key.Add)
        {
            action = CalendarZoomKeyboardShortcutAction.Increase;
            return true;
        }

        return false;
    }

    private static bool HasControlWithOptionalShift(ModifierKeys modifiers) =>
        (modifiers & ModifierKeys.Control) == ModifierKeys.Control &&
        (modifiers & ~(ModifierKeys.Control | ModifierKeys.Shift)) == ModifierKeys.None;
}

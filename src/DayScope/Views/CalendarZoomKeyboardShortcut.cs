using System.Windows.Input;

namespace DayScope.Views;

internal enum CalendarZoomKeyboardShortcutAction
{
    Decrease,
    Increase
}

internal static class CalendarZoomKeyboardShortcut
{
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

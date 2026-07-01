using System.Windows.Input;

using DayScope.Views;

using FluentAssertions;

namespace DayScope.Tests;

public sealed class CalendarZoomKeyboardShortcutTests
{
    [Theory(DisplayName = "Ctrl plus resolves to zoom in.")]
    [Trait("Category", "Unit")]
    [InlineData(Key.OemPlus, ModifierKeys.Control)]
    [InlineData(Key.OemPlus, ModifierKeys.Control | ModifierKeys.Shift)]
    [InlineData(Key.Add, ModifierKeys.Control)]
    public void TryResolveShouldResolveZoomIn(Key key, ModifierKeys modifiers)
    {
        // Act
        var resolved = CalendarZoomKeyboardShortcut.TryResolve(key, modifiers, out var action);

        // Assert
        resolved.Should().BeTrue();
        action.Should().Be(CalendarZoomKeyboardShortcutAction.Increase);
    }

    [Theory(DisplayName = "Ctrl minus resolves to zoom out.")]
    [Trait("Category", "Unit")]
    [InlineData(Key.OemMinus, ModifierKeys.Control)]
    [InlineData(Key.Subtract, ModifierKeys.Control)]
    public void TryResolveShouldResolveZoomOut(Key key, ModifierKeys modifiers)
    {
        // Act
        var resolved = CalendarZoomKeyboardShortcut.TryResolve(key, modifiers, out var action);

        // Assert
        resolved.Should().BeTrue();
        action.Should().Be(CalendarZoomKeyboardShortcutAction.Decrease);
    }

    [Theory(DisplayName = "Unsupported key combinations are ignored.")]
    [Trait("Category", "Unit")]
    [InlineData(Key.OemPlus, ModifierKeys.None)]
    [InlineData(Key.OemPlus, ModifierKeys.Control | ModifierKeys.Alt)]
    [InlineData(Key.OemMinus, ModifierKeys.Control | ModifierKeys.Windows)]
    [InlineData(Key.D0, ModifierKeys.Control)]
    public void TryResolveShouldIgnoreUnsupportedCombinations(Key key, ModifierKeys modifiers)
    {
        // Act
        var resolved = CalendarZoomKeyboardShortcut.TryResolve(key, modifiers, out _);

        // Assert
        resolved.Should().BeFalse();
    }
}

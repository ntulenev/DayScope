using System.Windows.Input;

using DayScope.Views;

using FluentAssertions;

namespace DayScope.Tests;

public sealed class PrivacyModeKeyboardShortcutTests
{
    [Fact(DisplayName = "Ctrl Shift P resolves to privacy mode toggle.")]
    [Trait("Category", "Unit")]
    public void IsToggleShouldResolvePrivacyModeToggle()
    {
        // Act
        var resolved = PrivacyModeKeyboardShortcut.IsToggle(
            Key.P,
            ModifierKeys.Control | ModifierKeys.Shift);

        // Assert
        resolved.Should().BeTrue();
    }

    [Theory(DisplayName = "Unsupported privacy mode shortcuts are ignored.")]
    [Trait("Category", "Unit")]
    [InlineData(Key.P, ModifierKeys.Control)]
    [InlineData(Key.P, ModifierKeys.Shift)]
    [InlineData(Key.P, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)]
    [InlineData(Key.O, ModifierKeys.Control | ModifierKeys.Shift)]
    public void IsToggleShouldIgnoreUnsupportedCombinations(Key key, ModifierKeys modifiers)
    {
        // Act
        var resolved = PrivacyModeKeyboardShortcut.IsToggle(key, modifiers);

        // Assert
        resolved.Should().BeFalse();
    }
}

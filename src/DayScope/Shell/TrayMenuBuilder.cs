using DayScope.Themes;

namespace DayScope.Shell;

/// <summary>
/// Builds the context menu used by the system-tray icon.
/// </summary>
internal static class TrayMenuBuilder
{
    /// <summary>
    /// Creates the tray menu and the helper used to synchronize theme selection.
    /// </summary>
    /// <param name="showAction">The action used to show the main window.</param>
    /// <param name="refreshAction">The action used to trigger an immediate refresh.</param>
    /// <param name="copyScheduleAction">The action used to copy the active schedule.</param>
    /// <param name="setThemeModeAction">The action used to switch the active theme mode.</param>
    /// <param name="exitAction">The action used to exit the application.</param>
    /// <returns>The tray menu and theme menu controller.</returns>
    public static (ContextMenuStrip Menu, TrayThemeMenuController ThemeMenuController) Build(
        Action showAction,
        Action refreshAction,
        Action copyScheduleAction,
        Action<AppThemeMode> setThemeModeAction,
        Action exitAction)
    {
        ArgumentNullException.ThrowIfNull(showAction);
        ArgumentNullException.ThrowIfNull(refreshAction);
        ArgumentNullException.ThrowIfNull(copyScheduleAction);
        ArgumentNullException.ThrowIfNull(setThemeModeAction);
        ArgumentNullException.ThrowIfNull(exitAction);

        var themeMenuController = new TrayThemeMenuController(setThemeModeAction);
        var menu = new ContextMenuStrip();
        menu.Items.Add(CreateMenuItem("Open", showAction));
        menu.Items.Add(CreateMenuItem("Refresh now", refreshAction));
        menu.Items.Add(CreateMenuItem("Copy Schedule", copyScheduleAction));
        menu.Items.Add(themeMenuController.CreateMenuItem());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(CreateMenuItem("Exit", exitAction));

        return (menu, themeMenuController);
    }

    private static ToolStripMenuItem CreateMenuItem(string text, Action onClick)
    {
        var menuItem = new ToolStripMenuItem(text);
        menuItem.Click += (_, _) => onClick();
        return menuItem;
    }
}

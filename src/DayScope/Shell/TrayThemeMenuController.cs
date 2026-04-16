using DayScope.Themes;

namespace DayScope.Shell;

/// <summary>
/// Owns the tray menu section used to switch between application themes.
/// </summary>
internal sealed class TrayThemeMenuController
{
    private static readonly (AppThemeMode Mode, string Label)[] _themeModes =
    [
        (AppThemeMode.Os, "OS"),
        (AppThemeMode.Light, "Light"),
        (AppThemeMode.Dark, "Dark"),
        (AppThemeMode.Forest, "Forest"),
        (AppThemeMode.Autumn, "Autumn"),
        (AppThemeMode.DarkPink, "Dark Pink"),
        (AppThemeMode.Matrix, "Matrix"),
        (AppThemeMode.Code, "Code"),
        (AppThemeMode.Cyberpunk, "Cyberpunk"),
        (AppThemeMode.DeepSea, "Deep sea")
    ];

    private readonly Action<AppThemeMode> _setThemeModeAction;
    private readonly Dictionary<AppThemeMode, ToolStripMenuItem> _menuItems = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TrayThemeMenuController"/> class.
    /// </summary>
    /// <param name="setThemeModeAction">The action used to apply the selected theme mode.</param>
    public TrayThemeMenuController(Action<AppThemeMode> setThemeModeAction)
    {
        ArgumentNullException.ThrowIfNull(setThemeModeAction);

        _setThemeModeAction = setThemeModeAction;
    }

    /// <summary>
    /// Creates the root tray menu item that contains all available theme options.
    /// </summary>
    /// <returns>The root theme menu item.</returns>
    public ToolStripMenuItem CreateMenuItem()
    {
        var rootItem = new ToolStripMenuItem("Theme");
        foreach (var (mode, label) in _themeModes)
        {
            var menuItem = new ToolStripMenuItem(label);
            menuItem.Click += (_, _) => _setThemeModeAction(mode);
            _menuItems.Add(mode, menuItem);
            rootItem.DropDownItems.Add(menuItem);
        }

        return rootItem;
    }

    /// <summary>
    /// Updates the checked state of tray theme items for the provided selection.
    /// </summary>
    /// <param name="selectedMode">The currently selected theme mode.</param>
    public void UpdateSelection(AppThemeMode selectedMode)
    {
        foreach (var (mode, menuItem) in _menuItems)
        {
            menuItem.Checked = mode == selectedMode;
        }
    }
}

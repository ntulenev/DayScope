using DayScope.Themes;
using DayScope.ViewModels;

namespace DayScope.Views;

/// <summary>
/// Represents a theme option rendered inside the main-window header menu.
/// </summary>
public sealed class MainWindowThemeOptionViewModel : ObservableObject
{

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowThemeOptionViewModel"/> class.
    /// </summary>
    /// <param name="mode">The represented theme mode.</param>
    /// <param name="label">The label shown in the menu.</param>
    public MainWindowThemeOptionViewModel(AppThemeMode mode, string label)
    {
        ArgumentNullException.ThrowIfNull(label);

        Mode = mode;
        Label = label;
    }

    /// <summary>
    /// Gets the represented theme mode.
    /// </summary>
    public AppThemeMode Mode { get; }

    /// <summary>
    /// Gets the label shown in the menu.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this option matches the current theme.
    /// </summary>
    public bool IsSelected {
        get;
        set => SetProperty(ref field, value);
    }
}

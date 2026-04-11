namespace DayScope.Domain.Configuration;

/// <summary>
/// Represents the configured window sizing constraints.
/// </summary>
public sealed class WindowSettings
{
    public int Width { get; set; } = 920;

    public int Height { get; set; } = 680;

    public int MinWidth { get; set; } = 660;

    public int MinHeight { get; set; } = 500;

    /// <summary>
    /// Normalizes window sizes so they respect minimum bounds.
    /// </summary>
    public void Normalize()
    {
        Width = Math.Max(640, Width);
        Height = Math.Max(480, Height);
        MinWidth = Math.Max(480, MinWidth);
        MinHeight = Math.Max(320, MinHeight);

        if (Width < MinWidth)
        {
            Width = MinWidth;
        }

        if (Height < MinHeight)
        {
            Height = MinHeight;
        }
    }

    /// <summary>
    /// Validates the current window settings.
    /// </summary>
    /// <returns>A list of validation failures.</returns>
    public IReadOnlyList<string> Validate()
    {
        List<string> failures = [];

        if (Width < MinWidth)
        {
            failures.Add("Window:Width must be greater than or equal to MinWidth.");
        }

        if (Height < MinHeight)
        {
            failures.Add("Window:Height must be greater than or equal to MinHeight.");
        }

        return failures;
    }
}

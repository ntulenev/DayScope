using System.Windows;
using System.Windows.Controls;

namespace DayScope.Views;

/// <summary>
/// Renders a constrained subset of HTML content into a WPF <see cref="TextBlock"/>.
/// </summary>
public static class HtmlTextBlock
{
#pragma warning disable IDE1006
    public static readonly DependencyProperty HtmlProperty =
        DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(HtmlTextBlock),
            new PropertyMetadata(null, OnHtmlChanged));
#pragma warning restore IDE1006

    /// <summary>
    /// Gets the HTML string attached to a dependency object.
    /// </summary>
    /// <param name="obj">The dependency object that stores the attached value.</param>
    /// <returns>The attached HTML content, if any.</returns>
    public static string? GetHtml(DependencyObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return (string?)obj.GetValue(HtmlProperty);
    }

    /// <summary>
    /// Sets the HTML string attached to a dependency object.
    /// </summary>
    /// <param name="obj">The dependency object that stores the attached value.</param>
    /// <param name="value">The HTML content to render.</param>
    public static void SetHtml(DependencyObject obj, string? value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(HtmlProperty, value);
    }

    /// <summary>
    /// Re-renders attached HTML content when the attached property changes.
    /// </summary>
    /// <param name="dependencyObject">The object receiving the HTML content.</param>
    /// <param name="args">The property change arguments.</param>
    private static void OnHtmlChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is not TextBlock textBlock)
        {
            return;
        }

        HtmlTextBlockRenderer.Render(textBlock, args.NewValue as string);
    }
}

using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DayScope.Views;

/// <summary>
/// Renders a constrained subset of HTML content into a WPF <see cref="TextBlock"/>.
/// </summary>
public static partial class HtmlTextBlock
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

        textBlock.Inlines.Clear();
        var html = args.NewValue as string;
        if (string.IsNullOrWhiteSpace(html))
        {
            return;
        }

        var pendingBreaks = 0;
        var hasVisibleContent = false;

        foreach (Match tokenMatch in HtmlTokenRegex().Matches(html))
        {
            if (tokenMatch.Groups["anchor"].Success)
            {
                FlushPendingBreaks(textBlock, ref pendingBreaks, hasVisibleContent);
                AppendAnchor(
                    textBlock,
                    tokenMatch.Groups["href"].Value,
                    tokenMatch.Groups["anchorText"].Value,
                    ref hasVisibleContent);
                continue;
            }

            if (tokenMatch.Groups["break"].Success)
            {
                pendingBreaks = Math.Max(pendingBreaks, 1);
                continue;
            }

            if (tokenMatch.Groups["block"].Success)
            {
                pendingBreaks = Math.Max(pendingBreaks, 2);
                continue;
            }

            if (tokenMatch.Groups["li"].Success)
            {
                FlushPendingBreaks(textBlock, ref pendingBreaks, hasVisibleContent);
                textBlock.Inlines.Add(new Run("• "));
                hasVisibleContent = true;
                continue;
            }

            if (!tokenMatch.Groups["text"].Success)
            {
                continue;
            }

            var decodedText = DecodeText(tokenMatch.Groups["text"].Value);
            if (string.IsNullOrWhiteSpace(decodedText))
            {
                continue;
            }

            FlushPendingBreaks(textBlock, ref pendingBreaks, hasVisibleContent);
            AppendTextWithAutoLinks(textBlock, decodedText, ref hasVisibleContent);
        }
    }

    /// <summary>
    /// Appends pending line breaks before the next visible content fragment.
    /// </summary>
    /// <param name="textBlock">The target text block.</param>
    /// <param name="pendingBreaks">The number of pending line breaks.</param>
    /// <param name="hasVisibleContent">Whether content has already been rendered.</param>
    private static void FlushPendingBreaks(
        TextBlock textBlock,
        ref int pendingBreaks,
        bool hasVisibleContent)
    {
        if (!hasVisibleContent || pendingBreaks <= 0)
        {
            pendingBreaks = 0;
            return;
        }

        for (var index = 0; index < pendingBreaks; index++)
        {
            textBlock.Inlines.Add(new LineBreak());
        }

        pendingBreaks = 0;
    }

    /// <summary>
    /// Appends an HTML anchor to the text block.
    /// </summary>
    /// <param name="textBlock">The target text block.</param>
    /// <param name="href">The anchor href value.</param>
    /// <param name="anchorText">The inner anchor text.</param>
    /// <param name="hasVisibleContent">Whether visible content has already been rendered.</param>
    private static void AppendAnchor(
        TextBlock textBlock,
        string href,
        string anchorText,
        ref bool hasVisibleContent)
    {
        var displayText = StripTags(anchorText);
        displayText = DecodeText(displayText);
        if (string.IsNullOrWhiteSpace(displayText))
        {
            displayText = href;
        }

        if (TryCreateUri(href, out var uri))
        {
            textBlock.Inlines.Add(CreateHyperlink(uri, displayText));
        }
        else
        {
            textBlock.Inlines.Add(new Run(displayText));
        }

        hasVisibleContent = true;
    }

    /// <summary>
    /// Appends decoded text and automatically links plain-text URLs.
    /// </summary>
    /// <param name="textBlock">The target text block.</param>
    /// <param name="text">The decoded text fragment.</param>
    /// <param name="hasVisibleContent">Whether visible content has already been rendered.</param>
    private static void AppendTextWithAutoLinks(
        TextBlock textBlock,
        string text,
        ref bool hasVisibleContent)
    {
        var cursor = 0;
        foreach (Match urlMatch in UrlRegex().Matches(text))
        {
            if (urlMatch.Index > cursor)
            {
                textBlock.Inlines.Add(new Run(text[cursor..urlMatch.Index]));
                hasVisibleContent = true;
            }

            var rawUrl = urlMatch.Value;
            var url = rawUrl.TrimEnd('.', ',', ';', ')', ']', '}');
            var trailingSuffix = rawUrl[url.Length..];
            if (TryCreateUri(url, out var uri))
            {
                textBlock.Inlines.Add(CreateHyperlink(uri, url));
            }
            else
            {
                textBlock.Inlines.Add(new Run(rawUrl));
            }

            if (trailingSuffix.Length > 0)
            {
                textBlock.Inlines.Add(new Run(trailingSuffix));
            }

            cursor = urlMatch.Index + urlMatch.Length;
            hasVisibleContent = true;
        }

        if (cursor < text.Length)
        {
            textBlock.Inlines.Add(new Run(text[cursor..]));
            hasVisibleContent = true;
        }
    }

    /// <summary>
    /// Creates a hyperlink inline for the specified URI.
    /// </summary>
    /// <param name="uri">The target URI.</param>
    /// <param name="text">The hyperlink text.</param>
    /// <returns>The hyperlink inline.</returns>
    private static Hyperlink CreateHyperlink(Uri uri, string text)
    {
        var hyperlink = new Hyperlink(new Run(text))
        {
            NavigateUri = uri,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(126, 185, 255))
        };
        hyperlink.Click += OnHyperlinkClick;
        return hyperlink;
    }

    /// <summary>
    /// Opens hyperlink clicks through the system shell.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="args">The routed event arguments.</param>
    private static void OnHyperlinkClick(object sender, RoutedEventArgs args)
    {
        if (sender is not Hyperlink { NavigateUri: { } uri })
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    /// <summary>
    /// Decodes HTML entities and normalizes non-breaking spaces.
    /// </summary>
    /// <param name="text">The encoded text to decode.</param>
    /// <returns>The decoded text.</returns>
    private static string DecodeText(string text) =>
        WebUtility.HtmlDecode(text)
            .Replace('\u00A0', ' ');

    /// <summary>
    /// Removes generic HTML tags from a fragment.
    /// </summary>
    /// <param name="text">The HTML fragment.</param>
    /// <returns>The text with generic tags removed.</returns>
    private static string StripTags(string text) => GenericTagRegex().Replace(text, string.Empty);

    /// <summary>
    /// Attempts to parse a decoded absolute URI.
    /// </summary>
    /// <param name="value">The raw URI value.</param>
    /// <param name="uri">The parsed URI.</param>
    /// <returns><see langword="true"/> when parsing succeeds; otherwise <see langword="false"/>.</returns>
    private static bool TryCreateUri(string value, out Uri uri) =>
        Uri.TryCreate(WebUtility.HtmlDecode(value), UriKind.Absolute, out uri!);

    /// <summary>
    /// Gets the regex used to tokenize limited HTML fragments.
    /// </summary>
    /// <returns>The compiled tokenization regex.</returns>
    [GeneratedRegex(
        "(?is)(?<anchor><a\\b[^>]*href\\s*=\\s*[\"'](?<href>[^\"']+)[\"'][^>]*>(?<anchorText>.*?)</a>)|(?<break><br\\s*/?>)|(?<block></p\\s*>|</div\\s*>|</h\\d\\s*>)|(?<li><li\\b[^>]*>)|(?<tag><[^>]+>)|(?<text>[^<]+)")]
    private static partial Regex HtmlTokenRegex();

    /// <summary>
    /// Gets the regex used to detect plain-text URLs.
    /// </summary>
    /// <returns>The compiled URL regex.</returns>
    [GeneratedRegex("(?i)\\bhttps?://[^\\s<]+")]
    private static partial Regex UrlRegex();

    /// <summary>
    /// Gets the regex used to strip generic HTML tags.
    /// </summary>
    /// <returns>The compiled generic tag regex.</returns>
    [GeneratedRegex("(?is)<[^>]+>")]
    private static partial Regex GenericTagRegex();
}

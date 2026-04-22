using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DayScope.Views;

/// <summary>
/// Converts a constrained HTML fragment into WPF text inlines.
/// </summary>
public static partial class HtmlTextBlockRenderer
{
    /// <summary>
    /// Converts a constrained HTML fragment into plain text while preserving line breaks.
    /// </summary>
    /// <param name="html">The HTML fragment to convert.</param>
    /// <returns>The plain-text representation of the fragment.</returns>
    public static string ToPlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var segments = new List<string>();
        var currentLine = new System.Text.StringBuilder();
        var pendingBreaks = 0;

        foreach (Match tokenMatch in HtmlTokenRegex().Matches(html))
        {
            if (tokenMatch.Groups["anchor"].Success)
            {
                FlushPendingPlainTextBreaks(segments, currentLine, ref pendingBreaks);
                AppendPlainText(currentLine, tokenMatch.Groups["anchorText"].Value, fallbackText: tokenMatch.Groups["href"].Value);
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
                FlushPendingPlainTextBreaks(segments, currentLine, ref pendingBreaks);
                if (currentLine.Length == 0)
                {
                    currentLine.Append("- ");
                }

                continue;
            }

            if (!tokenMatch.Groups["text"].Success)
            {
                continue;
            }

            FlushPendingPlainTextBreaks(segments, currentLine, ref pendingBreaks);
            AppendPlainText(currentLine, tokenMatch.Groups["text"].Value);
        }

        FlushCurrentPlainTextLine(segments, currentLine);
        return string.Join(Environment.NewLine, TrimTrailingBlankLines(segments)).Trim();
    }

    /// <summary>
    /// Renders the provided HTML fragment into the target text block.
    /// </summary>
    /// <param name="textBlock">The text block that receives the rendered inlines.</param>
    /// <param name="html">The HTML fragment to render.</param>
    public static void Render(TextBlock textBlock, string? html)
    {
        ArgumentNullException.ThrowIfNull(textBlock);

        textBlock.Inlines.Clear();
        Render(textBlock.Inlines, html);
    }

    /// <summary>
    /// Renders the provided HTML fragment into a selectable rich text box.
    /// </summary>
    /// <param name="richTextBox">The rich text box that receives the rendered document.</param>
    /// <param name="html">The HTML fragment to render.</param>
    public static void Render(System.Windows.Controls.RichTextBox richTextBox, string? html)
    {
        ArgumentNullException.ThrowIfNull(richTextBox);

        var document = richTextBox.Document;
        document.Blocks.Clear();
        document.PagePadding = new Thickness(0);
        document.Background = System.Windows.Media.Brushes.Transparent;
        document.FontFamily = richTextBox.FontFamily;
        document.FontSize = richTextBox.FontSize;
        document.Foreground = richTextBox.Foreground;
        document.TextAlignment = TextAlignment.Left;

        var paragraph = new Paragraph
        {
            Margin = new Thickness(0)
        };
        document.Blocks.Add(paragraph);
        Render(paragraph.Inlines, html);
    }

    private static void Render(InlineCollection inlines, string? html)
    {
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
                FlushPendingBreaks(inlines, ref pendingBreaks, hasVisibleContent);
                AppendAnchor(
                    inlines,
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
                FlushPendingBreaks(inlines, ref pendingBreaks, hasVisibleContent);
                inlines.Add(new Run("- "));
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

            FlushPendingBreaks(inlines, ref pendingBreaks, hasVisibleContent);
            AppendTextWithAutoLinks(inlines, decodedText, ref hasVisibleContent);
        }
    }

    private static void FlushPendingBreaks(
        InlineCollection inlines,
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
            inlines.Add(new LineBreak());
        }

        pendingBreaks = 0;
    }

    private static void AppendAnchor(
        InlineCollection inlines,
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
            inlines.Add(CreateHyperlink(uri, displayText));
        }
        else
        {
            inlines.Add(new Run(displayText));
        }

        hasVisibleContent = true;
    }

    private static void AppendTextWithAutoLinks(
        InlineCollection inlines,
        string text,
        ref bool hasVisibleContent)
    {
        var cursor = 0;
        foreach (Match urlMatch in UrlRegex().Matches(text))
        {
            if (urlMatch.Index > cursor)
            {
                inlines.Add(new Run(text[cursor..urlMatch.Index]));
                hasVisibleContent = true;
            }

            var rawUrl = urlMatch.Value;
            var url = rawUrl.TrimEnd('.', ',', ';', ')', ']', '}');
            var trailingSuffix = rawUrl[url.Length..];
            if (TryCreateUri(url, out var uri))
            {
                inlines.Add(CreateHyperlink(uri, url));
            }
            else
            {
                inlines.Add(new Run(rawUrl));
            }

            if (trailingSuffix.Length > 0)
            {
                inlines.Add(new Run(trailingSuffix));
            }

            cursor = urlMatch.Index + urlMatch.Length;
            hasVisibleContent = true;
        }

        if (cursor < text.Length)
        {
            inlines.Add(new Run(text[cursor..]));
            hasVisibleContent = true;
        }
    }

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

    private static void OnHyperlinkClick(object sender, RoutedEventArgs args)
    {
        if (sender is not Hyperlink { NavigateUri: { } uri })
        {
            return;
        }

        HtmlTextBlockLinkNavigator.Open(uri);
    }

    private static string DecodeText(string text) =>
        WebUtility.HtmlDecode(text)
            .Replace('\u00A0', ' ');

    private static string StripTags(string text) => GenericTagRegex().Replace(text, string.Empty);

    private static void FlushPendingPlainTextBreaks(
        List<string> segments,
        System.Text.StringBuilder currentLine,
        ref int pendingBreaks)
    {
        if (pendingBreaks <= 0)
        {
            return;
        }

        FlushCurrentPlainTextLine(segments, currentLine);
        for (var index = 1; index < pendingBreaks; index++)
        {
            segments.Add(string.Empty);
        }

        pendingBreaks = 0;
    }

    private static void FlushCurrentPlainTextLine(
        List<string> segments,
        System.Text.StringBuilder currentLine)
    {
        if (currentLine.Length == 0)
        {
            return;
        }

        segments.Add(currentLine.ToString().Trim());
        currentLine.Clear();
    }

    private static void AppendPlainText(
        System.Text.StringBuilder currentLine,
        string htmlFragment,
        string? fallbackText = null)
    {
        var plainText = DecodeText(StripTags(htmlFragment));
        if (string.IsNullOrWhiteSpace(plainText))
        {
            plainText = fallbackText is null ? string.Empty : DecodeText(fallbackText);
        }

        if (string.IsNullOrWhiteSpace(plainText))
        {
            return;
        }

        currentLine.Append(plainText);
    }

    private static string[] TrimTrailingBlankLines(List<string> segments)
    {
        var lastVisibleIndex = segments.Count - 1;
        while (lastVisibleIndex >= 0 && string.IsNullOrWhiteSpace(segments[lastVisibleIndex]))
        {
            lastVisibleIndex--;
        }

        return lastVisibleIndex < 0
            ? []
            : [.. segments.Take(lastVisibleIndex + 1)];
    }

    private static bool TryCreateUri(string value, out Uri uri) =>
        Uri.TryCreate(WebUtility.HtmlDecode(value), UriKind.Absolute, out uri!);

    [GeneratedRegex(
        "(?is)(?<anchor><a\\b[^>]*href\\s*=\\s*[\"'](?<href>[^\"']+)[\"'][^>]*>(?<anchorText>.*?)</a>)|(?<break><br\\s*/?>)|(?<block></p\\s*>|</div\\s*>|</h\\d\\s*>)|(?<li><li\\b[^>]*>)|(?<tag><[^>]+>)|(?<text>[^<]+)")]
    private static partial Regex HtmlTokenRegex();

    [GeneratedRegex("(?i)\\bhttps?://[^\\s<]+")]
    private static partial Regex UrlRegex();

    [GeneratedRegex("(?is)<[^>]+>")]
    private static partial Regex GenericTagRegex();
}

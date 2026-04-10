using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DayScope.Views;

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

    public static string? GetHtml(DependencyObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return (string?)obj.GetValue(HtmlProperty);
    }

    public static void SetHtml(DependencyObject obj, string? value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(HtmlProperty, value);
    }

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

    private static string DecodeText(string text) =>
        WebUtility.HtmlDecode(text)
            .Replace('\u00A0', ' ');

    private static string StripTags(string text) => GenericTagRegex().Replace(text, string.Empty);

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

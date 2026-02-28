using System.Text.RegularExpressions;

namespace WorkSlack.Helpers;

/// <summary>
/// Converts simple Slack-style markdown to HTML for display.
/// Supports bold, italic, strikethrough, inline code, code blocks, and blockquotes.
/// </summary>
internal static partial class MessageFormatter
{
    /// <summary>
    /// Renders Slack-style markdown content to safe HTML.
    /// </summary>
    public static string ToHtml(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var html = System.Net.WebUtility.HtmlEncode(content);

        // Code blocks: ```code```
        html = CodeBlockRegex().Replace(html, "<pre class=\"msg-codeblock\"><code>$1</code></pre>");

        // Inline code: `code`
        html = InlineCodeRegex().Replace(html, "<code class=\"msg-code\">$1</code>");

        // Bold: *text*
        html = BoldRegex().Replace(html, "<strong>$1</strong>");

        // Italic: _text_
        html = ItalicRegex().Replace(html, "<em>$1</em>");

        // Strikethrough: ~text~
        html = StrikethroughRegex().Replace(html, "<del>$1</del>");

        // Blockquote: > text (at line start)
        html = BlockquoteRegex().Replace(html, "<blockquote class=\"msg-blockquote\">$1</blockquote>");

        // URLs
        html = UrlRegex().Replace(html, "<a href=\"$0\" target=\"_blank\" rel=\"noopener noreferrer\">$0</a>");

        // @mentions
        html = MentionRegex().Replace(html, "<span class=\"msg-mention\">$0</span>");

        // Newlines
        html = html.Replace("\n", "<br />");

        return html;
    }

    [GeneratedRegex(@"```([\s\S]*?)```")]
    private static partial Regex CodeBlockRegex();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"\*([^\*]+)\*")]
    private static partial Regex BoldRegex();

    [GeneratedRegex(@"(?<!\w)_([^_]+)_(?!\w)")]
    private static partial Regex ItalicRegex();

    [GeneratedRegex(@"~([^~]+)~")]
    private static partial Regex StrikethroughRegex();

    [GeneratedRegex(@"^&gt;\s?(.+)$", RegexOptions.Multiline)]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"https?://[^\s<&]+")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"@\w+")]
    private static partial Regex MentionRegex();
}

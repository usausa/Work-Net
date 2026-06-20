namespace TuiAgentSampleCore;

using System.Text;

/// <summary>
/// アシスタント本文の簡易 Markdown を各 TUI 向けに変換するヘルパー。
/// <list type="bullet">
/// <item><see cref="ToConsoleMarkup"/>: Spectre.Console / SharpConsoleUI 互換の角括弧マークアップ。</item>
/// <item><see cref="ToPlainText"/>: 装飾記号を除いた素のテキスト (Terminal.Gui / Consolonia 用)。</item>
/// </list>
/// 対応記法は見出し (#, ##)・箇条書き (-, *)・引用 (&gt;)・**太字**・`インラインコード`・``` フェンス ``` のみ。
/// </summary>
public static class MarkupFormatter
{
    private const string Fence = "```";

    /// <param name="useBold">
    /// 見出しと <c>**太字**</c> を強調 (bold) するか。実ターミナル (Spectre) では
    /// 「bold + 明るい白」が配色によって背景と同化するため、その場合は <see langword="false"/> を渡す。
    /// </param>
    /// <param name="mutedColor">箇条書き <c>-</c> や引用 <c>|</c> など、本文以外の記号に使う色。</param>
    public static string ToConsoleMarkup(string markdown, bool useBold = true, string mutedColor = "silver")
    {
        var builder = new StringBuilder();
        var inFence = false;
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var heading = useBold ? "bold white" : "white";

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.StartsWith(Fence, StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            // 応答の本文は白。見出しと **太字** は useBold で強調の有無を切り替える。
            // コードはアクセント (aqua)、箇条書き等の記号は mutedColor とし、本文の白と分ける。
            if (inFence)
            {
                builder.Append("[aqua]  ").Append(Escape(line)).Append("[/]");
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                builder.Append('[').Append(heading).Append(']').Append(Inline(line[3..], useBold)).Append("[/]");
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                builder.Append('[').Append(heading).Append(']').Append(Inline(line[2..], useBold)).Append("[/]");
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                builder.Append("  [").Append(mutedColor).Append("]-[/] [white]").Append(Inline(line[2..], useBold)).Append("[/]");
            }
            else if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                builder.Append('[').Append(mutedColor).Append("]|[/] [white italic]").Append(Inline(line[2..], useBold)).Append("[/]");
            }
            else if (line.Length > 0)
            {
                builder.Append("[white]").Append(Inline(line, useBold)).Append("[/]");
            }

            if (i < lines.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    public static string ToPlainText(string markdown)
    {
        var builder = new StringBuilder();
        var inFence = false;
        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.StartsWith(Fence, StringComparison.Ordinal))
            {
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                builder.Append("    ").Append(line);
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                builder.Append(StripInline(line[3..]));
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                builder.Append(StripInline(line[2..]));
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                builder.Append("  - ").Append(StripInline(line[2..]));
            }
            else if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                builder.Append("| ").Append(StripInline(line[2..]));
            }
            else
            {
                builder.Append(StripInline(line));
            }

            if (i < lines.Length - 1)
            {
                builder.Append('\n');
            }
        }

        return builder.ToString();
    }

    private static string Inline(string text, bool useBold)
    {
        var builder = new StringBuilder();
        var i = 0;
        while (i < text.Length)
        {
            if ((i + 1) < text.Length && text[i] == '*' && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > 0)
                {
                    // useBold:false のときは囲みの白を継承させ、bold は付けない (背景同化の回避)。
                    var segment = Escape(text[(i + 2)..end]);
                    builder.Append(useBold ? $"[bold]{segment}[/]" : segment);
                    i = end + 2;
                    continue;
                }
            }

            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > 0)
                {
                    builder.Append("[aqua]").Append(Escape(text[(i + 1)..end])).Append("[/]");
                    i = end + 1;
                    continue;
                }
            }

            builder.Append(EscapeChar(text[i]));
            i++;
        }

        return builder.ToString();
    }

    private static string StripInline(string text) =>
        text.Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("`", string.Empty, StringComparison.Ordinal);

    /// <summary>角括弧をエスケープし、マークアップ中で安全に表示できる文字列にする。</summary>
    public static string Escape(string text) =>
        text.Replace("[", "[[", StringComparison.Ordinal)
            .Replace("]", "]]", StringComparison.Ordinal);

    private static string EscapeChar(char c) => c switch
    {
        '[' => "[[",
        ']' => "]]",
        _ => c.ToString()
    };
}

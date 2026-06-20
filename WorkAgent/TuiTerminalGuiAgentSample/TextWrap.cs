namespace TuiTerminalGuiAgentSample;

using System.Text;

/// <summary>
/// 表示幅 (CJK は 2 桁) を考慮した簡易折り返し。Terminal.Gui の Label は
/// 自前で改行位置を持たないため、ここで明示的に行へ分割する。
/// </summary>
internal static class TextWrap
{
    public static IReadOnlyList<string> Wrap(string text, int width)
    {
        var result = new List<string>();
        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            WrapLine(line, width, result);
        }

        return result;
    }

    private static void WrapLine(string line, int width, List<string> output)
    {
        var current = new StringBuilder();
        var currentWidth = 0;

        foreach (var ch in line)
        {
            var w = CharWidth(ch);
            if ((currentWidth + w) > width && current.Length > 0)
            {
                output.Add(current.ToString());
                current.Clear();
                currentWidth = 0;
            }

            current.Append(ch);
            currentWidth += w;
        }

        output.Add(current.ToString());
    }

    // East Asian Wide/Fullwidth のおおまかな範囲を 2 桁として扱う。
    private static int CharWidth(char c) =>
        c is (>= 'ᄀ' and <= 'ᅟ')   // Hangul Jamo
            or (>= '⺀' and <= '〾')  // CJK Radicals / Kangxi / 記号
            or (>= 'ぁ' and <= '㏿')  // かな / CJK 記号 / 全角単位
            or (>= '㐀' and <= '䶿')  // CJK 拡張 A
            or (>= '一' and <= '鿿')  // CJK 統合漢字
            or (>= 'ꀀ' and <= '꓏')  // Yi
            or (>= '가' and <= '힣')  // ハングル音節
            or (>= '豈' and <= '﫿')  // CJK 互換漢字
            or (>= '︰' and <= '﹏')  // CJK 互換記号
            or (>= '＀' and <= '｠')  // 全角英数記号
            or (>= '￠' and <= '￦')  // 全角通貨記号
                ? 2
                : 1;
}

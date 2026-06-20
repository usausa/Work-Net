namespace TuiAgentSampleCore;

using System.Globalization;
using System.Text;

/// <summary>
/// 応答本文を「自然なストリーミング」に見せるための分割器。
/// 英数字は単語 (末尾の空白を含む) 単位、CJK 文字は 1 文字単位で刻む。
/// </summary>
internal static class ResponseTokenizer
{
    public static IEnumerable<string> Tokenize(string text)
    {
        var builder = new StringBuilder();
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            var element = enumerator.GetTextElement();

            if (IsCjk(element) || element is "\n")
            {
                if (builder.Length > 0)
                {
                    yield return builder.ToString();
                    builder.Clear();
                }

                yield return element;
                continue;
            }

            builder.Append(element);
            if (element is " " or "\t")
            {
                yield return builder.ToString();
                builder.Clear();
            }
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private static bool IsCjk(string element)
    {
        if (element.Length == 0)
        {
            return false;
        }

        var rune = char.ConvertToUtf32(element, 0);
        return rune is (>= 0x3000 and <= 0x9FFF) or (>= 0xFF00 and <= 0xFFEF);
    }
}

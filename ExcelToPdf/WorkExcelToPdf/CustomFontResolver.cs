using PdfSharp.Fonts;
using System.Reflection;

namespace WorkExcelToPdf;

/// <summary>
/// カスタムフォントリゾルバー
/// 日本語フォントが見つからない場合にipaexm.ttfにフォールバックする
/// </summary>
public class CustomFontResolver : IFontResolver
{
    public string DefaultFontName => "IPAexMincho";

    public byte[]? GetFont(string faceName)
    {
        // 日本語フォント（ＭＳ Ｐゴシック、MS PGothic等）の場合
        if (faceName.Contains("ゴシック") || faceName.Contains("Gothic") || 
            faceName.Contains("Pゴシック") || faceName.Contains("PGothic") ||
            faceName.Contains("明朝") || faceName.Contains("Mincho"))
        {
            return LoadFontData("ipaexm.ttf");
        }

        // デフォルトでipaexm.ttfを使用
        return LoadFontData("ipaexm.ttf");
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        // 日本語フォントの場合
        if (familyName.Contains("ゴシック") || familyName.Contains("Gothic") || 
            familyName.Contains("Pゴシック") || familyName.Contains("PGothic") ||
            familyName.Contains("明朝") || familyName.Contains("Mincho") ||
            familyName.Contains("ＭＳ") || familyName.Contains("MS"))
        {
            return new FontResolverInfo("IPAexMincho");
        }

        // その他のフォントもipaexm.ttfにフォールバック
        return new FontResolverInfo("IPAexMincho");
    }

    /// <summary>
    /// フォントファイルを読み込む
    /// </summary>
    private byte[]? LoadFontData(string fontFileName)
    {
        try
        {
            // プロジェクトのルートディレクトリからフォントを読み込む
            var fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fontFileName);
            
            if (File.Exists(fontPath))
            {
                Console.WriteLine($"フォントを読み込みました: {fontPath}");
                return File.ReadAllBytes(fontPath);
            }

            // カレントディレクトリから試す
            fontPath = Path.Combine(Directory.GetCurrentDirectory(), fontFileName);
            if (File.Exists(fontPath))
            {
                Console.WriteLine($"フォントを読み込みました: {fontPath}");
                return File.ReadAllBytes(fontPath);
            }

            // 親ディレクトリから試す
            fontPath = Path.Combine(Directory.GetCurrentDirectory(), "..", fontFileName);
            if (File.Exists(fontPath))
            {
                Console.WriteLine($"フォントを読み込みました: {fontPath}");
                return File.ReadAllBytes(fontPath);
            }

            Console.WriteLine($"警告: フォントファイルが見つかりません: {fontFileName}");
            Console.WriteLine($"探したパス:");
            Console.WriteLine($"  - {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fontFileName)}");
            Console.WriteLine($"  - {Path.Combine(Directory.GetCurrentDirectory(), fontFileName)}");
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"フォント読み込みエラー: {ex.Message}");
            return null;
        }
    }
}

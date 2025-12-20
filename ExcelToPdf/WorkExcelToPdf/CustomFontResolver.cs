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
        // すべてのフェイス名（Regular, Bold, Italic, BoldItalic）に対して
        // 同じIPAフォントを返す
        // PDFSharpが疑似Bold/Italicを自動生成する
        
        // フェイス名からベース名を抽出（-Bold, -Italic等を削除）
        var baseFaceName = faceName.Split('-')[0];
        
        Console.WriteLine($"フォント要求: {faceName} (ベース: {baseFaceName})");
        
        // すべてのスタイルに対してipaexm.ttfを返す
        return LoadFontData("ipaexm.ttf");
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic)
    {
        // IPAフォントは単一ウェイトなので、Bold/Italicは疑似的に生成される
        // PDFSharpに疑似Bold/Italicを生成させるため、スタイル情報を含めたフォント名を返す
        
        string faceName = "IPAexMincho";
        
        // Bold/Italicの場合、異なるフェイス名を返すことで、PDFSharpが疑似スタイルを適用する
        if (bold && italic)
        {
            faceName = "IPAexMincho-BoldItalic";
        }
        else if (bold)
        {
            faceName = "IPAexMincho-Bold";
        }
        else if (italic)
        {
            faceName = "IPAexMincho-Italic";
        }
        
        // 実際のフォントデータは同じものを使用するが、フェイス名を変えることで
        // PDFSharpが疑似Bold/Italicを生成する
        return new FontResolverInfo(faceName);
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

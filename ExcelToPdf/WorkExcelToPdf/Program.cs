namespace WorkExcelToPdf;

using ClosedXML.Excel;

public static class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Excel to PDF Converter ===");
            Console.WriteLine();

            // Excelファイルのパスを指定
            string excelFilePath = args.Length > 0 ? args[0] : "Template.xlsx";
            string pdfFilePath = args.Length > 1 ? args[1] : "output.pdf";

            if (!System.IO.File.Exists(excelFilePath))
            {
                Console.WriteLine($"ファイルが見つかりません: {excelFilePath}");
                Console.WriteLine($"カレントディレクトリ: {Directory.GetCurrentDirectory()}");
                Console.WriteLine();
                Console.WriteLine("使い方: dotnet run [Excelファイル] [出力PDFファイル]");
                return;
            }

            Console.WriteLine($"Excelファイル: {excelFilePath}");
            Console.WriteLine();
            Console.WriteLine("モードを選択してください:");
            Console.WriteLine("  1: PDF生成モード（通常）");
            Console.WriteLine("  2: デバッグモード（Excel詳細分析）");
            Console.WriteLine("  Enter: PDF生成モード（デフォルト）");
            Console.Write("> ");

            var input = Console.ReadLine();
            Console.WriteLine();

            if (input == "2")
            {
                // デバッグモード: Excelファイルの詳細分析
                Console.WriteLine("=== デバッグモード: Excel詳細分析 ===");
                ExcelDebugTool.InspectExcelFile(excelFilePath);

                Console.WriteLine("\nPDF生成も実行しますか? (y/n)");
                var continueKey = Console.ReadKey();
                Console.WriteLine();

                if (continueKey.Key != ConsoleKey.Y)
                {
                    Console.WriteLine("プログラムを終了します。");
                    return;
                }

                Console.Clear();
            }

            // PDF生成モード
            Console.WriteLine("=== PDF生成モード ===");
            Console.WriteLine($"入力ファイル: {excelFilePath}");
            Console.WriteLine($"出力ファイル: {pdfFilePath}");
            Console.WriteLine();

            // PDF生成
            var readerEx = new ExcelBorderReaderEx();
            var sheetInfoEx = readerEx.ReadSheet(excelFilePath);

            var pdfGenerator = new PdfGenerator();
            pdfGenerator.GeneratePdf(sheetInfoEx, pdfFilePath);

            Console.WriteLine();
            Console.WriteLine(new string('=', 100));
            Console.WriteLine("✓ 処理が完了しました！");
            Console.WriteLine($"  入力: {excelFilePath}");
            Console.WriteLine($"  出力: {pdfFilePath}");
            Console.WriteLine(new string('=', 100));
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"エラーが発生しました: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine();
        Console.WriteLine("何かキーを押すと終了します...");
        Console.ReadLine();
    }

    /// <summary>
    /// 罫線の統計情報を分析
    /// </summary>
    private static Dictionary<string, int> AnalyzeBorders(List<CellInfo> cells)
    {
        var stats = new Dictionary<string, int>();

        foreach (var cell in cells)
        {
            var borders = new[] {
                cell.Border.Top,
                cell.Border.Bottom,
                cell.Border.Left,
                cell.Border.Right,
                cell.Border.DiagonalUp,
                cell.Border.DiagonalDown
            };

            foreach (var border in borders.Where(b => b.HasBorder))
            {
                if (!stats.ContainsKey(border.LineStyle))
                {
                    stats[border.LineStyle] = 0;
                }
                stats[border.LineStyle]++;
            }
        }

        return stats;
    }
}

/// <summary>
/// シート全体の情報を保持するクラス
/// </summary>
public class SheetInfo
{
    public string SheetName { get; set; } = string.Empty;
    public string UsedRange { get; set; } = string.Empty;
    public List<CellInfo> Cells { get; set; } = new();
}

/// <summary>
/// セルの情報を保持するクラス
/// </summary>
public class CellInfo
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string CellAddress { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public double Width { get; set; }
    public double Height { get; set; }
    public CellBorderInfo Border { get; set; } = new();
}

/// <summary>
/// セルの罫線情報を保持するクラス
/// </summary>
public class CellBorderInfo
{
    public BorderSideInfo Top { get; set; } = new();
    public BorderSideInfo Bottom { get; set; } = new();
    public BorderSideInfo Left { get; set; } = new();
    public BorderSideInfo Right { get; set; } = new();
    public BorderSideInfo DiagonalUp { get; set; } = new();
    public BorderSideInfo DiagonalDown { get; set; } = new();
}

/// <summary>
/// 罫線の一辺の情報を保持するクラス
/// </summary>
public class BorderSideInfo
{
    public bool HasBorder { get; set; }
    public string LineStyle { get; set; } = "なし";
    public string Color { get; set; } = "#000000";
    public double Width { get; set; }

    public override string ToString()
    {
        return HasBorder
            ? $"{LineStyle}(色:{Color}, 幅:{Width}pt)"
            : "なし";
    }
}

/// <summary>
/// Excel罫線リーダー
/// </summary>
public class ExcelBorderReader
{
    /// <summary>
    /// Excelファイルから罫線情報とセル情報を取得
    /// </summary>
    public SheetInfo ReadSheetWithBorders(string filePath, string? sheetName = null)
    {
        var sheetInfo = new SheetInfo();

        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = string.IsNullOrEmpty(sheetName)
                ? workbook.Worksheet(1)
                : workbook.Worksheet(sheetName);

            sheetInfo.SheetName = worksheet.Name;

            var usedRange = worksheet.RangeUsed();
            if (usedRange == null)
            {
                Console.WriteLine("使用されているセルがありません。");
                return sheetInfo;
            }

            sheetInfo.UsedRange = usedRange.RangeAddress.ToString();

            // 使用されている範囲のすべてのセルをチェック
            foreach (var cell in usedRange.Cells())
            {
                var cellInfo = ExtractCellInfo(cell);

                // 罫線が設定されているセル、または値が入っているセルを追加
                if (HasAnyBorder(cellInfo.Border) || !string.IsNullOrWhiteSpace(cellInfo.Value))
                {
                    sheetInfo.Cells.Add(cellInfo);
                }
            }
        }

        return sheetInfo;
    }

    /// <summary>
    /// セルから情報を抽出
    /// </summary>
    private CellInfo ExtractCellInfo(IXLCell cell)
    {
        var cellInfo = new CellInfo
        {
            Row = cell.Address.RowNumber,
            Column = cell.Address.ColumnNumber,
            CellAddress = cell.Address.ToString(),
            Value = cell.GetString(),
            Width = cell.WorksheetColumn().Width * 7.5, // Excelの列幅をピクセルに概算変換
            Height = cell.WorksheetRow().Height * 1.33, // Excelの行高をピクセルに概算変換
            Border = ExtractBorderInfo(cell)
        };

        return cellInfo;
    }

    /// <summary>
    /// セルから罫線情報を抽出
    /// </summary>
    private CellBorderInfo ExtractBorderInfo(IXLCell cell)
    {
        var borderInfo = new CellBorderInfo();
        var style = cell.Style;

        // 各辺の罫線情報を取得
        borderInfo.Top = GetBorderSideInfo(style.Border.TopBorder, style.Border.TopBorderColor);
        borderInfo.Bottom = GetBorderSideInfo(style.Border.BottomBorder, style.Border.BottomBorderColor);
        borderInfo.Left = GetBorderSideInfo(style.Border.LeftBorder, style.Border.LeftBorderColor);
        borderInfo.Right = GetBorderSideInfo(style.Border.RightBorder, style.Border.RightBorderColor);

        // 斜め線はbool型なので特別な処理
        borderInfo.DiagonalUp = GetDiagonalBorderInfo(style.Border.DiagonalUp, style.Border.DiagonalBorder, style.Border.DiagonalBorderColor);
        borderInfo.DiagonalDown = GetDiagonalBorderInfo(style.Border.DiagonalDown, style.Border.DiagonalBorder, style.Border.DiagonalBorderColor);

        return borderInfo;
    }

    /// <summary>
    /// 罫線の一辺の情報を取得
    /// </summary>
    private BorderSideInfo GetBorderSideInfo(XLBorderStyleValues borderStyle, XLColor borderColor)
    {
        var info = new BorderSideInfo
        {
            HasBorder = borderStyle != XLBorderStyleValues.None,
            LineStyle = ConvertBorderStyle(borderStyle),
            Color = GetColorHex(borderColor),
            Width = GetBorderWidth(borderStyle)
        };

        return info;
    }

    /// <summary>
    /// 斜め線の情報を取得
    /// </summary>
    private BorderSideInfo GetDiagonalBorderInfo(bool hasDiagonal, XLBorderStyleValues borderStyle, XLColor borderColor)
    {
        if (!hasDiagonal)
        {
            return new BorderSideInfo { HasBorder = false };
        }

        var info = new BorderSideInfo
        {
            HasBorder = true,
            LineStyle = ConvertBorderStyle(borderStyle),
            Color = GetColorHex(borderColor),
            Width = GetBorderWidth(borderStyle)
        };

        return info;
    }

    /// <summary>
    /// 色情報を16進数文字列に変換
    /// </summary>
    private string GetColorHex(XLColor color)
    {
        if (color == null)
            return "#000000";

        try
        {
            var colorType = color.ColorType;
            if (colorType == XLColorType.Color)
            {
                return $"#{color.Color.ToArgb() & 0xFFFFFF:X6}";
            }
            return "#000000";
        }
        catch
        {
            return "#000000";
        }
    }

    /// <summary>
    /// Excel罫線スタイルを文字列に変換
    /// </summary>
    private string ConvertBorderStyle(XLBorderStyleValues style)
    {
        return style switch
        {
            XLBorderStyleValues.None => "なし",
            XLBorderStyleValues.Thin => "細線",
            XLBorderStyleValues.Medium => "中線",
            XLBorderStyleValues.Thick => "太線",
            XLBorderStyleValues.Double => "二重線",
            XLBorderStyleValues.Dotted => "点線",
            XLBorderStyleValues.Dashed => "破線",
            XLBorderStyleValues.DashDot => "一点鎖線",
            XLBorderStyleValues.DashDotDot => "二点鎖線",
            XLBorderStyleValues.SlantDashDot => "斜線一点鎖線",
            XLBorderStyleValues.Hair => "極細線",
            XLBorderStyleValues.MediumDashed => "中破線",
            XLBorderStyleValues.MediumDashDot => "中一点鎖線",
            XLBorderStyleValues.MediumDashDotDot => "中二点鎖線",
            _ => style.ToString()
        };
    }

    /// <summary>
    /// 罫線スタイルから幅を推定（ポイント単位）
    /// </summary>
    private double GetBorderWidth(XLBorderStyleValues style)
    {
        return style switch
        {
            XLBorderStyleValues.None => 0,
            XLBorderStyleValues.Hair => 0.25,
            XLBorderStyleValues.Thin => 0.5,
            XLBorderStyleValues.Dotted => 0.5,
            XLBorderStyleValues.Dashed => 0.5,
            XLBorderStyleValues.DashDot => 0.5,
            XLBorderStyleValues.DashDotDot => 0.5,
            XLBorderStyleValues.Medium => 1.5,
            XLBorderStyleValues.MediumDashed => 1.5,
            XLBorderStyleValues.MediumDashDot => 1.5,
            XLBorderStyleValues.MediumDashDotDot => 1.5,
            XLBorderStyleValues.Thick => 2.5,
            XLBorderStyleValues.Double => 2.0,
            XLBorderStyleValues.SlantDashDot => 1.5,
            _ => 0.5
        };
    }

    /// <summary>
    /// 罫線が設定されているかチェック
    /// </summary>
    private bool HasAnyBorder(CellBorderInfo borderInfo)
    {
        return borderInfo.Top.HasBorder ||
               borderInfo.Bottom.HasBorder ||
               borderInfo.Left.HasBorder ||
               borderInfo.Right.HasBorder ||
               borderInfo.DiagonalUp.HasBorder ||
               borderInfo.DiagonalDown.HasBorder;
    }
}

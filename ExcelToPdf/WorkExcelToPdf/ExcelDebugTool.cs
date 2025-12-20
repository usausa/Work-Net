using ClosedXML.Excel;

namespace WorkExcelToPdf;

/// <summary>
/// Excelファイルの詳細情報を表示するデバッグツール
/// </summary>
public class ExcelDebugTool
{
    public static void InspectExcelFile(string filePath)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine($"Excel詳細分析: {filePath}");
        Console.WriteLine(new string('=', 80));

        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheet(1);

        Console.WriteLine($"\nシート名: {worksheet.Name}");

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null)
        {
            Console.WriteLine("使用されているセルがありません。");
            return;
        }

        Console.WriteLine($"使用範囲: {usedRange.RangeAddress}");
        Console.WriteLine($"開始: 行{usedRange.FirstRow().RowNumber()}, 列{usedRange.FirstColumn().ColumnNumber()} ({GetColumnName(usedRange.FirstColumn().ColumnNumber())})");
        Console.WriteLine($"終了: 行{usedRange.LastRow().RowNumber()}, 列{usedRange.LastColumn().ColumnNumber()} ({GetColumnName(usedRange.LastColumn().ColumnNumber())})");

        Console.WriteLine("\n" + new string('-', 80));
        Console.WriteLine("すべてのセルの詳細情報:");
        Console.WriteLine(new string('-', 80));

        int totalCells = 0;
        int cellsWithValue = 0;
        int cellsWithBorder = 0;
        int cellsWithBackground = 0;
        int cellsWithBold = 0;
        int cellsWithItalic = 0;
        int emptyCellsNoBorder = 0;

        for (int row = usedRange.FirstRow().RowNumber(); row <= usedRange.LastRow().RowNumber(); row++)
        {
            for (int col = usedRange.FirstColumn().ColumnNumber(); col <= usedRange.LastColumn().ColumnNumber(); col++)
            {
                var cell = worksheet.Cell(row, col);
                var address = cell.Address.ToString();
                var value = cell.GetString();
                var isEmpty = string.IsNullOrEmpty(value);

                totalCells++;

                // 罫線情報
                var style = cell.Style;
                bool hasTopBorder = style.Border.TopBorder != XLBorderStyleValues.None;
                bool hasBottomBorder = style.Border.BottomBorder != XLBorderStyleValues.None;
                bool hasLeftBorder = style.Border.LeftBorder != XLBorderStyleValues.None;
                bool hasRightBorder = style.Border.RightBorder != XLBorderStyleValues.None;
                bool hasBorder = hasTopBorder || hasBottomBorder || hasLeftBorder || hasRightBorder;

                // スタイル情報
                bool hasBold = style.Font.Bold;
                bool hasItalic = style.Font.Italic;
                // 背景色
                var bgColorInfo = "なし";
                if (style.Fill.PatternType != XLFillPatternValues.None)
                {
                    var bgColor = GetColorInfo(style.Fill.BackgroundColor);
                    var patternColor = GetColorInfo(style.Fill.PatternColor);
                    
                    if (bgColor != "なし" && bgColor != "Indexed (#FFFFFF)")
                    {
                        bgColorInfo = $"{bgColor} (Pattern:{style.Fill.PatternType})";
                    }
                    else if (patternColor != "なし" && patternColor != "Indexed (#FFFFFF)")
                    {
                        bgColorInfo = $"PatternColor:{patternColor} (Pattern:{style.Fill.PatternType})";
                    }
                    else
                    {
                        bgColorInfo = $"Pattern:{style.Fill.PatternType}, BG:{bgColor}, Pattern:{patternColor}";
                    }
                }
                
                Console.WriteLine($"  背景色: {bgColorInfo}");

                // 罫線
                if (hasBorder)
                {
                    Console.WriteLine($"  罫線:");
                    if (hasTopBorder)
                        Console.WriteLine($"    上: {style.Border.TopBorder} (色:{GetColorInfo(style.Border.TopBorderColor)})");
                    if (hasBottomBorder)
                        Console.WriteLine($"    下: {style.Border.BottomBorder} (色:{GetColorInfo(style.Border.BottomBorderColor)})");
                    if (hasLeftBorder)
                        Console.WriteLine($"    左: {style.Border.LeftBorder} (色:{GetColorInfo(style.Border.LeftBorderColor)})");
                    if (hasRightBorder)
                        Console.WriteLine($"    右: {style.Border.RightBorder} (色:{GetColorInfo(style.Border.RightBorderColor)})");
                }
                else
                {
                    Console.WriteLine($"  罫線: なし");
                }
            }
        }

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("分析サマリー:");
        Console.WriteLine($"  総セル数: {totalCells}");
        Console.WriteLine($"  値があるセル: {cellsWithValue}");
        Console.WriteLine($"  罫線があるセル: {cellsWithBorder}");
        Console.WriteLine($"  背景色があるセル: {cellsWithBackground}");
        Console.WriteLine($"  太字のセル: {cellsWithBold}");
        Console.WriteLine($"  斜体のセル: {cellsWithItalic}");
        Console.WriteLine($"  空で罫線なしのセル: {emptyCellsNoBorder}");
        Console.WriteLine(new string('=', 80));

        // 特定セルの強制チェック
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("特定セルの強制チェック:");
        Console.WriteLine(new string('=', 80));

        var specificCells = new[] { "D1", "A3", "B3", "C3", "D3", "E3", "F3", "G3", "A4", "B4", "C4", "A5", "A8", "D6", "E6" };
        foreach (var cellAddress in specificCells)
        {
            try
            {
                var cell = worksheet.Cell(cellAddress);
                var value = cell.GetString();
                var style = cell.Style;

                Console.WriteLine($"\n{cellAddress}:");
                Console.WriteLine($"  値: '{value}' (IsEmpty: {string.IsNullOrEmpty(value)})");
                Console.WriteLine($"  データ型: {cell.DataType}");
                Console.WriteLine($"  太字: {style.Font.Bold}");
                Console.WriteLine($"  斜体: {style.Font.Italic}");
                Console.WriteLine($"  下線: {style.Font.Underline}");
                Console.WriteLine($"  取り消し線: {style.Font.Strikethrough}");
                Console.WriteLine($"  背景色: {GetColorInfo(style.Fill.BackgroundColor)}");
                Console.WriteLine($"  水平アライメント: {style.Alignment.Horizontal}");
                Console.WriteLine($"  罫線:");
                Console.WriteLine($"    上: {style.Border.TopBorder}");
                Console.WriteLine($"    下: {style.Border.BottomBorder}");
                Console.WriteLine($"    左: {style.Border.LeftBorder}");
                Console.WriteLine($"    右: {style.Border.RightBorder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n{cellAddress}: エラー - {ex.Message}");
            }
        }

        Console.WriteLine("\n" + new string('=', 80));
    }

    private static string GetColumnName(int columnNumber)
    {
        string columnName = "";
        while (columnNumber > 0)
        {
            int modulo = (columnNumber - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            columnNumber = (columnNumber - modulo) / 26;
        }
        return columnName;
    }

    private static string GetColorInfo(XLColor? color)
    {
        if (color == null)
            return "なし";

        try
        {
            var colorType = color.ColorType;
            
            // Colorタイプ（直接RGB指定）
            if (colorType == XLColorType.Color)
            {
                var argb = color.Color.ToArgb() & 0xFFFFFF;
                return $"#{argb:X6}";
            }
            
            // Indexedタイプ（インデックス色）
            if (colorType == XLColorType.Indexed)
            {
                try
                {
                    var indexedColor = color.Color;
                    var argb = indexedColor.ToArgb() & 0xFFFFFF;
                    return $"Indexed (#{argb:X6})";
                }
                catch
                {
                    return "Indexed";
                }
            }
            
            // Themeタイプ（テーマ色）
            if (colorType == XLColorType.Theme)
            {
                try
                {
                    var themeColor = color.Color;
                    var argb = themeColor.ToArgb() & 0xFFFFFF;
                    return $"Theme (#{argb:X6})";
                }
                catch
                {
                    return $"Theme";
                }
            }
            
            return colorType.ToString();
        }
        catch
        {
            return "エラー";
        }
    }
}

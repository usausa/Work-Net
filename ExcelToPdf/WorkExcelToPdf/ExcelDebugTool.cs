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

                if (!isEmpty) cellsWithValue++;
                if (hasBorder) cellsWithBorder++;
                if (isEmpty && !hasBorder) emptyCellsNoBorder++;

                // すべてのセルを表示（A4セルを見逃さないため）
                Console.WriteLine($"\nセル {address} (行{row}, 列{col}):");
                
                if (!isEmpty)
                {
                    Console.WriteLine($"  値: '{value}'");
                    Console.WriteLine($"  フォント: {style.Font.FontName}, サイズ:{style.Font.FontSize}pt, 太字:{style.Font.Bold}");
                    Console.WriteLine($"  配置: 横={style.Alignment.Horizontal}, 縦={style.Alignment.Vertical}");
                }
                else
                {
                    Console.WriteLine($"  値: (空)");
                }

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

                // 背景色
                var bgColor = GetColorInfo(style.Fill.BackgroundColor);
                if (bgColor != "なし" && bgColor != "#FFFFFF")
                {
                    Console.WriteLine($"  背景色: {bgColor}");
                }
            }
        }

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("分析サマリー:");
        Console.WriteLine($"  総セル数: {totalCells}");
        Console.WriteLine($"  値があるセル: {cellsWithValue}");
        Console.WriteLine($"  罫線があるセル: {cellsWithBorder}");
        Console.WriteLine($"  空で罫線なしのセル: {emptyCellsNoBorder}");
        Console.WriteLine(new string('=', 80));

        // 特定セルの強制チェック
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("特定セルの強制チェック:");
        Console.WriteLine(new string('=', 80));

        var specificCells = new[] { "D1", "A3", "B3", "C3", "D3", "E3", "F3", "G3", "A4", "B4", "C4", "A5", "A8" };
        foreach (var cellAddress in specificCells)
        {
            try
            {
                var cell = worksheet.Cell(cellAddress);
                var value = cell.GetString();
                var style = cell.Style;

                Console.WriteLine($"\n{cellAddress}:");
                Console.WriteLine($"  値: '{value}' (IsEmpty: {string.IsNullOrEmpty(value)})");
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

    private static string GetColorInfo(XLColor color)
    {
        if (color == null)
            return "なし";

        try
        {
            if (color.ColorType == XLColorType.Color)
            {
                return $"#{color.Color.ToArgb() & 0xFFFFFF:X6}";
            }
            return color.ColorType.ToString();
        }
        catch
        {
            return "なし";
        }
    }
}

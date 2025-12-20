namespace WorkExcelToPdf;

using ClosedXML.Excel;

/// <summary>
/// テスト用のExcelファイルを作成するクラス
/// </summary>
public class SampleExcelCreator
{
    /// <summary>
    /// 罫線のサンプルExcelファイルを作成
    /// </summary>
    public static void CreateSampleExcel(string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("罫線サンプル");

        // タイトル行
        var titleCell = worksheet.Cell(1, 1);
        titleCell.Value = "罫線サンプル";
        titleCell.Style.Font.Bold = true;
        titleCell.Style.Font.FontSize = 16;

        // 各種罫線スタイルのサンプル
        var row = 3;
        var borderStyles = new[]
        {
            (XLBorderStyleValues.Thin, "細線"),
            (XLBorderStyleValues.Medium, "中線"),
            (XLBorderStyleValues.Thick, "太線"),
            (XLBorderStyleValues.Double, "二重線"),
            (XLBorderStyleValues.Dotted, "点線"),
            (XLBorderStyleValues.Dashed, "破線"),
            (XLBorderStyleValues.DashDot, "一点鎖線"),
            (XLBorderStyleValues.DashDotDot, "二点鎖線")
        };

        worksheet.Cell(row, 1).Value = "スタイル名";
        worksheet.Cell(row, 2).Value = "サンプル";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        row++;

        foreach (var (style, name) in borderStyles)
        {
            worksheet.Cell(row, 1).Value = name;
            
            var cell = worksheet.Cell(row, 2);
            cell.Value = "サンプル";
            cell.Style.Border.OutsideBorder = style;
            cell.Style.Border.OutsideBorderColor = XLColor.Black;
            
            row++;
        }

        // 表のサンプル
        row += 2;
        worksheet.Cell(row, 1).Value = "商品名";
        worksheet.Cell(row, 2).Value = "価格";
        worksheet.Cell(row, 3).Value = "数量";
        
        var headerRange = worksheet.Range(row, 1, row, 3);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        row++;
        var dataRows = new[]
        {
            ("りんご", "100円", "5"),
            ("みかん", "80円", "10"),
            ("バナナ", "150円", "3")
        };

        foreach (var (product, price, quantity) in dataRows)
        {
            worksheet.Cell(row, 1).Value = product;
            worksheet.Cell(row, 2).Value = price;
            worksheet.Cell(row, 3).Value = quantity;
            
            var dataRange = worksheet.Range(row, 1, row, 3);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            
            row++;
        }

        // 列幅の調整
        worksheet.Column(1).Width = 15;
        worksheet.Column(2).Width = 20;
        worksheet.Column(3).Width = 10;

        workbook.SaveAs(filePath);
        Console.WriteLine($"サンプルExcelファイルを作成しました: {filePath}");
    }

    /// <summary>
    /// 複雑な罫線パターンのサンプルを作成
    /// </summary>
    public static void CreateComplexBorderSample(string filePath)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("複雑な罫線");

        // タイトル
        worksheet.Cell(1, 1).Value = "複雑な罫線パターン";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;

        // 各辺に異なる罫線スタイルを設定
        var cell = worksheet.Cell(3, 2);
        cell.Value = "各辺が異なる";
        cell.Style.Border.TopBorder = XLBorderStyleValues.Thick;
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Double;
        cell.Style.Border.LeftBorder = XLBorderStyleValues.Dashed;
        cell.Style.Border.RightBorder = XLBorderStyleValues.Dotted;

        // 色付き罫線
        var colorCell = worksheet.Cell(5, 2);
        colorCell.Value = "色付き罫線";
        colorCell.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        colorCell.Style.Border.OutsideBorderColor = XLColor.Red;

        // 斜め線
        var diagonalCell = worksheet.Cell(7, 2);
        diagonalCell.Value = "斜め線";
        diagonalCell.Style.Border.DiagonalUp = true;
        diagonalCell.Style.Border.DiagonalDown = true;
        diagonalCell.Style.Border.DiagonalBorder = XLBorderStyleValues.Thin;
        diagonalCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // マージされたセル
        var mergedRange = worksheet.Range(9, 2, 10, 4);
        mergedRange.Merge();
        mergedRange.Value = "マージされたセル";
        mergedRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        mergedRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        mergedRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        worksheet.Column(2).Width = 20;

        workbook.SaveAs(filePath);
        Console.WriteLine($"複雑な罫線サンプルを作成しました: {filePath}");
    }
}

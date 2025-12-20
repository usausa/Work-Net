using ClosedXML.Excel;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace WorkExcelToPdf;

/// <summary>
/// セルの罫線情報を保持するクラス（PDF用拡張版）
/// </summary>
public class CellBorderInfoEx
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string CellAddress { get; set; } = string.Empty;
    public BorderSideInfo Top { get; set; } = new();
    public BorderSideInfo Bottom { get; set; } = new();
    public BorderSideInfo Left { get; set; } = new();
    public BorderSideInfo Right { get; set; } = new();
    public BorderSideInfo DiagonalUp { get; set; } = new();
    public BorderSideInfo DiagonalDown { get; set; } = new();

    // セルの位置とサイズ情報
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    // セルの内容
    public string Value { get; set; } = string.Empty;
    public CellStyleInfo Style { get; set; } = new();
}

/// <summary>
/// セルのスタイル情報
/// </summary>
public class CellStyleInfo
{
    public string FontName { get; set; } = "Arial";
    public double FontSize { get; set; } = 11;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public string FontColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = string.Empty;
    public string HorizontalAlignment { get; set; } = "Left";
    public string VerticalAlignment { get; set; } = "Top";
}

/// <summary>
/// シート全体の情報（PDF用拡張版）
/// </summary>
public class SheetInfoEx
{
    public string Name { get; set; } = string.Empty;
    public List<CellBorderInfoEx> Cells { get; set; } = new();
    public Dictionary<int, double> ColumnWidths { get; set; } = new();
    public Dictionary<int, double> RowHeights { get; set; } = new();
}

/// <summary>
/// Excel罫線リーダー（PDF用拡張版）
/// </summary>
public class ExcelBorderReaderEx
{
    private const double DefaultColumnWidth = 64.0; // ピクセル
    private const double DefaultRowHeight = 20.0;   // ピクセル

    /// <summary>
    /// Excelファイルからシート情報を取得
    /// </summary>
    public SheetInfoEx ReadSheet(string filePath, string? sheetName = null)
    {
        var sheetInfo = new SheetInfoEx();

        using (var workbook = new XLWorkbook(filePath))
        {
            var worksheet = string.IsNullOrEmpty(sheetName)
                ? workbook.Worksheet(1)
                : workbook.Worksheet(sheetName);

            sheetInfo.Name = worksheet.Name;

            var usedRange = worksheet.RangeUsed();
            if (usedRange == null)
            {
                Console.WriteLine("使用されているセルがありません。");
                return sheetInfo;
            }

            Console.WriteLine($"\n=== Excel読み込み詳細 ===");
            Console.WriteLine($"シート名: {worksheet.Name}");
            Console.WriteLine($"使用範囲（RangeUsed）: {usedRange.RangeAddress}");
            
            // 実際に罫線があるセルを探すため、より広い範囲をスキャン
            int firstRow = usedRange.FirstRow().RowNumber();
            int firstCol = usedRange.FirstColumn().ColumnNumber();
            int lastRow = usedRange.LastRow().RowNumber();
            int lastCol = usedRange.LastColumn().ColumnNumber();
            
            // 罫線のみが設定されているセルを見つけるため、さらに下方向にスキャン
            int scanRows = Math.Max(lastRow + 10, 20); // 最低20行までスキャン
            for (int row = lastRow + 1; row <= scanRows; row++)
            {
                for (int col = firstCol; col <= lastCol; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    var style = cell.Style;
                    
                    // 罫線があるかチェック
                    if (style.Border.TopBorder != XLBorderStyleValues.None ||
                        style.Border.BottomBorder != XLBorderStyleValues.None ||
                        style.Border.LeftBorder != XLBorderStyleValues.None ||
                        style.Border.RightBorder != XLBorderStyleValues.None)
                    {
                        lastRow = Math.Max(lastRow, row);
                        break;
                    }
                }
            }
            
            Console.WriteLine($"拡張後の範囲: 行{firstRow}～{lastRow}, 列{firstCol}～{lastCol}");

            // 列幅を取得
            for (int col = firstCol; col <= lastCol; col++)
            {
                var column = worksheet.Column(col);
                sheetInfo.ColumnWidths[col] = column.Width * 7; // Excelの幅単位をピクセルに変換
            }

            // 行高を取得
            for (int row = firstRow; row <= lastRow; row++)
            {
                var rowObj = worksheet.Row(row);
                sheetInfo.RowHeights[row] = rowObj.Height * 1.33; // Excelのポイントをピクセルに変換
            }

            // セル情報を取得（拡張範囲内のすべてのセルを確認）
            int cellCount = 0;
            int cellsWithValue = 0;
            int cellsWithBorder = 0;

            Console.WriteLine($"\n=== セル情報読み取り ===");
            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int col = firstCol; col <= lastCol; col++)
                {
                    var cell = worksheet.Cell(row, col);
                    var cellInfo = ExtractCellInfo(cell, sheetInfo);
                    sheetInfo.Cells.Add(cellInfo);
                    cellCount++;

                    bool hasValue = !string.IsNullOrEmpty(cellInfo.Value);
                    bool hasBorder = HasAnyBorder(cellInfo);

                    if (hasValue || hasBorder)
                    {
                        Console.Write($"セル {cellInfo.CellAddress}: ");
                        if (hasValue)
                        {
                            Console.Write($"値='{cellInfo.Value}' ");
                            cellsWithValue++;
                        }
                        if (hasBorder)
                        {
                            Console.Write($"罫線=[");
                            var borders = new List<string>();
                            if (cellInfo.Top.HasBorder) borders.Add($"上:{cellInfo.Top.LineStyle}");
                            if (cellInfo.Bottom.HasBorder) borders.Add($"下:{cellInfo.Bottom.LineStyle}");
                            if (cellInfo.Left.HasBorder) borders.Add($"左:{cellInfo.Left.LineStyle}");
                            if (cellInfo.Right.HasBorder) borders.Add($"右:{cellInfo.Right.LineStyle}");
                            Console.Write(string.Join(",", borders));
                            Console.Write("]");
                            cellsWithBorder++;
                        }
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine($"\n=== 読み込み結果サマリー ===");
            Console.WriteLine($"読み込んだセル総数: {cellCount}");
            Console.WriteLine($"値があるセル: {cellsWithValue}個");
            Console.WriteLine($"罫線があるセル: {cellsWithBorder}個");
        }

        return sheetInfo;
    }

    /// <summary>
    /// 罫線があるかチェック
    /// </summary>
    private bool HasAnyBorder(CellBorderInfoEx cellInfo)
    {
        return cellInfo.Top.HasBorder ||
               cellInfo.Bottom.HasBorder ||
               cellInfo.Left.HasBorder ||
               cellInfo.Right.HasBorder ||
               cellInfo.DiagonalUp.HasBorder ||
               cellInfo.DiagonalDown.HasBorder;
    }

    /// <summary>
    /// セルから完全な情報を抽出
    /// </summary>
    private CellBorderInfoEx ExtractCellInfo(IXLCell cell, SheetInfoEx sheetInfo)
    {
        var cellInfo = new CellBorderInfoEx
        {
            Row = cell.Address.RowNumber,
            Column = cell.Address.ColumnNumber,
            CellAddress = cell.Address.ToString(),
            Value = cell.GetString()
        };

        // セルの位置を計算
        cellInfo.X = CalculateX(cellInfo.Column, sheetInfo.ColumnWidths);
        cellInfo.Y = CalculateY(cellInfo.Row, sheetInfo.RowHeights);
        cellInfo.Width = sheetInfo.ColumnWidths.GetValueOrDefault(cellInfo.Column, DefaultColumnWidth);
        cellInfo.Height = sheetInfo.RowHeights.GetValueOrDefault(cellInfo.Row, DefaultRowHeight);

        var style = cell.Style;

        // 罫線情報を取得
        cellInfo.Top = GetBorderSideInfo(style.Border.TopBorder, style.Border.TopBorderColor);
        cellInfo.Bottom = GetBorderSideInfo(style.Border.BottomBorder, style.Border.BottomBorderColor);
        cellInfo.Left = GetBorderSideInfo(style.Border.LeftBorder, style.Border.LeftBorderColor);
        cellInfo.Right = GetBorderSideInfo(style.Border.RightBorder, style.Border.RightBorderColor);
        
        // 斜め線の処理（bool型対応）
        cellInfo.DiagonalUp = GetDiagonalBorderInfo(style.Border.DiagonalUp, style.Border.DiagonalBorder, style.Border.DiagonalBorderColor);
        cellInfo.DiagonalDown = GetDiagonalBorderInfo(style.Border.DiagonalDown, style.Border.DiagonalBorder, style.Border.DiagonalBorderColor);

        // スタイル情報を取得
        cellInfo.Style.FontName = style.Font.FontName;
        cellInfo.Style.FontSize = style.Font.FontSize;
        cellInfo.Style.IsBold = style.Font.Bold;
        cellInfo.Style.IsItalic = style.Font.Italic;
        cellInfo.Style.FontColor = GetColorHex(style.Font.FontColor);
        cellInfo.Style.BackgroundColor = GetColorHex(style.Fill.BackgroundColor);
        cellInfo.Style.HorizontalAlignment = style.Alignment.Horizontal.ToString();
        cellInfo.Style.VerticalAlignment = style.Alignment.Vertical.ToString();

        return cellInfo;
    }

    private double CalculateX(int column, Dictionary<int, double> columnWidths)
    {
        double x = 0;
        for (int i = 1; i < column; i++)
        {
            x += columnWidths.GetValueOrDefault(i, DefaultColumnWidth);
        }
        return x;
    }

    private double CalculateY(int row, Dictionary<int, double> rowHeights)
    {
        double y = 0;
        for (int i = 1; i < row; i++)
        {
            y += rowHeights.GetValueOrDefault(i, DefaultRowHeight);
        }
        return y;
    }

    private BorderSideInfo GetBorderSideInfo(XLBorderStyleValues borderStyle, XLColor borderColor)
    {
        var info = new BorderSideInfo
        {
            HasBorder = borderStyle != XLBorderStyleValues.None,
            LineStyle = borderStyle.ToString(),
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
            LineStyle = borderStyle.ToString(),
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
}

/// <summary>
/// PDF生成クラス
/// </summary>
public class PdfGenerator
{
    private const double PixelToPoint = 0.75; // ピクセルからポイントへの変換係数

    /// <summary>
    /// シート情報からPDFを生成
    /// </summary>
    public void GeneratePdf(SheetInfoEx sheetInfo, string outputPath)
    {
        Console.WriteLine($"\nPDF生成を開始します...");
        Console.WriteLine($"シート名: {sheetInfo.Name}");
        Console.WriteLine($"セル数: {sheetInfo.Cells.Count}");

        // カスタムフォントリゾルバーを設定
        if (PdfSharp.Fonts.GlobalFontSettings.FontResolver == null)
        {
            PdfSharp.Fonts.GlobalFontSettings.FontResolver = new CustomFontResolver();
            Console.WriteLine("カスタムフォントリゾルバーを設定しました。");
        }

        // PDFドキュメントを作成
        PdfDocument document = new PdfDocument();
        document.Info.Title = sheetInfo.Name;

        // ページを追加
        PdfPage page = document.AddPage();
        page.Size = PdfSharp.PageSize.A4;
        page.Orientation = PdfSharp.PageOrientation.Portrait;

        // グラフィックスオブジェクトを取得
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // 描画
        DrawSheet(gfx, sheetInfo);

        // PDFを保存
        document.Save(outputPath);
        Console.WriteLine($"? PDFを生成しました: {outputPath}");
    }

    /// <summary>
    /// シート全体を描画
    /// </summary>
    private void DrawSheet(XGraphics gfx, SheetInfoEx sheetInfo)
    {
        // マージン設定
        double marginLeft = 30;
        double marginTop = 30;

        int bgCount = 0, textCount = 0, borderCount = 0;

        Console.WriteLine($"\n=== PDF描画処理 ===");
        Console.WriteLine($"全セル数: {sheetInfo.Cells.Count}");

        // デバッグ: セル情報をログ出力
        var cellsWithText = sheetInfo.Cells.Where(c => !string.IsNullOrEmpty(c.Value)).ToList();
        var cellsWithBorders = sheetInfo.Cells.Where(c => HasAnyBorder(c)).ToList();
        var cellsWithBackground = sheetInfo.Cells.Where(c => 
            !string.IsNullOrEmpty(c.Style.BackgroundColor) &&
            c.Style.BackgroundColor != "#FFFFFF" &&
            c.Style.BackgroundColor != "#000000").ToList();

        Console.WriteLine($"テキストがあるセル: {cellsWithText.Count}個");
        if (cellsWithText.Any())
        {
            Console.WriteLine("  テキストセル一覧:");
            foreach (var cell in cellsWithText)
            {
                Console.WriteLine($"    {cell.CellAddress}: '{cell.Value}'");
            }
        }

        Console.WriteLine($"罫線があるセル: {cellsWithBorders.Count}個");
        if (cellsWithBorders.Any())
        {
            Console.WriteLine("  罫線セル一覧（最初の10個）:");
            foreach (var cell in cellsWithBorders.Take(10))
            {
                var borders = new List<string>();
                if (cell.Top.HasBorder) borders.Add($"上:{cell.Top.LineStyle}");
                if (cell.Bottom.HasBorder) borders.Add($"下:{cell.Bottom.LineStyle}");
                if (cell.Left.HasBorder) borders.Add($"左:{cell.Left.LineStyle}");
                if (cell.Right.HasBorder) borders.Add($"右:{cell.Right.LineStyle}");
                Console.WriteLine($"    {cell.CellAddress}: [{string.Join(",", borders)}]");
            }
            if (cellsWithBorders.Count > 10)
            {
                Console.WriteLine($"    ... 他 {cellsWithBorders.Count - 10} セル");
            }
        }

        Console.WriteLine($"背景色があるセル: {cellsWithBackground.Count}個");

        Console.WriteLine($"\n=== 描画実行 ===");

        // 背景色を先に描画
        foreach (var cell in sheetInfo.Cells)
        {
            if (!string.IsNullOrEmpty(cell.Style.BackgroundColor) &&
                cell.Style.BackgroundColor != "#FFFFFF" &&
                cell.Style.BackgroundColor != "#000000")
            {
                DrawCellBackground(gfx, cell, marginLeft, marginTop);
                bgCount++;
            }
        }

        // セルの内容を描画
        foreach (var cell in sheetInfo.Cells)
        {
            if (!string.IsNullOrEmpty(cell.Value))
            {
                DrawCellText(gfx, cell, marginLeft, marginTop);
                textCount++;
            }
        }

        // 罫線を太さ順に描画（細い線から太い線へ）
        // これにより、太い線が細い線を上書きする
        var bordersToDrawn = new List<(CellBorderInfoEx cell, string side, BorderSideInfo border)>();

        foreach (var cell in sheetInfo.Cells)
        {
            if (cell.Top.HasBorder)
                bordersToDrawn.Add((cell, "Top", cell.Top));
            if (cell.Bottom.HasBorder)
                bordersToDrawn.Add((cell, "Bottom", cell.Bottom));
            if (cell.Left.HasBorder)
                bordersToDrawn.Add((cell, "Left", cell.Left));
            if (cell.Right.HasBorder)
                bordersToDrawn.Add((cell, "Right", cell.Right));
            if (cell.DiagonalUp.HasBorder)
                bordersToDrawn.Add((cell, "DiagonalUp", cell.DiagonalUp));
            if (cell.DiagonalDown.HasBorder)
                bordersToDrawn.Add((cell, "DiagonalDown", cell.DiagonalDown));
        }

        // 太さでソート（細い線から太い線へ）
        var sortedBorders = bordersToDrawn.OrderBy(b => b.border.Width).ToList();

        foreach (var (cell, side, border) in sortedBorders)
        {
            double x = marginLeft + cell.X * PixelToPoint;
            double y = marginTop + cell.Y * PixelToPoint;
            double width = cell.Width * PixelToPoint;
            double height = cell.Height * PixelToPoint;

            switch (side)
            {
                case "Top":
                    DrawBorderLine(gfx, border, x, y, x + width, y);
                    break;
                case "Bottom":
                    DrawBorderLine(gfx, border, x, y + height, x + width, y + height);
                    break;
                case "Left":
                    DrawBorderLine(gfx, border, x, y, x, y + height);
                    break;
                case "Right":
                    DrawBorderLine(gfx, border, x + width, y, x + width, y + height);
                    break;
                case "DiagonalUp":
                    DrawBorderLine(gfx, border, x, y + height, x + width, y);
                    break;
                case "DiagonalDown":
                    DrawBorderLine(gfx, border, x, y, x + width, y + height);
                    break;
            }
            borderCount++;
        }

        Console.WriteLine($"\n=== 描画完了 ===");
        Console.WriteLine($"背景色: {bgCount}個, テキスト: {textCount}個, 罫線: {borderCount}本");
    }

    private bool HasAnyBorder(CellBorderInfoEx cell)
    {
        return cell.Top.HasBorder || cell.Bottom.HasBorder ||
               cell.Left.HasBorder || cell.Right.HasBorder ||
               cell.DiagonalUp.HasBorder || cell.DiagonalDown.HasBorder;
    }

    /// <summary>
    /// セルの背景色を描画
    /// </summary>
    private void DrawCellBackground(XGraphics gfx, CellBorderInfoEx cell, double marginLeft, double marginTop)
    {
        try
        {
            var color = ParseColor(cell.Style.BackgroundColor);
            XBrush brush = new XSolidBrush(color);

            double x = marginLeft + cell.X * PixelToPoint;
            double y = marginTop + cell.Y * PixelToPoint;
            double width = cell.Width * PixelToPoint;
            double height = cell.Height * PixelToPoint;

            gfx.DrawRectangle(brush, x, y, width, height);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"背景色描画エラー ({cell.CellAddress}): {ex.Message}");
        }
    }

    /// <summary>
    /// セルのテキストを描画
    /// </summary>
    private void DrawCellText(XGraphics gfx, CellBorderInfoEx cell, double marginLeft, double marginTop)
    {
        try
        {
            // フォントを作成
            XFontStyleEx fontStyle = XFontStyleEx.Regular;
            if (cell.Style.IsBold) fontStyle |= XFontStyleEx.Bold;
            if (cell.Style.IsItalic) fontStyle |= XFontStyleEx.Italic;

            XFont font = new XFont(cell.Style.FontName, cell.Style.FontSize, fontStyle);
            XBrush brush = new XSolidBrush(ParseColor(cell.Style.FontColor));

            double x = marginLeft + cell.X * PixelToPoint;
            double y = marginTop + cell.Y * PixelToPoint;
            double width = cell.Width * PixelToPoint;
            double height = cell.Height * PixelToPoint;

            // テキストの配置を決定
            XStringFormat format = new XStringFormat();
            format.Alignment = GetHorizontalAlignment(cell.Style.HorizontalAlignment);
            format.LineAlignment = GetVerticalAlignment(cell.Style.VerticalAlignment);

            XRect rect = new XRect(x + 2, y + 2, width - 4, height - 4);
            gfx.DrawString(cell.Value, font, brush, rect, format);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"テキスト描画エラー ({cell.CellAddress}): {ex.Message}");
        }
    }

    /// <summary>
    /// セルの罫線を描画
    /// </summary>
    private void DrawCellBorders(XGraphics gfx, CellBorderInfoEx cell, double marginLeft, double marginTop)
    {
        double x = marginLeft + cell.X * PixelToPoint;
        double y = marginTop + cell.Y * PixelToPoint;
        double width = cell.Width * PixelToPoint;
        double height = cell.Height * PixelToPoint;

        // 上辺
        if (cell.Top.HasBorder)
        {
            DrawBorderLine(gfx, cell.Top, x, y, x + width, y);
        }

        // 下辺
        if (cell.Bottom.HasBorder)
        {
            DrawBorderLine(gfx, cell.Bottom, x, y + height, x + width, y + height);
        }

        // 左辺
        if (cell.Left.HasBorder)
        {
            DrawBorderLine(gfx, cell.Left, x, y, x, y + height);
        }

        // 右辺
        if (cell.Right.HasBorder)
        {
            DrawBorderLine(gfx, cell.Right, x + width, y, x + width, y + height);
        }

        // 斜め線（左上から右下）
        if (cell.DiagonalDown.HasBorder)
        {
            DrawBorderLine(gfx, cell.DiagonalDown, x, y, x + width, y + height);
        }

        // 斜め線（左下から右上）
        if (cell.DiagonalUp.HasBorder)
        {
            DrawBorderLine(gfx, cell.DiagonalUp, x, y + height, x + width, y);
        }
    }

    /// <summary>
    /// 罫線を描画
    /// </summary>
    private void DrawBorderLine(XGraphics gfx, BorderSideInfo border, double x1, double y1, double x2, double y2)
    {
        try
        {
            XPen pen = CreatePen(border);
            gfx.DrawLine(pen, x1, y1, x2, y2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"罫線描画エラー: {ex.Message}");
        }
    }

    /// <summary>
    /// 罫線情報からペンを作成
    /// </summary>
    private XPen CreatePen(BorderSideInfo border)
    {
        XColor color = ParseColor(border.Color);
        XPen pen = new XPen(color, border.Width);

        // 線のスタイルを設定
        switch (border.LineStyle)
        {
            case "Dotted":
                pen.DashStyle = XDashStyle.Dot;
                break;
            case "Dashed":
            case "MediumDashed":
                pen.DashStyle = XDashStyle.Dash;
                break;
            case "DashDot":
            case "MediumDashDot":
                pen.DashStyle = XDashStyle.DashDot;
                break;
            case "DashDotDot":
            case "MediumDashDotDot":
                pen.DashStyle = XDashStyle.DashDotDot;
                break;
            case "Double":
                // 二重線は2本の線で表現
                pen.Width = border.Width / 2;
                break;
            default:
                pen.DashStyle = XDashStyle.Solid;
                break;
        }

        return pen;
    }

    /// <summary>
    /// 16進数カラーコードをXColorに変換
    /// </summary>
    private XColor ParseColor(string hexColor)
    {
        if (string.IsNullOrEmpty(hexColor))
            return XColors.Black;

        try
        {
            hexColor = hexColor.TrimStart('#');

            if (hexColor.Length == 6)
            {
                byte r = Convert.ToByte(hexColor.Substring(0, 2), 16);
                byte g = Convert.ToByte(hexColor.Substring(2, 2), 16);
                byte b = Convert.ToByte(hexColor.Substring(4, 2), 16);
                return XColor.FromArgb(r, g, b);
            }
            else if (hexColor.Length == 8)
            {
                byte a = Convert.ToByte(hexColor.Substring(0, 2), 16);
                byte r = Convert.ToByte(hexColor.Substring(2, 2), 16);
                byte g = Convert.ToByte(hexColor.Substring(4, 2), 16);
                byte b = Convert.ToByte(hexColor.Substring(6, 2), 16);
                return XColor.FromArgb(a, r, g, b);
            }
        }
        catch
        {
            return XColors.Black;
        }

        return XColors.Black;
    }

    /// <summary>
    /// 水平方向の配置を取得
    /// </summary>
    private XStringAlignment GetHorizontalAlignment(string alignment)
    {
        return alignment switch
        {
            "Center" => XStringAlignment.Center,
            "Right" => XStringAlignment.Far,
            _ => XStringAlignment.Near
        };
    }

    /// <summary>
    /// 垂直方向の配置を取得
    /// </summary>
    private XLineAlignment GetVerticalAlignment(string alignment)
    {
        return alignment switch
        {
            "Center" => XLineAlignment.Center,
            "Bottom" => XLineAlignment.Far,
            _ => XLineAlignment.Near
        };
    }
}

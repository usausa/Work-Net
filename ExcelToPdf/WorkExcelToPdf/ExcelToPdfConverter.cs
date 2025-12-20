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
    
    // セル結合情報
    public bool IsMerged { get; set; }
    public int MergeWidth { get; set; } = 1;  // 結合された列数
    public int MergeHeight { get; set; } = 1; // 結合された行数
    public string MergeRangeAddress { get; set; } = string.Empty;
}

/// <summary>
/// セルのスタイル情報（拡張版）
/// </summary>
public class CellStyleInfo
{
    public string FontName { get; set; } = "Arial";
    public double FontSize { get; set; } = 11;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsUnderline { get; set; }
    public bool IsStrikethrough { get; set; }
    public string FontColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = string.Empty;
    public string HorizontalAlignment { get; set; } = "Left";
    public string VerticalAlignment { get; set; } = "Top";
    public int TextRotation { get; set; } = 0; // 0, 90, 180, 270度
    public int Indent { get; set; } = 0;
    public bool WrapText { get; set; } = false;
    public bool ShrinkToFit { get; set; } = false;
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
/// PDF生成オプション
/// </summary>
public class PdfGenerationOptions
{
    public PdfSharp.PageSize PageSize { get; set; } = PdfSharp.PageSize.A4;
    public PdfSharp.PageOrientation Orientation { get; set; } = PdfSharp.PageOrientation.Portrait;
    public double MarginLeft { get; set; } = 30;
    public double MarginTop { get; set; } = 30;
    public double MarginRight { get; set; } = 30;
    public double MarginBottom { get; set; } = 30;
    public double Scale { get; set; } = 1.0;
    public bool EnableAutoPageBreak { get; set; } = false;
    public bool ShowPageNumber { get; set; } = false;
    public string PageNumberFormat { get; set; } = "Page {0}";
    public string HeaderText { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
    public bool ShowGridLines { get; set; } = false;
    public bool IgnoreMergedCells { get; set; } = false;
    public bool IgnoreBackgroundColor { get; set; } = false;
    public int ImageQuality { get; set; } = 90;
    public bool DebugMode { get; set; } = false;
    
    /// <summary>
    /// 疑似Bold（太字）を有効にするか
    /// フォントが単一ウェイトの場合、テキストを重ね描きして太字を再現します
    /// </summary>
    public bool EnableSimulatedBold { get; set; } = true;
    
    /// <summary>
    /// 疑似Italic（斜体）を有効にするか
    /// フォントが斜体をサポートしていない場合、Skew変換で斜体を再現します
    /// </summary>
    public bool EnableSimulatedItalic { get; set; } = true;
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
        cellInfo.Style.IsUnderline = style.Font.Underline != XLFontUnderlineValues.None;
        cellInfo.Style.IsStrikethrough = style.Font.Strikethrough;
        cellInfo.Style.FontColor = GetColorHex(style.Font.FontColor, isBackgroundColor: false);
        
        // 背景色の取得 - PatternTypeもチェック
        if (style.Fill.PatternType != XLFillPatternValues.None)
        {
            // パターン塗りつぶしがある場合、BackgroundColorを取得
            cellInfo.Style.BackgroundColor = GetColorHex(style.Fill.BackgroundColor, isBackgroundColor: true);
            
            // もし背景色が取得できない場合、PatternColorを試す
            if (string.IsNullOrEmpty(cellInfo.Style.BackgroundColor))
            {
                cellInfo.Style.BackgroundColor = GetColorHex(style.Fill.PatternColor, isBackgroundColor: true);
            }
        }
        else
        {
            cellInfo.Style.BackgroundColor = string.Empty;
        }
        
        // 水平方向のアライメント取得 - Excelのデフォルト動作を再現
        var horizontalAlignment = style.Alignment.Horizontal;
        if (horizontalAlignment == XLAlignmentHorizontalValues.General)
        {
            // Generalの場合、セルの値の型に応じてアライメントを決定
            if (cell.DataType == XLDataType.Number || cell.DataType == XLDataType.DateTime)
            {
                // 数値・日付は右揃え（Excelのデフォルト動作）
                cellInfo.Style.HorizontalAlignment = "Right";
            }
            else if (cell.DataType == XLDataType.Boolean)
            {
                // ブール値は中央揃え（Excelのデフォルト動作）
                cellInfo.Style.HorizontalAlignment = "Center";
            }
            else
            {
                // テキストは左揃え（Excelのデフォルト動作）
                cellInfo.Style.HorizontalAlignment = "Left";
            }
        }
        else
        {
            cellInfo.Style.HorizontalAlignment = horizontalAlignment.ToString();
        }
        
        cellInfo.Style.VerticalAlignment = style.Alignment.Vertical.ToString();
        cellInfo.Style.TextRotation = style.Alignment.TextRotation;
        cellInfo.Style.Indent = style.Alignment.Indent;
        cellInfo.Style.WrapText = style.Alignment.WrapText;
        cellInfo.Style.ShrinkToFit = style.Alignment.ShrinkToFit;

        // セル結合情報を取得
        if (cell.IsMerged())
        {
            var mergedRange = cell.MergedRange();
            cellInfo.IsMerged = true;
            cellInfo.MergeRangeAddress = mergedRange.RangeAddress.ToString();
            cellInfo.MergeWidth = mergedRange.ColumnCount();
            cellInfo.MergeHeight = mergedRange.RowCount();
            
            // 結合されたセルのサイズを計算
            double mergedWidth = 0;
            for (int col = cellInfo.Column; col < cellInfo.Column + cellInfo.MergeWidth; col++)
            {
                mergedWidth += sheetInfo.ColumnWidths.GetValueOrDefault(col, DefaultColumnWidth);
            }
            cellInfo.Width = mergedWidth;
            
            double mergedHeight = 0;
            for (int row = cellInfo.Row; row < cellInfo.Row + cellInfo.MergeHeight; row++)
            {
                mergedHeight += sheetInfo.RowHeights.GetValueOrDefault(row, DefaultRowHeight);
            }
            cellInfo.Height = mergedHeight;
        }

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
        var color = GetColorHex(borderColor, isBackgroundColor: false);
        
        // 罫線色が取得できない場合は黒をデフォルトとする
        if (string.IsNullOrEmpty(color))
        {
            color = "#000000";
        }
        
        var info = new BorderSideInfo
        {
            HasBorder = borderStyle != XLBorderStyleValues.None,
            LineStyle = borderStyle.ToString(),
            Color = color,
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

        var color = GetColorHex(borderColor, isBackgroundColor: false);
        
        // 罫線色が取得できない場合は黒をデフォルトとする
        if (string.IsNullOrEmpty(color))
        {
            color = "#000000";
        }

        var info = new BorderSideInfo
        {
            HasBorder = true,
            LineStyle = borderStyle.ToString(),
            Color = color,
            Width = GetBorderWidth(borderStyle)
        };

        return info;
    }

    /// <summary>
    /// 色情報を16進数文字列に変換
    /// </summary>
    private string GetColorHex(XLColor color, bool isBackgroundColor = false)
    {
        if (color == null)
            return string.Empty;

        try
        {
            var colorType = color.ColorType;
            
            // Colorタイプ（直接RGB指定）
            if (colorType == XLColorType.Color)
            {
                var argb = color.Color.ToArgb() & 0xFFFFFF;
                
                // 背景色の場合、白または黒は無視
                if (isBackgroundColor && (argb == 0xFFFFFF || argb == 0x000000))
                    return string.Empty;
                
                return $"#{argb:X6}";
            }
            
            // Indexedタイプ（インデックス色）
            if (colorType == XLColorType.Indexed)
            {
                try
                {
                    var argb = color.Color.ToArgb() & 0xFFFFFF;
                    
                    if (isBackgroundColor)
                    {
                        // 背景色の場合、白または黒は無視
                        if (argb == 0xFFFFFF || argb == 0x000000)
                            return string.Empty;
                    }
                    else
                    {
                        // フォント色・罫線色の場合、白はデフォルトの黒に変換
                        if (argb == 0xFFFFFF)
                            return "#000000";
                    }
                    
                    return $"#{argb:X6}";
                }
                catch
                {
                    // フォント色・罫線色のデフォルトは黒
                    return isBackgroundColor ? string.Empty : "#000000";
                }
            }
            
            // Themeタイプ（テーマ色）
            if (colorType == XLColorType.Theme)
            {
                try
                {
                    var argb = color.Color.ToArgb() & 0xFFFFFF;
                    
                    // 背景色の場合
                    if (isBackgroundColor)
                    {
                        // 白が返される場合、テーマのアクセントカラーを使用
                        if (argb == 0xFFFFFF)
                        {
                            return "#4472C4"; // Excel default accent1 color
                        }
                        
                        if (argb == 0x000000)
                            return string.Empty;
                    }
                    else
                    {
                        // フォント色・罫線色の場合
                        // 白が返される場合は、デフォルトの黒色（テキスト色）を使用
                        if (argb == 0xFFFFFF)
                        {
                            return "#000000"; // 黒（デフォルトテキスト色）
                        }
                    }
                    
                    return $"#{argb:X6}";
                }
                catch
                {
                    // エラー時のデフォルト値
                    return isBackgroundColor ? "#4472C4" : "#000000";
                }
            }
            
            return string.Empty;
        }
        catch
        {
            return string.Empty;
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
/// PDF生成クラス（拡張版）
/// </summary>
public class PdfGenerator
{
    private const double PixelToPoint = 0.75; // ピクセルからポイントへの変換係数
    private PdfGenerationOptions _options;

    public PdfGenerator()
    {
        _options = new PdfGenerationOptions();
    }

    public PdfGenerator(PdfGenerationOptions options)
    {
        _options = options ?? new PdfGenerationOptions();
    }

    /// <summary>
    /// シート情報からPDFを生成
    /// </summary>
    public void GeneratePdf(SheetInfoEx sheetInfo, string outputPath)
    {
        GeneratePdf(sheetInfo, outputPath, _options);
    }

    /// <summary>
    /// シート情報からPDFを生成（オプション指定）
    /// </summary>
    public void GeneratePdf(SheetInfoEx sheetInfo, string outputPath, PdfGenerationOptions options)
    {
        _options = options ?? _options;
        
        Console.WriteLine($"\nPDF生成を開始します...");
        Console.WriteLine($"シート名: {sheetInfo.Name}");
        Console.WriteLine($"セル数: {sheetInfo.Cells.Count}");
        Console.WriteLine($"ページサイズ: {_options.PageSize}");
        Console.WriteLine($"向き: {_options.Orientation}");
        Console.WriteLine($"スケール: {_options.Scale * 100}%");

        // カスタムフォントリゾルバーを設定
        if (PdfSharp.Fonts.GlobalFontSettings.FontResolver == null)
        {
            PdfSharp.Fonts.GlobalFontSettings.FontResolver = new CustomFontResolver();
            Console.WriteLine("カスタムフォントリゾルバーを設定しました。");
        }

        // PDFドキュメントを作成
        PdfDocument document = new PdfDocument();
        document.Info.Title = sheetInfo.Name;
        document.Info.Author = "Excel to PDF Converter";
        document.Info.Creator = "WorkExcelToPdf";
        document.Info.Subject = $"Converted from Excel sheet: {sheetInfo.Name}";

        if (_options.EnableAutoPageBreak)
        {
            // 複数ページ対応
            GenerateMultiplePages(document, sheetInfo);
        }
        else
        {
            // 単一ページ
            GenerateSinglePage(document, sheetInfo);
        }

        // PDFを保存
        document.Save(outputPath);
        Console.WriteLine($"✓ PDFを生成しました: {outputPath}");
    }

    /// <summary>
    /// 単一ページを生成
    /// </summary>
    private void GenerateSinglePage(PdfDocument document, SheetInfoEx sheetInfo)
    {
        // ページを追加
        PdfPage page = document.AddPage();
        page.Size = _options.PageSize;
        page.Orientation = _options.Orientation;

        // グラフィックスオブジェクトを取得
        XGraphics gfx = XGraphics.FromPdfPage(page);

        // ヘッダー・フッターを描画
        if (!string.IsNullOrEmpty(_options.HeaderText))
        {
            DrawHeader(gfx, page, _options.HeaderText, 1, 1);
        }

        if (!string.IsNullOrEmpty(_options.FooterText) || _options.ShowPageNumber)
        {
            string footerText = _options.FooterText;
            if (_options.ShowPageNumber)
            {
                footerText += (string.IsNullOrEmpty(footerText) ? "" : " - ") + 
                             string.Format(_options.PageNumberFormat, 1);
            }
            DrawFooter(gfx, page, footerText);
        }

        // シートを描画
        DrawSheet(gfx, sheetInfo);
    }

    /// <summary>
    /// 複数ページを生成
    /// </summary>
    private void GenerateMultiplePages(PdfDocument document, SheetInfoEx sheetInfo)
    {
        // ページサイズを取得
        PdfPage tempPage = document.AddPage();
        tempPage.Size = _options.PageSize;
        tempPage.Orientation = _options.Orientation;
        
        double pageWidth = tempPage.Width.Point - _options.MarginLeft - _options.MarginRight;
        double pageHeight = tempPage.Height.Point - _options.MarginTop - _options.MarginBottom;
        
        document.Pages.Remove(tempPage);

        // シート全体のサイズを計算
        double totalWidth = sheetInfo.Cells.Max(c => c.X + c.Width) * PixelToPoint * _options.Scale;
        double totalHeight = sheetInfo.Cells.Max(c => c.Y + c.Height) * PixelToPoint * _options.Scale;

        // 必要なページ数を計算
        int pagesX = (int)Math.Ceiling(totalWidth / pageWidth);
        int pagesY = (int)Math.Ceiling(totalHeight / pageHeight);
        int totalPages = pagesX * pagesY;

        Console.WriteLine($"複数ページモード: {pagesX} x {pagesY} = {totalPages} ページ");

        int currentPage = 1;
        for (int py = 0; py < pagesY; py++)
        {
            for (int px = 0; px < pagesX; px++)
            {
                PdfPage page = document.AddPage();
                page.Size = _options.PageSize;
                page.Orientation = _options.Orientation;

                XGraphics gfx = XGraphics.FromPdfPage(page);

                // ヘッダー・フッター
                if (!string.IsNullOrEmpty(_options.HeaderText))
                {
                    DrawHeader(gfx, page, _options.HeaderText, currentPage, totalPages);
                }

                if (!string.IsNullOrEmpty(_options.FooterText) || _options.ShowPageNumber)
                {
                    string footerText = _options.FooterText;
                    if (_options.ShowPageNumber)
                    {
                        footerText += (string.IsNullOrEmpty(footerText) ? "" : " - ") + 
                                     string.Format(_options.PageNumberFormat, currentPage, totalPages);
                    }
                    DrawFooter(gfx, page, footerText);
                }

                // 現在のページに描画する範囲を計算
                double offsetX = px * pageWidth;
                double offsetY = py * pageHeight;

                DrawSheetPartial(gfx, sheetInfo, offsetX, offsetY, pageWidth, pageHeight);

                currentPage++;
            }
        }
    }

    /// <summary>
    /// ヘッダーを描画
    /// </summary>
    private void DrawHeader(XGraphics gfx, PdfPage page, string text, int currentPage, int totalPages)
    {
        XFont font = new XFont("Arial", 10, XFontStyleEx.Regular);
        XBrush brush = XBrushes.Gray;
        
        string headerText = text.Replace("{page}", currentPage.ToString())
                                .Replace("{total}", totalPages.ToString());
        
        gfx.DrawString(headerText, font, brush, 
            new XRect(0, 10, page.Width.Point, 20), 
            XStringFormats.TopCenter);
    }

    /// <summary>
    /// フッターを描画
    /// </summary>
    private void DrawFooter(XGraphics gfx, PdfPage page, string text)
    {
        XFont font = new XFont("Arial", 10, XFontStyleEx.Regular);
        XBrush brush = XBrushes.Gray;
        
        gfx.DrawString(text, font, brush, 
            new XRect(0, page.Height.Point - 30, page.Width.Point, 20), 
            XStringFormats.BottomCenter);
    }

    /// <summary>
    /// シートの一部を描画（複数ページ用）
    /// </summary>
    private void DrawSheetPartial(XGraphics gfx, SheetInfoEx sheetInfo, double offsetX, double offsetY, double viewWidth, double viewHeight)
    {
        // クリッピング領域を設定
        gfx.Save();
        gfx.IntersectClip(new XRect(_options.MarginLeft, _options.MarginTop, viewWidth, viewHeight));

        // オフセットを適用
        gfx.TranslateTransform(_options.MarginLeft - offsetX, _options.MarginTop - offsetY);

        // スケールを適用
        if (_options.Scale != 1.0)
        {
            gfx.ScaleTransform(_options.Scale, _options.Scale);
        }

        // 描画
        DrawSheetContent(gfx, sheetInfo, 0, 0);

        gfx.Restore();
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

            // 塗りつぶし矩形を描画（ペンなし）
            gfx.DrawRectangle(brush, x, y, width, height);
            
            Console.WriteLine($"背景色描画: {cell.CellAddress} at ({x:F1}, {y:F1}) size ({width:F1} x {height:F1}) color {cell.Style.BackgroundColor}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"背景色描画エラー ({cell.CellAddress}): {ex.Message}");
        }
    }

    /// <summary>
    /// セルのテキストを描画（拡張版）
    /// </summary>
    private void DrawCellText(XGraphics gfx, CellBorderInfoEx cell, double marginLeft, double marginTop)
    {
        try
        {
            Console.WriteLine($"テキスト描画開始: {cell.CellAddress} = '{cell.Value}'");
            
            // フォントを作成
            XFontStyleEx fontStyle = XFontStyleEx.Regular;
            if (cell.Style.IsBold) fontStyle |= XFontStyleEx.Bold;
            if (cell.Style.IsItalic) fontStyle |= XFontStyleEx.Italic;

            Console.WriteLine($"  フォント: {cell.Style.FontName}, サイズ:{cell.Style.FontSize}, スタイル:{fontStyle}");

            // IPAフォントは単一ウェイトなので、Regularフォントを使用
            XFont font = new XFont(cell.Style.FontName, cell.Style.FontSize, XFontStyleEx.Regular);
            XBrush brush = new XSolidBrush(ParseColor(cell.Style.FontColor));

            double x = marginLeft + cell.X * PixelToPoint;
            double y = marginTop + cell.Y * PixelToPoint;
            double width = cell.Width * PixelToPoint;
            double height = cell.Height * PixelToPoint;

            Console.WriteLine($"  位置: ({x:F1}, {y:F1}), サイズ: ({width:F1} x {height:F1})");

            // インデント適用
            double indentOffset = cell.Style.Indent * 8; // インデント1レベル = 約8ポイント
            
            // テキストの配置を決定
            XStringFormat format = new XStringFormat();
            format.Alignment = GetHorizontalAlignment(cell.Style.HorizontalAlignment);
            format.LineAlignment = GetVerticalAlignment(cell.Style.VerticalAlignment);

            Console.WriteLine($"  配置: 水平={cell.Style.HorizontalAlignment}, 垂直={cell.Style.VerticalAlignment}");

            // テキスト領域を調整（罫線との余白を考慮）
            double padding = 2;
            XRect rect = new XRect(
                x + padding + indentOffset, 
                y + padding, 
                width - (padding * 2) - indentOffset, 
                height - (padding * 2)
            );

            Console.WriteLine($"  描画領域: ({rect.X:F1}, {rect.Y:F1}) - ({rect.Width:F1} x {rect.Height:F1})");

            // グラフィックス状態を保存
            var state = gfx.Save();

            try
            {
                // テキスト回転が設定されている場合
                if (cell.Style.TextRotation != 0)
                {
                    // 回転の中心点を計算
                    double centerX = x + width / 2;
                    double centerY = y + height / 2;
                    
                    gfx.TranslateTransform(centerX, centerY);
                    gfx.RotateTransform(-cell.Style.TextRotation); // 反時計回りに回転
                    gfx.TranslateTransform(-centerX, -centerY);
                }

                // 疑似Italic：斜体変換（オプションで制御）
                if (cell.Style.IsItalic && _options.EnableSimulatedItalic)
                {
                    Console.WriteLine($"  疑似Italic適用");
                    // Skew変換で斜体を実現（15度傾ける）
                    var matrix = new XMatrix();
                    matrix.TranslatePrepend(rect.X, rect.Y);
                    matrix.SkewPrepend(-15, 0); // 15度傾ける（負の値で右に傾く）
                    matrix.TranslatePrepend(-rect.X, -rect.Y);
                    gfx.MultiplyTransform(matrix);
                }

                // 通常の描画
                Console.WriteLine($"  DrawString実行: '{cell.Value}'");
                gfx.DrawString(cell.Value, font, brush, rect, format);
                
                // 疑似Bold：同じ位置に少しずらして重ね描き（オプションで制御）
                if (cell.Style.IsBold && _options.EnableSimulatedBold)
                {
                    Console.WriteLine($"  疑似Bold適用（重ね描き）");
                    // 0.3ポイントずらして描画
                    var boldRect1 = new XRect(rect.X + 0.3, rect.Y, rect.Width, rect.Height);
                    var boldRect2 = new XRect(rect.X, rect.Y + 0.3, rect.Width, rect.Height);
                    gfx.DrawString(cell.Value, font, brush, boldRect1, format);
                    gfx.DrawString(cell.Value, font, brush, boldRect2, format);
                }

                Console.WriteLine($"  DrawString完了");
            }
            finally
            {
                gfx.Restore(state);
            }

            // 下線を描画
            if (cell.Style.IsUnderline)
            {
                DrawUnderline(gfx, cell, font, rect, brush);
            }

            // 取り消し線を描画
            if (cell.Style.IsStrikethrough)
            {
                DrawStrikethrough(gfx, cell, font, rect, brush);
            }
            
            Console.WriteLine($"テキスト描画完了: {cell.CellAddress}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"テキスト描画エラー ({cell.CellAddress}): {ex.Message}");
            Console.WriteLine($"  スタックトレース: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// 下線を描画
    /// </summary>
    private void DrawUnderline(XGraphics gfx, CellBorderInfoEx cell, XFont font, XRect rect, XBrush brush)
    {
        try
        {
            var size = gfx.MeasureString(cell.Value, font);
            double underlineY = rect.Y + size.Height - 1;
            
            XPen pen = new XPen(((XSolidBrush)brush).Color, 0.5);
            
            // 配置に応じて下線の位置を調整
            double startX = rect.X;
            double endX = rect.X + size.Width;
            
            if (cell.Style.HorizontalAlignment == "Center")
            {
                startX = rect.X + (rect.Width - size.Width) / 2;
                endX = startX + size.Width;
            }
            else if (cell.Style.HorizontalAlignment == "Right")
            {
                startX = rect.X + rect.Width - size.Width;
                endX = rect.X + rect.Width;
            }
            
            gfx.DrawLine(pen, startX, underlineY, endX, underlineY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下線描画エラー ({cell.CellAddress}): {ex.Message}");
        }
    }

    /// <summary>
    /// 取り消し線を描画
    /// </summary>
    private void DrawStrikethrough(XGraphics gfx, CellBorderInfoEx cell, XFont font, XRect rect, XBrush brush)
    {
        try
        {
            var size = gfx.MeasureString(cell.Value, font);
            double strikeY = rect.Y + size.Height / 2;
            
            XPen pen = new XPen(((XSolidBrush)brush).Color, 0.5);
            
            // 配置に応じて取り消し線の位置を調整
            double startX = rect.X;
            double endX = rect.X + size.Width;
            
            if (cell.Style.HorizontalAlignment == "Center")
            {
                startX = rect.X + (rect.Width - size.Width) / 2;
                endX = startX + size.Width;
            }
            else if (cell.Style.HorizontalAlignment == "Right")
            {
                startX = rect.X + rect.Width - size.Width;
                endX = rect.X + rect.Width;
            }
            
            gfx.DrawLine(pen, startX, strikeY, endX, strikeY);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"取り消し線描画エラー ({cell.CellAddress}): {ex.Message}");
        }
    }

    /// <summary>
    /// シート全体を描画
    /// </summary>
    private void DrawSheet(XGraphics gfx, SheetInfoEx sheetInfo)
    {
        // スケールを適用
        if (_options.Scale != 1.0)
        {
            gfx.ScaleTransform(_options.Scale, _options.Scale);
        }

        DrawSheetContent(gfx, sheetInfo, _options.MarginLeft / _options.Scale, _options.MarginTop / _options.Scale);
    }

    /// <summary>
    /// シートの内容を描画（共通処理）
    /// </summary>
    private void DrawSheetContent(XGraphics gfx, SheetInfoEx sheetInfo, double marginLeft, double marginTop)
    {
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
            foreach (var cell in cellsWithText.Take(20))
            {
                var styleInfo = new List<string>();
                if (cell.Style.IsBold) styleInfo.Add("太字");
                if (cell.Style.IsItalic) styleInfo.Add("斜体");
                if (cell.Style.IsUnderline) styleInfo.Add("下線");
                var styles = styleInfo.Any() ? $" [{string.Join(",", styleInfo)}]" : "";
                Console.WriteLine($"    {cell.CellAddress}: '{cell.Value}'{styles} (背景色:{cell.Style.BackgroundColor})");
            }
            if (cellsWithText.Count > 20)
            {
                Console.WriteLine($"    ... 他 {cellsWithText.Count - 20} セル");
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
        if (cellsWithBackground.Any())
        {
            Console.WriteLine("  背景色セル一覧:");
            foreach (var cell in cellsWithBackground)
            {
                Console.WriteLine($"    {cell.CellAddress}: {cell.Style.BackgroundColor}");
            }
        }

        Console.WriteLine($"\n=== 描画実行 ===");

        // グリッド線を描画（オプション）
        if (_options.ShowGridLines)
        {
            DrawGridLines(gfx, sheetInfo, marginLeft, marginTop);
        }

        // 背景色を先に描画
        if (!_options.IgnoreBackgroundColor)
        {
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
        }

        // セルの内容を描画（結合セルは最初のセルのみ描画）
        var cellsToDraw = sheetInfo.Cells.Where(c => 
            !string.IsNullOrEmpty(c.Value) && 
            (!c.IsMerged || c.CellAddress == c.MergeRangeAddress.Split(':')[0])
        ).ToList();

        foreach (var cell in cellsToDraw)
        {
            DrawCellText(gfx, cell, marginLeft, marginTop);
            textCount++;
        }

        // 罫線を太さ順に描画（細い線から太い線へ）
        var bordersToDrawn = new List<(CellBorderInfoEx cell, string side, BorderSideInfo border)>();

        foreach (var cell in sheetInfo.Cells)
        {
            // 結合セルの場合、最初のセルのみ罫線を描画
            if (_options.IgnoreMergedCells || !cell.IsMerged || 
                cell.CellAddress == cell.MergeRangeAddress.Split(':')[0])
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

        // デバッグモード: セルの境界線を表示
        if (_options.DebugMode)
        {
            DrawDebugBorders(gfx, sheetInfo, marginLeft, marginTop);
        }

        Console.WriteLine($"\n=== 描画完了 ===");
        Console.WriteLine($"背景色: {bgCount}個, テキスト: {textCount}個, 罫線: {borderCount}本");
    }

    /// <summary>
    /// グリッド線を描画
    /// </summary>
    private void DrawGridLines(XGraphics gfx, SheetInfoEx sheetInfo, double marginLeft, double marginTop)
    {
        XPen gridPen = new XPen(XColor.FromArgb(220, 220, 220), 0.25);
        
        // 列のグリッド線
        double x = marginLeft;
        foreach (var width in sheetInfo.ColumnWidths.Values)
        {
            double y1 = marginTop;
            double y2 = marginTop + sheetInfo.RowHeights.Values.Sum() * PixelToPoint;
            gfx.DrawLine(gridPen, x, y1, x, y2);
            x += width * PixelToPoint;
        }
        
        // 行のグリッド線
        double y = marginTop;
        foreach (var height in sheetInfo.RowHeights.Values)
        {
            double x1 = marginLeft;
            double x2 = marginLeft + sheetInfo.ColumnWidths.Values.Sum() * PixelToPoint;
            gfx.DrawLine(gridPen, x1, y, x2, y);
            y += height * PixelToPoint;
        }
    }

    /// <summary>
    /// デバッグ用のセル境界線を描画
    /// </summary>
    private void DrawDebugBorders(XGraphics gfx, SheetInfoEx sheetInfo, double marginLeft, double marginTop)
    {
        XPen debugPen = new XPen(XColors.Red, 0.5);
        debugPen.DashStyle = XDashStyle.Dot;
        
        foreach (var cell in sheetInfo.Cells)
        {
            double x = marginLeft + cell.X * PixelToPoint;
            double y = marginTop + cell.Y * PixelToPoint;
            double width = cell.Width * PixelToPoint;
            double height = cell.Height * PixelToPoint;
            
            gfx.DrawRectangle(debugPen, x, y, width, height);
            
            // セルアドレスを表示
            XFont debugFont = new XFont("Arial", 6, XFontStyleEx.Regular);
            gfx.DrawString(cell.CellAddress, debugFont, XBrushes.Red, 
                new XPoint(x + 1, y + 8));
        }
    }

    private bool HasAnyBorder(CellBorderInfoEx cell)
    {
        return cell.Top.HasBorder || cell.Bottom.HasBorder ||
               cell.Left.HasBorder || cell.Right.HasBorder ||
               cell.DiagonalUp.HasBorder || cell.DiagonalDown.HasBorder;
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

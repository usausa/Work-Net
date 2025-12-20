# Excel罫線情報取得サンプルの実行方法

## クイックスタート

### 1. プロジェクトのビルド

```bash
cd WorkExcelToPdf
dotnet build
```

### 2. サンプルExcelファイルで実行

既存の `Template.xlsx` を使用する場合:

```bash
dotnet run
```

### 3. 独自のExcelファイルで実行

```bash
dotnet run -- "path/to/your/file.xlsx"
```

## サンプルExcelファイルを自動作成する

プログラムを以下のように変更して、テスト用のExcelファイルを作成できます:

### Program.cs の Main メソッドの先頭に追加:

```csharp
// サンプルファイルの作成（初回のみ）
if (!File.Exists("sample_basic.xlsx"))
{
    SampleExcelCreator.CreateSampleExcel("sample_basic.xlsx");
}

if (!File.Exists("sample_complex.xlsx"))
{
    SampleExcelCreator.CreateComplexBorderSample("sample_complex.xlsx");
}
```

## 出力される情報

プログラムを実行すると以下の情報が表示されます:

1. **シート情報**
   - シート名
   - 使用範囲（例: A1:D20）
   - 罫線が設定されているセル数

2. **セル一覧**（最初の20セル）
   - セルアドレス（例: A1）
   - 行番号・列番号
   - セルの値
   - セルのサイズ（幅・高さ）
   - 設定されている罫線（簡易表示）

3. **詳細情報**（最初の3セル）
   - 各辺の罫線の詳細
     - 線のスタイル（細線、太線、破線など）
     - 線の色（16進数カラーコード）
     - 線の幅（ポイント単位）

## プログラムから使用する例

### 基本的な使い方

```csharp
using WorkExcelToPdf;

var reader = new ExcelBorderReader();
var sheetInfo = reader.ReadSheetWithBorders("myfile.xlsx");

// シート情報の表示
Console.WriteLine($"シート: {sheetInfo.SheetName}");
Console.WriteLine($"セル数: {sheetInfo.Cells.Count}");

// 各セルの処理
foreach (var cell in sheetInfo.Cells)
{
    Console.WriteLine($"{cell.CellAddress}: {cell.Value}");
    
    // 上辺の罫線をチェック
    if (cell.Border.Top.HasBorder)
    {
        Console.WriteLine($"  上辺: {cell.Border.Top.LineStyle}, " +
                         $"色: {cell.Border.Top.Color}, " +
                         $"幅: {cell.Border.Top.Width}pt");
    }
}
```

### 特定のパターンを検索

```csharp
// 太線が設定されているセルを検索
var thickBorderCells = sheetInfo.Cells.Where(c =>
    c.Border.Top.LineStyle == "太線" ||
    c.Border.Bottom.LineStyle == "太線" ||
    c.Border.Left.LineStyle == "太線" ||
    c.Border.Right.LineStyle == "太線"
).ToList();

Console.WriteLine($"太線が設定されているセル: {thickBorderCells.Count}個");
```

### 罫線の統計情報を取得

```csharp
var stats = new Dictionary<string, int>();

foreach (var cell in sheetInfo.Cells)
{
    var borders = new[] { 
        cell.Border.Top, 
        cell.Border.Bottom, 
        cell.Border.Left, 
        cell.Border.Right 
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

Console.WriteLine("罫線スタイルの使用状況:");
foreach (var (style, count) in stats.OrderByDescending(x => x.Value))
{
    Console.WriteLine($"  {style}: {count}回");
}
```

## トラブルシューティング

### ファイルが見つからない

```
ファイルが見つかりません: Template.xlsx
カレントディレクトリ: C:\...
```

→ 実行ディレクトリに Excel ファイルをコピーするか、フルパスで指定してください。

### ClosedXML のエラー

```
System.IO.FileFormatException: Archive does not contain [Content_Types].xml
```

→ ファイルが破損しているか、xlsx形式ではない可能性があります。

### メモリ不足

大きなExcelファイルを処理する場合、メモリが不足する可能性があります。
その場合は、セルを逐次処理するように変更してください。

## 次のステップ

このサンプルをベースに、PDF生成機能を追加できます:

1. **PDF生成ライブラリの選択**
   - iTextSharp (iText7)
   - PdfSharp
   - QuestPDF

2. **罫線情報のPDF変換**
   - 線のスタイルをPDF形式に変換
   - 色情報の変換
   - 座標計算

3. **レイアウトの再現**
   - セルのサイズと位置を計算
   - 罫線の描画
   - テキストの配置

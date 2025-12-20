# Excel to PDF Converter

このプロジェクトは、Excelファイル（xlsx）から罫線情報を読み取り、その情報をもとにPDFを生成するライブラリです。

## 機能

### 1. 罫線情報の取得
- Excelファイルから罫線情報を読み取る
- 各セルの以下の情報を取得:
  - セル位置（行、列、アドレス）
  - セルの値
  - セルのサイズ（幅、高さ）
  - 罫線情報（上下左右、斜め線）
    - 線のスタイル（細線、太線、破線など14種類）
    - 線の色
    - 線の幅

### 2. PDF生成機能 ?新機能
- Excelの罫線情報をもとにPDFを生成
- セルの配置、サイズ、罫線スタイルを再現
- テキスト内容、フォント、配置を再現
- 背景色の再現
- 斜め線の再現

## 使用ライブラリ

- **ClosedXML** (v0.105.0): Excelファイルの読み書き
- **PdfSharp** (v6.2.3): PDF生成
- **PDFsharp-MigraDoc** (v6.2.3): 高度なPDFレイアウト

## プロジェクト構成

```
WorkExcelToPdf/
├── Program.cs                 // メインプログラム
├── ExcelToPdfConverter.cs     // PDF生成機能
├── SampleExcelCreator.cs      // サンプルExcel作成ツール
├── Template.xlsx              // サンプルExcelファイル
└── WorkExcelToPdf.csproj      // プロジェクトファイル
```

## 使い方

### 1. 基本的な使い方

```bash
# Template.xlsxを読み込んでoutput.pdfを生成
dotnet run

# 特定のファイルを指定
dotnet run -- "C:\path\to\your\file.xlsx"

# 出力PDFファイル名も指定
dotnet run -- "input.xlsx" "output.pdf"
```

### 2. プログラムからの使用

#### 罫線情報の取得

```csharp
var reader = new ExcelBorderReader();
var sheetInfo = reader.ReadSheetWithBorders("Template.xlsx");

Console.WriteLine($"シート名: {sheetInfo.SheetName}");
Console.WriteLine($"セル数: {sheetInfo.Cells.Count}");

foreach (var cellInfo in sheetInfo.Cells)
{
    Console.WriteLine($"セル {cellInfo.CellAddress}: {cellInfo.Value}");
    
    if (cellInfo.Border.Top.HasBorder)
    {
        Console.WriteLine($"  上辺: {cellInfo.Border.Top.LineStyle}");
    }
}
```

#### PDF生成

```csharp
// Excelファイルから情報を読み取る
var readerEx = new ExcelBorderReaderEx();
var sheetInfoEx = readerEx.ReadSheet("input.xlsx");

// PDFを生成
var pdfGenerator = new PdfGenerator();
pdfGenerator.GeneratePdf(sheetInfoEx, "output.pdf");
```

### 3. サンプルExcelファイルの作成

`SampleExcelCreator.cs`を使用して、テスト用のExcelファイルを作成できます:

```csharp
// 基本的な罫線サンプルを作成
SampleExcelCreator.CreateSampleExcel("sample.xlsx");

// 複雑な罫線パターンのサンプルを作成
SampleExcelCreator.CreateComplexBorderSample("complex.xlsx");
```

## クラス構成

### Program.cs

#### SheetInfo
シート全体の情報を保持

- `SheetName`: シート名
- `UsedRange`: 使用範囲
- `Cells`: セル情報のリスト

#### CellInfo
個々のセルの情報を保持

- `Row`, `Column`: セル位置
- `CellAddress`: セルアドレス（例: "A1"）
- `Value`: セルの値
- `Width`, `Height`: セルのサイズ（ピクセル）
- `Border`: 罫線情報

#### CellBorderInfo
セルの罫線情報を保持

- `Top`, `Bottom`, `Left`, `Right`: 四辺の罫線
- `DiagonalUp`, `DiagonalDown`: 斜め線

#### BorderSideInfo
罫線の一辺の詳細情報

- `HasBorder`: 罫線の有無
- `LineStyle`: 線のスタイル名
- `Color`: 線の色（16進数）
- `Width`: 線の幅（ポイント）

### ExcelToPdfConverter.cs

#### ExcelBorderReaderEx
PDF生成用に拡張されたExcel読み取りクラス

- セルの正確な位置とサイズを計算
- フォント情報、配置情報を取得
- 背景色情報を取得

#### PdfGenerator
PDF生成クラス

- 罫線の描画（上下左右、斜め線）
- テキストの描画（フォント、配置、色）
- 背景色の描画
- 線のスタイル変換（実線、破線、点線など）

## 出力例

```
=== Excel to PDF Converter ===

Excelファイルを読み込んでいます: Template.xlsx

シート名: 罫線サンプル
使用範囲: A1:C12
罫線が設定されているセル数: 15
====================================================================================================

罫線の統計情報:
  細線: 48箇所
  中線: 12箇所
  太線: 8箇所

====================================================================================================

PDF生成を開始します...
シート名: 罫線サンプル
セル数: 42
  背景色: 3個, テキスト: 15個, 罫線: 38個
? PDFを生成しました: output.pdf

====================================================================================================
? 処理が完了しました！
  入力: Template.xlsx
  出力: output.pdf
====================================================================================================
```

## PDF生成の特徴

### サポートされる機能

? **罫線スタイル**
- 実線（細線、中線、太線）
- 点線
- 破線
- 一点鎖線、二点鎖線
- 二重線
- 斜め線

? **セル情報**
- テキスト内容
- フォント（名前、サイズ、太字、斜体）
- テキスト配置（左、中央、右、上、中、下）
- 背景色
- 前景色（テキスト色）

? **レイアウト**
- セルの正確な位置とサイズ
- 列幅・行高の再現
- マージン設定

### 現在の制限事項

- 単一ページのみ対応（A4サイズ）
- セルの結合は未対応
- 画像・グラフは未対応
- グラデーション、パターンは未対応
- 数式の計算は未対応（表示値のみ）

## 今後の拡張予定

1. **複数ページ対応**
   - 大きな表を複数ページに分割
   - ページサイズの自動調整

2. **セルの結合対応**
   - マージされたセルの検出と描画

3. **画像・グラフの対応**
   - Excelに埋め込まれた画像の抽出と配置
   - グラフのPDF変換

4. **高度な書式設定**
   - 条件付き書式
   - データバー、カラースケール
   - グラデーション背景

5. **最適化**
   - 大きなファイルのメモリ効率化
   - 処理速度の向上

## 注意事項

- Excel列幅・行高のピクセル変換は概算値です
- 複雑な書式設定（グラデーション、パターンなど）は未対応
- マクロやVBAコードは読み取りません
- PDFのサイズは固定（A4縦向き）です

## トラブルシューティング

### ファイルが見つからない

```
ファイルが見つかりません: Template.xlsx
```

→ 実行ディレクトリにExcelファイルをコピーするか、フルパスで指定してください。

### PDF生成エラー

```
背景色描画エラー (A1): ...
```

→ 一部のセルで描画エラーが発生しても、処理は継続されます。警告メッセージを確認してください。

### フォントが見つからない

```
テキスト描画エラー (B2): Font not found
```

→ Excelで使用されているフォントがシステムにインストールされているか確認してください。

## ライセンス

このサンプルコードは自由に使用・改変できます。

# Excel to PDF Converter

ClosedXMLとPDFSharpを使用してExcelファイルをPDFに変換するツールです。

## 機能概要

### サポートされている機能

#### 1. セルの基本表示
- ✅ セルの値の表示
- ✅ セルの結合
- ✅ セルの配置（左揃え、中央揃え、右揃え）
- ✅ 垂直方向の配置（上揃え、中央揃え、下揃え）
- ✅ インデント
- ✅ テキストの回転

#### 2. フォントスタイル
- ✅ フォント名
- ✅ フォントサイズ
- ✅ **太字（Bold）** - 疑似Bold対応
- ✅ **斜体（Italic）** - 疑似Italic対応
- ✅ 下線
- ✅ 取り消し線
- ✅ フォント色

#### 3. 背景色
- ✅ セルの背景色
- ✅ パターン塗りつぶし（Solid）
- ✅ テーマ色の対応

#### 4. 罫線
- ✅ 上下左右の罫線
- ✅ 斜め線（対角線）
- ✅ 罫線の太さ（Thin, Medium, Thick等）
- ✅ 罫線のスタイル（実線、点線、破線等）
- ✅ 罫線の色

#### 5. PDFオプション
- ✅ ページサイズ（A4、Letter等）
- ✅ ページの向き（縦、横）
- ✅ 余白設定
- ✅ 拡大・縮小（Scale）
- ✅ ヘッダー・フッター
- ✅ ページ番号
- ✅ 複数ページ対応
- ✅ グリッド線表示
- ✅ デバッグモード

## サイズ・単位の計算

### Excelの単位系

#### 列幅（Column Width）
- **単位**: 文字数（Character Units）
- **基準**: デフォルトフォントの「0」文字の幅
- **例**: `Width = 7.5` = 「0」文字が7.5個分入る幅

#### 行高（Row Height）
- **単位**: ポイント（Points）
- **定義**: 1ポイント = 1/72インチ
- **例**: `Height = 15` = 15ポイント ≈ 5.29mm

### PDFの単位系

#### 座標系
- **単位**: ポイント（Points）
- **定義**: 1ポイント = 1/72インチ
- **原点**: 左上が (0, 0)

#### A4サイズの例
```
A4 = 210mm × 297mm

ポイント換算:
幅: 210mm ÷ 25.4 × 72 = 595.28 pt
高さ: 297mm ÷ 25.4 × 72 = 841.89 pt
```

### 変換プロセス

#### 全体の流れ
```
Excel → 中間形式（ピクセル） → PDF（ポイント）
```

#### 1. Excel読み込み時の変換

```csharp
// 列幅: 文字数 → ピクセル
ColumnWidths[col] = column.Width * 7;

// 行高: ポイント → ピクセル (96 DPI想定)
RowHeights[row] = row.Height * 1.33;
// 正確には: row.Height * 96 / 72
```

**変換係数の根拠:**
- **列幅 × 7**: 経験的な値。1文字幅 ≈ 7ピクセル
- **行高 × 1.33**: 96 DPI換算
  - 1ポイント = 96/72 ピクセル = 1.333... ピクセル

#### 2. PDF描画時の変換

```csharp
// ピクセル → ポイント
private const double PixelToPoint = 0.75;

// 描画座標の計算
double x = marginLeft + cell.X * PixelToPoint;
double y = marginTop + cell.Y * PixelToPoint;
double width = cell.Width * PixelToPoint;
double height = cell.Height * PixelToPoint;
```

**PixelToPoint = 0.75の理由:**
- 96 DPI (Windowsの標準DPI)を前提
- 1インチ = 96ピクセル = 72ポイント
- 1ピクセル = 72/96 ポイント = 0.75ポイント

### DPIと単位の関係

| DPI | 1インチあたりのピクセル数 | ピクセル/ポイント比率 |
|-----|------------------------|-------------------|
| 72  | 72 pixels              | 1.0               |
| 96  | 96 pixels              | 1.333 (4/3)       |
| 120 | 120 pixels             | 1.667 (5/3)       |
| 144 | 144 pixels             | 2.0               |

**本ツールは96 DPIを想定しています。**

### 具体的な計算例

#### 例1: 列幅 7.5

```
1. Excel読み込み
   column.Width = 7.5 (文字数単位)

2. ピクセルに変換
   ColumnWidths[col] = 7.5 × 7 = 52.5 px

3. PDF描画時にポイントに変換
   width = 52.5 × 0.75 = 39.375 pt

4. PDF上での表示
   約 39.375 / 72 × 25.4 = 13.9mm
```

#### 例2: 行高 15

```
1. Excel読み込み
   row.Height = 15 (ポイント単位)

2. ピクセルに変換
   RowHeights[row] = 15 × 1.33 = 20 px

3. PDF描画時にポイントに戻す
   height = 20 × 0.75 = 15 pt

4. PDF上での表示
   15 / 72 × 25.4 = 5.29mm (元の値に戻る)
```

## フォントスタイルの実装

### 疑似Bold（太字）の実装

日本語フォント（IPAフォント）は単一ウェイトのため、疑似的に太字を実現しています。

```csharp
// 元の位置に描画
gfx.DrawString(text, font, brush, rect, format);

// 0.3ポイントずらして重ね描き（2回）
gfx.DrawString(text, font, brush, rect + (0.3, 0), format);
gfx.DrawString(text, font, brush, rect + (0, 0.3), format);
```

**効果**: 3回重ね描きすることで太く見える

### 疑似Italic（斜体）の実装

Skew変換を使用して文字を傾けます。

```csharp
// Skew変換行列を作成
var matrix = new XMatrix();
matrix.TranslatePrepend(rect.X, rect.Y);
matrix.SkewPrepend(-15, 0);  // 15度右に傾ける
matrix.TranslatePrepend(-rect.X, -rect.Y);
gfx.MultiplyTransform(matrix);

// 傾けた状態で描画
gfx.DrawString(text, font, brush, rect, format);
```

**傾斜角度**: -15度（右に傾く）

### 疑似スタイルの有効化/無効化

```csharp
var options = new PdfGenerationOptions
{
    EnableSimulatedBold = true,    // 疑似Boldを有効化
    EnableSimulatedItalic = true,  // 疑似Italicを有効化
};
```

## 色の処理

### Excelの色タイプ

| 色タイプ | 説明 | 処理方法 |
|---------|------|---------|
| Color | 直接RGB指定 | そのまま使用 |
| Indexed | インデックス色 | RGB値に変換 |
| Theme | テーマ色 | RGB値に変換、または代替色を使用 |

### 色の取得ロジック

```csharp
private string GetColorHex(XLColor color, bool isBackgroundColor = false)
{
    if (color.ColorType == XLColorType.Color) {
        // 直接RGB指定
        return $"#{color.Color.ToArgb() & 0xFFFFFF:X6}";
    }
    else if (color.ColorType == XLColorType.Theme) {
        // テーマ色の処理
        if (isBackgroundColor) {
            // 背景色: 白→アクセント色
            if (argb == 0xFFFFFF) return "#4472C4";
        } else {
            // フォント色・罫線色: 白→黒
            if (argb == 0xFFFFFF) return "#000000";
        }
    }
}
```

### デフォルト色

| 要素 | デフォルト色 |
|------|------------|
| フォント色 | #000000（黒） |
| 罫線色 | #000000（黒） |
| 背景色（テーマ色で白の場合） | #4472C4（Excel既定アクセント色） |

## 罫線の処理

### 罫線の太さ

| Excelのスタイル | 幅（ポイント） |
|----------------|--------------|
| None | 0 |
| Hair | 0.25 |
| Thin | 0.5 |
| Medium | 1.5 |
| Thick | 2.5 |
| Double | 2.0 |

### 罫線のスタイル

| Excelのスタイル | PDFのスタイル |
|----------------|-------------|
| Solid | Solid |
| Dotted | Dot |
| Dashed | Dash |
| DashDot | DashDot |
| DashDotDot | DashDotDot |

### 描画順序

罫線は太さ順に描画されます（細い線→太い線）。これにより、太い罫線が細い罫線の上に描画され、Excelの表示に近づきます。

## PDF生成オプション

### PdfGenerationOptions

```csharp
var options = new PdfGenerationOptions
{
    // ページ設定
    PageSize = PdfSharp.PageSize.A4,
    Orientation = PdfSharp.PageOrientation.Portrait,
    
    // 余白（ポイント単位）
    MarginLeft = 30,
    MarginTop = 30,
    MarginRight = 30,
    MarginBottom = 30,
    
    // 拡大縮小
    Scale = 1.0,  // 1.0 = 100%, 0.5 = 50%
    
    // ページ分割
    EnableAutoPageBreak = false,  // 自動ページ分割
    
    // ヘッダー・フッター
    ShowPageNumber = false,
    PageNumberFormat = "Page {0}",
    HeaderText = "",
    FooterText = "",
    
    // 表示オプション
    ShowGridLines = false,         // グリッド線表示
    IgnoreBackgroundColor = false, // 背景色を無視
    IgnoreMergedCells = false,     // セル結合を無視
    
    // フォントスタイル（疑似処理）
    EnableSimulatedBold = true,    // 疑似Bold
    EnableSimulatedItalic = true,  // 疑似Italic
    
    // デバッグ
    DebugMode = false,  // セル境界線を赤点線で表示
};
```

## 使用方法

### 基本的な使い方

```csharp
// Excel読み込み
var reader = new ExcelBorderReaderEx();
var sheetInfo = reader.ReadSheet("input.xlsx");

// PDF生成
var generator = new PdfGenerator();
generator.GeneratePdf(sheetInfo, "output.pdf");
```

### オプション指定

```csharp
var options = new PdfGenerationOptions
{
    PageSize = PdfSharp.PageSize.A4,
    Orientation = PdfSharp.PageOrientation.Landscape,
    Scale = 0.9,
    EnableSimulatedBold = true,
    EnableSimulatedItalic = true,
};

var generator = new PdfGenerator(options);
generator.GeneratePdf(sheetInfo, "output.pdf");
```

### デバッグモードでExcelファイルを分析

```csharp
// プログラム実行時にモード選択
// 2を選択するとExcelファイルの詳細分析が表示される
dotnet run

モードを選択してください:
  1: PDF生成モード（通常）
  2: デバッグモード（Excel詳細分析）
> 2
```

## トラブルシューティング

### フォントが表示されない

**原因**: `ipaexm.ttf`フォントファイルが見つからない

**解決策**:
1. IPAexフォントをダウンロード
2. プロジェクトディレクトリに`ipaexm.ttf`を配置
3. プロパティで「出力ディレクトリにコピー」を「常にコピーする」に設定

### 罫線が表示されない

**原因**: 罫線の色が白になっている

**解決策**:
- コードが自動的に白→黒に変換するように修正済み
- `GetColorHex`メソッドが罫線色の白を黒に変換

### 背景色が表示されない

**原因**: `PatternType`が`None`になっている

**確認方法**:
```csharp
// デバッグモードで確認
// セルの背景色情報を確認
背景色: PatternColor:#XXXXXX (Pattern:Solid)
```

### Bold/Italicが反映されない

**原因**: 疑似スタイルが無効化されている

**解決策**:
```csharp
var options = new PdfGenerationOptions
{
    EnableSimulatedBold = true,
    EnableSimulatedItalic = true,
};
```

## 制限事項

### 現在サポートされていない機能

- ❌ 画像の埋め込み
- ❌ グラフ・チャート
- ❌ 条件付き書式
- ❌ データの入力規則
- ❌ ハイパーリンク
- ❌ コメント・メモ
- ❌ セルの保護
- ❌ マクロ
- ❌ ピボットテーブル

### 部分的にサポート

- ⚠️ フォントスタイル: 疑似Bold/Italicのみ（実際のBold/Italicフォントは未対応）
- ⚠️ 背景色: Solidパターンのみ（グラデーション等は未対応）
- ⚠️ 罫線: 基本スタイルのみ（一部の特殊スタイルは未対応）

## 技術スタック

- **.NET 10**
- **ClosedXML**: Excelファイルの読み込み
- **PDFSharp**: PDF生成
- **IPAexフォント**: 日本語表示

## ライセンス

このプロジェクトのライセンスについては、プロジェクトのルートディレクトリにあるLICENSEファイルを参照してください。

## 参考資料

### 単位換算

```
1インチ = 25.4mm
1ポイント = 1/72インチ ≈ 0.35mm
1ピクセル (96 DPI) = 1/96インチ ≈ 0.26mm

変換式:
mm → ポイント: mm ÷ 25.4 × 72
ポイント → mm: ポイント × 25.4 ÷ 72
ピクセル → ポイント (96 DPI): ピクセル × 0.75
ポイント → ピクセル (96 DPI): ポイント × 1.333...
```

### Excelの列幅について

Excelの列幅は複雑な計算式で決まります:
- デフォルト幅: 8.43（「0」文字が8.43個分）
- 実際のピクセル幅 ≈ 列幅 × 文字幅 + パディング
- 本ツールでは簡易的に `列幅 × 7` で計算

詳細: [Microsoft Docs - Column Width](https://docs.microsoft.com/en-us/office/troubleshoot/excel/determine-column-widths)

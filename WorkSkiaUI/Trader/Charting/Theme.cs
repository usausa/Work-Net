using SkiaSharp;

namespace Trader.Charting;

// TradingView ダークテーマ準拠の配色。
// WPF 側 (App.xaml のブラシ) と同じ色を SkiaSharp 用に定義しており、
// ツールバーとチャートで見た目が揃うようにしている。
public static class Theme
{
    // 背景・枠線・グリッド
    public static readonly SKColor Bg         = new(0x13, 0x17, 0x22);
    public static readonly SKColor Grid       = new(0x2A, 0x2E, 0x39, 0x80);
    public static readonly SKColor Border     = new(0x2A, 0x2E, 0x39);

    // 文字色。AxisText は軸ラベル、Bright は強調、Dim はラベル見出し用。
    public static readonly SKColor AxisText   = new(0xB2, 0xB5, 0xBE);
    public static readonly SKColor TextBright = new(0xF0, 0xF3, 0xFA);
    public static readonly SKColor TextDim    = new(0x78, 0x7B, 0x86);

    // 騰落色。Dim 側は出来高バー用に透過させたもの。
    public static readonly SKColor Up         = new(0x26, 0xA6, 0x9A);
    public static readonly SKColor Down       = new(0xEF, 0x53, 0x50);
    public static readonly SKColor UpDim      = new(0x26, 0xA6, 0x9A, 0x55);
    public static readonly SKColor DownDim    = new(0xEF, 0x53, 0x50, 0x55);

    // オーバーレイ要素 (クロスヘア、軸チップ、ウォーターマーク)
    public static readonly SKColor Crosshair  = new(0x75, 0x86, 0x96);
    public static readonly SKColor ChipBg     = new(0x36, 0x3A, 0x45);
    public static readonly SKColor Watermark  = new(0xFF, 0xFF, 0xFF, 0x12);

    // 系列色。Line はライン/エリアチャート本体、Sma/Ema はインジケーター。
    public static readonly SKColor Line       = new(0x29, 0x62, 0xFF);
    public static readonly SKColor Sma        = new(0xFF, 0x98, 0x00);
    public static readonly SKColor Ema        = new(0x29, 0xB6, 0xF6);
}

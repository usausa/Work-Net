using SkiaSharp;

namespace EnergyLineHmi.Rendering;

/// <summary>HMI 全体の配色とフォント。</summary>
public static class Theme
{
    // 背景・パネル
    public static readonly SKColor Bg          = new(0x0B, 0x11, 0x18);
    public static readonly SKColor HeaderBg    = new(0x0F, 0x17, 0x22);
    public static readonly SKColor PanelBg     = new(0x10, 0x17, 0x21);
    public static readonly SKColor PanelBorder = new(0x24, 0x31, 0x42);
    public static readonly SKColor CardBg      = new(0x16, 0x1F, 0x2B);
    public static readonly SKColor GridDot     = new(0x18, 0x22, 0x2E);
    public static readonly SKColor PipeCasing  = new(0x26, 0x32, 0x40);
    public static readonly SKColor BadgeBg     = new(0x0D, 0x14, 0x1D);

    // 機器ボックス
    public static readonly SKColor BoxTop    = new(0x1C, 0x27, 0x35);
    public static readonly SKColor BoxBottom = new(0x12, 0x1A, 0x25);
    public static readonly SKColor BoxBorder = new(0x3A, 0x4B, 0x5F);

    // テキスト
    public static readonly SKColor TextMain = new(0xE6, 0xED, 0xF3);
    public static readonly SKColor TextDim  = new(0x8A, 0x9B, 0xAC);
    public static readonly SKColor Accent   = new(0x4F, 0xC3, 0xF7);

    // エネルギーライン色
    public static readonly SKColor Electric   = new(0xFF, 0xD5, 0x4F);
    public static readonly SKColor Gas        = new(0x64, 0xB5, 0xF6);
    public static readonly SKColor Steam      = new(0xFF, 0x8A, 0x65);
    public static readonly SKColor Condensate = new(0x4D, 0xD0, 0xE1);
    public static readonly SKColor CoolWater  = new(0x26, 0xA6, 0x9A);
    public static readonly SKColor Air        = new(0x90, 0xA4, 0xAE);
    public static readonly SKColor GenGreen   = new(0x81, 0xC7, 0x84);

    // 状態色
    public static readonly SKColor StateRun   = new(0x66, 0xBB, 0x6A);
    public static readonly SKColor StateStop  = new(0x78, 0x90, 0x9C);
    public static readonly SKColor StateAlarm = new(0xFF, 0x52, 0x52);
    public static readonly SKColor Warn       = new(0xFF, 0xB7, 0x4D);

    public static readonly SKTypeface Jp =
        SKTypeface.FromFamilyName("Yu Gothic UI", SKFontStyle.Normal) ?? SKTypeface.Default;
    public static readonly SKTypeface JpBold =
        SKTypeface.FromFamilyName("Yu Gothic UI", SKFontStyle.Bold) ?? SKTypeface.Default;
}

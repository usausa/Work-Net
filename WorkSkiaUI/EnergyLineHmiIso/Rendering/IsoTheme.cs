using SkiaSharp;

namespace EnergyLineHmiIso.Rendering;

/// <summary>近未来 HMI の配色・フォント・グロー用マスクフィルタ。</summary>
public static class IsoTheme
{
    // 背景・パネル
    public static readonly SKColor Bg        = new(0x05, 0x08, 0x0F);
    public static readonly SKColor BgGlow    = new(0x0B, 0x16, 0x26);
    public static readonly SKColor GridLine  = new(0x10, 0x30, 0x48);
    public static readonly SKColor PanelBg   = new(0x0A, 0x12, 0x1E, 0xC8);
    public static readonly SKColor PanelEdge = new(0x1E, 0x3A, 0x52);
    public static readonly SKColor CardBg    = new(0x0E, 0x18, 0x26, 0xE0);
    public static readonly SKColor PipeBase  = new(0x0E, 0x18, 0x24);

    // テキスト・アクセント
    public static readonly SKColor Cyan     = new(0x00, 0xE5, 0xFF);
    public static readonly SKColor TextMain = new(0xE8, 0xF4, 0xFF);
    public static readonly SKColor TextDim  = new(0x7E, 0x97, 0xAD);

    // エネルギーライン色
    public static readonly SKColor Electric = new(0xFF, 0xD7, 0x40);
    public static readonly SKColor Gas      = new(0x4F, 0xA8, 0xFF);
    public static readonly SKColor Steam    = new(0xFF, 0x7A, 0x59);
    public static readonly SKColor Water    = new(0x34, 0xE1, 0xE8);
    public static readonly SKColor Cool     = new(0x2B, 0xD9, 0xA9);
    public static readonly SKColor Green    = new(0x5A, 0xF2, 0xA6);

    // 状態色
    public static readonly SKColor Warn  = new(0xFF, 0xB5, 0x47);
    public static readonly SKColor Alarm = new(0xFF, 0x4D, 0x6A);
    public static readonly SKColor Stop  = new(0x6E, 0x82, 0x95);

    public static readonly SKTypeface Jp =
        SKTypeface.FromFamilyName("Yu Gothic UI", SKFontStyle.Normal) ?? SKTypeface.Default;
    public static readonly SKTypeface JpBold =
        SKTypeface.FromFamilyName("Yu Gothic UI", SKFontStyle.Bold) ?? SKTypeface.Default;

    // グロー（ぼかし）フィルタ：生成コストが高いので共有する
    public static readonly SKMaskFilter GlowS = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2.5f);
    public static readonly SKMaskFilter GlowM = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 5f);
    public static readonly SKMaskFilter GlowL = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 11f);
}

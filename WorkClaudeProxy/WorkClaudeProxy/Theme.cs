using SkiaSharp;

namespace WorkClaudeProxy;

internal sealed class Theme
{
    public required SKColor BgColor       { get; init; }
    public required SKColor HeaderBg      { get; init; }
    public required SKColor BorderColor   { get; init; }
    public required SKColor TextPrimary   { get; init; }
    public required SKColor TextSecondary { get; init; }
    public required SKColor AccentColor   { get; init; }
    public required SKColor ColorGood     { get; init; }
    public required SKColor ColorWarn     { get; init; }
    public required SKColor ColorError    { get; init; }
    public required SKColor BarBgColor    { get; init; }
    public required string  FontFamily    { get; init; }

    // ── GitHub Dark ──────────────────────────────────────────────────────────
    // public static readonly Theme GitHubDark = new()
    // {
    //     BgColor       = new(0x0D, 0x11, 0x17),
    //     HeaderBg      = new(0x16, 0x1B, 0x22),
    //     BorderColor   = new(0x30, 0x36, 0x3D),
    //     TextPrimary   = new(0xE6, 0xED, 0xF3),
    //     TextSecondary = new(0x8B, 0x94, 0x9E),
    //     AccentColor   = new(0x58, 0xA6, 0xFF),
    //     ColorGood     = new(0x3F, 0xB9, 0x50),
    //     ColorWarn     = new(0xD2, 0x99, 0x22),
    //     ColorError    = new(0xF8, 0x51, 0x49),
    //     BarBgColor    = new(0x21, 0x26, 0x2D),
    //     FontFamily    = "Consolas",
    // };

    // ── Claude Code ──────────────────────────────────────────────────────────
    public static readonly Theme ClaudeCode = new()
    {
        BgColor       = new(0x0D, 0x0D, 0x0D),
        HeaderBg      = new(0x1C, 0x1C, 0x1C),
        BorderColor   = new(0x33, 0x33, 0x33),
        TextPrimary   = new(0xF0, 0xED, 0xE8),
        TextSecondary = new(0x70, 0x6B, 0x65),
        AccentColor   = new(0xDA, 0x77, 0x56),
        ColorGood     = new(0x57, 0xA7, 0x73),
        ColorWarn     = new(0xD9, 0xA3, 0x1C),
        ColorError    = new(0xD9, 0x52, 0x52),
        BarBgColor    = new(0x24, 0x24, 0x24),
        FontFamily    = "Consolas",
    };
}

using SkiaSharp;

namespace WorkCar;

public static class HudColors
{
    public static readonly SKColor Bg        = new(0x04, 0x07, 0x0D);
    public static readonly SKColor Cyan      = new(0x00, 0xE5, 0xFF);
    public static readonly SKColor Azure     = new(0x2E, 0x9B, 0xFF);
    public static readonly SKColor Amber     = new(0xFF, 0xB3, 0x20);
    public static readonly SKColor Red       = new(0xFF, 0x2D, 0x40);
    public static readonly SKColor Green     = new(0x2E, 0xFF, 0x9E);
    public static readonly SKColor Dim       = new(0x5E, 0x8C, 0xA8);
    public static readonly SKColor Panel     = new(0x0A, 0x14, 0x21);
    public static readonly SKColor PanelLine = new(0x1C, 0x3A, 0x52);
    public static readonly SKColor White     = new(0xE8, 0xF6, 0xFF);
}

/// <summary>グロー付きテキスト・7セグ数字・小型計器などの共通描画部品。</summary>
public static class HudParts
{
    public static readonly SKTypeface LabelFace =
        SKTypeface.FromFamilyName("Bahnschrift", SKFontStyle.Bold);
    public static readonly SKTypeface MonoFace =
        SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);

    static readonly SKPaint Fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    static readonly SKPaint Stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    static readonly SKPaint Text = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    static readonly Dictionary<int, SKMaskFilter> Blurs = new();

    public static SKMaskFilter Blur(float sigma)
    {
        int key = (int)(sigma * 4f);
        if (!Blurs.TryGetValue(key, out var f))
            Blurs[key] = f = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, MathF.Max(0.5f, sigma));
        return f;
    }

    public static SKPoint Polar(float cx, float cy, float r, float deg)
    {
        float rad = deg * MathF.PI / 180f;
        return new SKPoint(cx + r * MathF.Cos(rad), cy + r * MathF.Sin(rad));
    }

    // ---------------- テキスト ----------------

    public static void DrawText(SKCanvas c, string s, float x, float y, float size,
        SKColor color, SKTextAlign align = SKTextAlign.Center, SKTypeface? face = null)
    {
        Text.Typeface = face ?? LabelFace;
        Text.TextSize = size;
        Text.TextAlign = align;
        Text.Color = color;
        Text.MaskFilter = null;
        c.DrawText(s, x, y, Text);
    }

    public static void DrawGlowText(SKCanvas c, string s, float x, float y, float size,
        SKColor color, SKTextAlign align = SKTextAlign.Center, SKTypeface? face = null,
        float glow = 0f)
    {
        Text.Typeface = face ?? LabelFace;
        Text.TextSize = size;
        Text.TextAlign = align;
        if (glow <= 0f) glow = size * 0.18f;
        Text.Color = color.WithAlpha(150);
        Text.MaskFilter = Blur(glow);
        c.DrawText(s, x, y, Text);
        Text.Color = color;
        Text.MaskFilter = null;
        c.DrawText(s, x, y, Text);
    }

    // ---------------- 線・弧・パネル ----------------

    public static void DrawArc(SKCanvas c, float cx, float cy, float r,
        float startDeg, float sweepDeg, float width, SKColor color, float glow = 0f)
    {
        var rect = new SKRect(cx - r, cy - r, cx + r, cy + r);
        Stroke.StrokeWidth = width;
        Stroke.StrokeCap = SKStrokeCap.Butt;
        if (glow > 0f)
        {
            Stroke.Color = color.WithAlpha(120);
            Stroke.MaskFilter = Blur(glow);
            c.DrawArc(rect, startDeg, sweepDeg, false, Stroke);
        }
        Stroke.Color = color;
        Stroke.MaskFilter = null;
        c.DrawArc(rect, startDeg, sweepDeg, false, Stroke);
    }

    public static void DrawLine(SKCanvas c, float x0, float y0, float x1, float y1,
        float width, SKColor color, float glow = 0f, SKStrokeCap cap = SKStrokeCap.Round)
    {
        Stroke.StrokeWidth = width;
        Stroke.StrokeCap = cap;
        if (glow > 0f)
        {
            Stroke.Color = color.WithAlpha(120);
            Stroke.MaskFilter = Blur(glow);
            c.DrawLine(x0, y0, x1, y1, Stroke);
        }
        Stroke.Color = color;
        Stroke.MaskFilter = null;
        c.DrawLine(x0, y0, x1, y1, Stroke);
    }

    public static void FillCircle(SKCanvas c, float cx, float cy, float r, SKColor color, float glow = 0f)
    {
        if (glow > 0f)
        {
            Fill.Color = color.WithAlpha(130);
            Fill.MaskFilter = Blur(glow);
            c.DrawCircle(cx, cy, r, Fill);
        }
        Fill.Color = color;
        Fill.MaskFilter = null;
        c.DrawCircle(cx, cy, r, Fill);
    }

    /// <summary>角を斜めに落とした八角形パネル。</summary>
    public static void DrawCutPanel(SKCanvas c, float x, float y, float w, float h,
        float cut, SKColor fill, SKColor border, float borderWidth = 1.5f)
    {
        using var path = new SKPath();
        path.MoveTo(x + cut, y);
        path.LineTo(x + w - cut, y);
        path.LineTo(x + w, y + cut);
        path.LineTo(x + w, y + h - cut);
        path.LineTo(x + w - cut, y + h);
        path.LineTo(x + cut, y + h);
        path.LineTo(x, y + h - cut);
        path.LineTo(x, y + cut);
        path.Close();
        Fill.Color = fill;
        Fill.MaskFilter = null;
        c.DrawPath(path, Fill);
        Stroke.Color = border;
        Stroke.StrokeWidth = borderWidth;
        Stroke.StrokeCap = SKStrokeCap.Butt;
        Stroke.MaskFilter = null;
        c.DrawPath(path, Stroke);
    }

    // ---------------- 7セグメント数字 ----------------

    static readonly Dictionary<char, int> SegMap = new()
    {
        ['0'] = 0b0111111, ['1'] = 0b0000110, ['2'] = 0b1011011, ['3'] = 0b1001111,
        ['4'] = 0b1100110, ['5'] = 0b1101101, ['6'] = 0b1111101, ['7'] = 0b0000111,
        ['8'] = 0b1111111, ['9'] = 0b1101111, ['-'] = 0b1000000, [' '] = 0,
    };

    public static float SevenSegWidth(float h, int chars)
        => chars * (h * 0.58f + h * 0.17f) - h * 0.17f;

    /// <summary>左上(x,y)基準・高さhの斜体7セグ数字列を描く。</summary>
    public static void DrawSevenSeg(SKCanvas c, float x, float y, float h, string text,
        SKColor color, bool ghost = true)
    {
        float w = h * 0.58f;
        float gap = h * 0.17f;
        float t = h * 0.115f;

        c.Save();
        c.Translate(x + h * 0.06f, y);
        c.Skew(-0.07f, 0f); // 軽い前傾イタリック

        float dx = 0f;
        foreach (char ch in text)
        {
            if (ghost) DrawSegDigit(c, dx, 0f, w, h, t, 0b1111111, color.WithAlpha(16), 0f);
            if (SegMap.TryGetValue(ch, out int bits) && bits != 0)
            {
                DrawSegDigit(c, dx, 0f, w, h, t, bits, color.WithAlpha(110), h * 0.075f);
                DrawSegDigit(c, dx, 0f, w, h, t, bits, color, 0f);
            }
            dx += w + gap;
        }
        c.Restore();
    }

    static void DrawSegDigit(SKCanvas c, float x, float y, float w, float h, float t,
        int bits, SKColor color, float glow)
    {
        Stroke.StrokeWidth = t;
        Stroke.StrokeCap = SKStrokeCap.Round;
        Stroke.Color = color;
        Stroke.MaskFilter = glow > 0f ? Blur(glow) : null;
        float m = t * 0.78f;
        float hy = h / 2f;
        if ((bits & 1) != 0) c.DrawLine(x + m, y, x + w - m, y, Stroke);                  // A
        if ((bits & 2) != 0) c.DrawLine(x + w, y + m, x + w, y + hy - m, Stroke);          // B
        if ((bits & 4) != 0) c.DrawLine(x + w, y + hy + m, x + w, y + h - m, Stroke);      // C
        if ((bits & 8) != 0) c.DrawLine(x + m, y + h, x + w - m, y + h, Stroke);           // D
        if ((bits & 16) != 0) c.DrawLine(x, y + hy + m, x, y + h - m, Stroke);             // E
        if ((bits & 32) != 0) c.DrawLine(x, y + m, x, y + hy - m, Stroke);                 // F
        if ((bits & 64) != 0) c.DrawLine(x + m, y + hy, x + w - m, y + hy, Stroke);        // G
        Stroke.MaskFilter = null;
    }

    // ---------------- 小型計器(下段の温度・油圧など) ----------------

    public static void DrawMiniGauge(SKCanvas c, float cx, float cy,
        string label, string value, string unit, float frac, bool warn,
        SKColor accent, float t)
    {
        const float W = 224f, H = 170f;
        float x = cx - W / 2f, y = cy - H / 2f;
        bool flash = warn && MathF.Sin(t * 12f) > -0.2f;
        var col = warn ? HudColors.Red : accent;
        var border = warn
            ? HudColors.Red.WithAlpha((byte)(flash ? 200 : 90))
            : HudColors.PanelLine;

        DrawCutPanel(c, x, y, W, H, 16f, HudColors.Panel.WithAlpha(215), border);

        // 角のアクセント
        DrawLine(c, x + 6, y + 14, x + 6, y + 38, 2.5f, col.WithAlpha(160), 0f, SKStrokeCap.Butt);
        DrawLine(c, x + W - 6, y + H - 38, x + W - 6, y + H - 14, 2.5f, col.WithAlpha(160), 0f, SKStrokeCap.Butt);

        DrawText(c, label, cx, y + 30f, 20f, HudColors.Dim);

        // 上半円のアークゲージ
        float gx = cx, gy = cy + 26f, r = 56f;
        frac = Math.Clamp(frac, 0f, 1f);
        DrawArc(c, gx, gy, r, 180f, 180f, 7f, HudColors.PanelLine.WithAlpha(140));
        if (frac > 0.005f)
            DrawArc(c, gx, gy, r, 180f, 180f * frac, 7f, flash ? HudColors.Red : col, 6f);

        // 針先ドット
        var p = Polar(gx, gy, r, 180f + 180f * frac);
        FillCircle(c, p.X, p.Y, 4.5f, HudColors.White, 5f);

        // 目盛
        for (int i = 0; i <= 4; i++)
        {
            var p0 = Polar(gx, gy, r + 9f, 180f + 45f * i);
            var p1 = Polar(gx, gy, r + 15f, 180f + 45f * i);
            DrawLine(c, p0.X, p0.Y, p1.X, p1.Y, 2f, HudColors.Dim.WithAlpha(150), 0f, SKStrokeCap.Butt);
        }

        DrawGlowText(c, value, cx, gy + 8f, 34f, flash ? HudColors.Red : HudColors.White,
            SKTextAlign.Center, MonoFace, 5f);
        DrawText(c, unit, cx, gy + 32f, 17f, HudColors.Dim);
    }
}

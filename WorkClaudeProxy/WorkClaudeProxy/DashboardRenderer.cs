using SkiaSharp;

namespace WorkClaudeProxy;

internal static class DashboardRenderer
{
    private const int W       = 1280;
    private const int H       = 480;
    private const int HeaderH = 70;
    private const int Pad     = 24;  // horizontal panel margins
    private const int PadTop  = 14;  // vertical gap between header and first content row
    private const int MidX    = 640;

    // Height of the MODEL section (left panel).
    // Used to align TOKEN USAGE (left) with RATE LIMITS (right) vertically.
    // = DrawLabel(24+6=30) + model text(36+18=54) + pre-section gap(8)
    private const int ModelSectionH = 92;

    private static readonly Theme T = Theme.ClaudeCode;

    public static byte[] Render(DisplayState state)
    {
        var info = new SKImageInfo(W, H, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        if (surface is null) return [];

        var canvas = surface.Canvas;
        canvas.Clear(T.BgColor);

        DrawHeader(canvas);
        DrawDividers(canvas);
        DrawLeftPanel(canvas, state);
        DrawRightPanel(canvas, state);

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        return encoded.ToArray();
    }

    // ──── Header ────────────────────────────────────────────────────────────────

    private static void DrawHeader(SKCanvas canvas)
    {
        using var bg = Fill(T.HeaderBg);
        canvas.DrawRect(0, 0, W, HeaderH, bg);

        using var titleP = TextPaint(T.AccentColor, 36, bold: true);
        canvas.DrawText("CLAUDE API MONITOR", Pad, 52, titleP);

        var ts = DateTime.Now.ToString("yyyy-MM-dd  HH:mm:ss");
        using var tsP = TextPaint(T.TextSecondary, 36);
        canvas.DrawText(ts, W - Pad - tsP.MeasureText(ts), 52, tsP);
    }

    private static void DrawDividers(SKCanvas canvas)
    {
        using var p = Stroke(T.BorderColor, 1);
        canvas.DrawLine(0, HeaderH, W, HeaderH, p);
        canvas.DrawLine(MidX, HeaderH, MidX, H, p);
    }

    // ──── Left panel ─────────────────────────────────────────────────────────────

    private static void DrawLeftPanel(SKCanvas canvas, DisplayState state)
    {
        float x      = Pad;
        float y      = HeaderH + PadTop;
        float rightX = MidX - Pad;
        float panelW = MidX - Pad * 2;

        // MODEL ─────────────────────────────────────────────────────────────────
        DrawLabel(canvas, "MODEL", x, ref y);

        using (var p = TextPaint(state.Model is null ? T.TextSecondary : T.AccentColor, 36))
            canvas.DrawText(state.Model ?? "—", x, y + 36, p);
        y += 36 + 18;

        // TOKEN USAGE ── aligned with RATE LIMITS on right panel (y = HeaderH + PadTop + ModelSectionH)
        y += 8;
        DrawLabel(canvas, "TOKEN USAGE", x, ref y);

        var u = state.Usage;
        DrawKV(canvas, "Input",  u is not null ? $"{u.InputTokens:N0}"  : "—", x, rightX, ref y);
        DrawKV(canvas, "Output", u is not null ? $"{u.OutputTokens:N0}" : "—", x, rightX, ref y);
        if (u is not null && u.CacheReadInputTokens > 0)
            DrawKV(canvas, "Cache read",    $"{u.CacheReadInputTokens:N0}",     x, rightX, ref y);
        if (u is not null && u.CacheCreationInputTokens > 0)
            DrawKV(canvas, "Cache created", $"{u.CacheCreationInputTokens:N0}", x, rightX, ref y);

        // CONTEXT WINDOW ────────────────────────────────────────────────────────
        var ctxSize = ClaudeProxyMiddleware.GetContextWindowSize(state.Model);

        y += 10;
        DrawLabel(canvas, "CONTEXT WINDOW", x, ref y);

        if (ctxSize > 0 && u is not null)
        {
            var total    = u.InputTokens + u.CacheReadInputTokens + u.CacheCreationInputTokens;
            var frac     = Math.Clamp((float)total / ctxSize, 0f, 1f);
            var barColor = frac >= 0.9f ? T.ColorError : frac >= 0.7f ? T.ColorWarn : T.ColorGood;
            DrawBar(canvas, x, y, panelW, 24, frac, barColor);
            y += 24 + 6;

            using var ctxP = TextPaint(T.TextSecondary, 28);
            canvas.DrawText($"{total:N0} / {ctxSize:N0}  ({frac * 100:F1}%)", x, y + 28, ctxP);
        }
        else
        {
            DrawBar(canvas, x, y, panelW, 24, 0f, T.ColorGood);
            y += 24 + 6;

            using var ctxP = TextPaint(T.TextSecondary, 28);
            var ctxLabel = ctxSize > 0 ? $"— / {ctxSize:N0}" : "—";
            canvas.DrawText(ctxLabel, x, y + 28, ctxP);
        }
    }

    // ──── Right panel ────────────────────────────────────────────────────────────

    private static void DrawRightPanel(SKCanvas canvas, DisplayState state)
    {
        float x      = MidX + Pad;
        // Start at the same y as TOKEN USAGE on the left panel
        float y      = HeaderH + PadTop + ModelSectionH;
        float panelW = W - MidX - Pad * 2;

        DrawLabel(canvas, "RATE LIMITS", x, ref y);
        y += 8;

        var rl = state.RateLimit;

        DrawRateRow(canvas, "5H",
            rl?.FiveHourUtilization ?? 0.0,
            rl?.FiveHourStatus,
            rl is not null ? rl.FiveHourReset.ToLocalTime().ToString("HH:mm:ss") : "—",
            x, ref y, panelW);
        y += 16;

        DrawRateRow(canvas, "7D",
            rl?.SevenDayUtilization ?? 0.0,
            rl?.SevenDayStatus,
            rl is not null ? rl.SevenDayReset.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : "—",
            x, ref y, panelW);
    }

    private static void DrawRateRow(
        SKCanvas canvas, string label, double utilization, string? status, string resetTime,
        float x, ref float y, float panelW)
    {
        const float labelW = 52f;
        const float barH   = 28f;
        float barX = x + labelW;
        float barW = panelW - labelW;
        var   color = StatusColor(status);

        // Axis label ("5H" / "7D")
        using var lp = TextPaint(T.TextSecondary, 28);
        canvas.DrawText(label, x, y + 24, lp);

        // Progress bar
        DrawBar(canvas, barX, y, barW, barH, (float)utilization, color);

        // Percentage text inside bar, right-aligned
        var pctTxt = $"{utilization * 100:F1}%";
        using var pctP = TextPaint(T.TextPrimary, 24);
        canvas.DrawText(pctTxt, barX + barW - pctP.MeasureText(pctTxt) - 6, y + barH - 5, pctP);

        y += barH + 5;

        // Status badge (only when not "allowed") + reset time
        if (status is not null && status != "allowed")
        {
            using var sp = TextPaint(color, 24);
            canvas.DrawText($"[{status}]", barX, y + 24, sp);
        }

        using var rp = TextPaint(T.TextSecondary, 24);
        canvas.DrawText(resetTime, barX + barW - rp.MeasureText(resetTime), y + 24, rp);

        y += 24 + 6;
    }

    // ──── Primitive helpers ──────────────────────────────────────────────────────

    private static void DrawLabel(SKCanvas canvas, string text, float x, ref float y)
    {
        using var p = TextPaint(T.TextSecondary, 24);
        canvas.DrawText(text, x, y + 24, p);
        y += 24 + 6;
    }

    private static void DrawKV(SKCanvas canvas, string key, string value, float leftX, float rightX, ref float y)
    {
        using var kp = TextPaint(T.TextSecondary, 28);
        canvas.DrawText(key, leftX, y + 28, kp);

        using var vp = TextPaint(T.TextPrimary, 28);
        canvas.DrawText(value, rightX - vp.MeasureText(value), y + 28, vp);

        y += 28 + 12;
    }

    private static void DrawBar(SKCanvas canvas, float x, float y, float w, float h, float fraction, SKColor fillColor)
    {
        const float r = 3f;

        using var bg = Fill(T.BarBgColor);
        canvas.DrawRoundRect(x, y, w, h, r, r, bg);

        var fw = Math.Max(0f, Math.Min(w, w * fraction));
        if (fw > r * 2)
        {
            using var fg = Fill(fillColor);
            canvas.DrawRoundRect(x, y, fw, h, r, r, fg);
        }
    }

    private static SKColor StatusColor(string? status) => status switch
    {
        "allowed"         => T.ColorGood,
        "allowed_warning" => T.ColorWarn,
        "rejected"        => T.ColorError,
        _                 => T.TextSecondary,
    };

    private static SKPaint Fill(SKColor color) =>
        new() { Color = color, IsAntialias = true };

    private static SKPaint Stroke(SKColor color, float width) =>
        new() { Color = color, StrokeWidth = width, Style = SKPaintStyle.Stroke };

    private static SKPaint TextPaint(SKColor color, float size, bool bold = false)
    {
        var weight = bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
        var tf = SKTypeface.FromFamilyName(T.FontFamily, weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
                 ?? SKTypeface.Default;
        return new SKPaint
        {
            Color       = color,
            TextSize    = size,
            Typeface    = tf,
            IsAntialias = true,
        };
    }
}

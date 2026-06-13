using SkiaSharp;

namespace WorkCar;

/// <summary>
/// コックピットHUD全体の描画。1920x1080 の仮想座標系にレイアウトし、
/// ウィンドウサイズに合わせて等比スケールする。
/// </summary>
public static partial class HudRenderer
{
    const float VW = 1920f, VH = 1080f;

    static readonly SKPaint Fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    static readonly SKPaint Stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    static SKShader? _bgGlow;
    static readonly Random Rng = new(3);
    static string _telemetry = "TLM:: -- -- -- --";
    static float _nextTelemetry;

    public static void Render(SKCanvas c, int w, int h, VehicleSimulator sim, float t, bool paused)
    {
        c.Clear(HudColors.Bg);
        float s = MathF.Min(w / VW, h / VH);
        c.Save();
        c.Translate((w - VW * s) / 2f, (h - VH * s) / 2f);
        c.Scale(s);
        c.ClipRect(new SKRect(0, 0, VW, VH));

        DrawBackground(c, t);
        DrawTopBar(c, sim, t);
        DrawShiftLights(c, sim, t);
        DrawTachometer(c, sim, t);
        DrawBoostPot(c, sim, t);
        DrawSpeedCluster(c, sim, t);
        DrawGearIndicator(c, sim, t);
        DrawSideDials(c, sim, t);
        DrawBottomGauges(c, sim, t);
        DrawWarnings(c, sim, t);
        DrawFooter(c, sim, t);
        if (paused) DrawPaused(c);
        DrawScanlines(c, t);

        c.Restore();
    }

    // ---------------- 背景 ----------------

    static void DrawBackground(SKCanvas c, float t)
    {
        // 中央のかすかな発光
        _bgGlow ??= SKShader.CreateRadialGradient(
            new SKPoint(VW / 2f, VH * 0.40f), VW * 0.62f,
            new[] { new SKColor(0x0B, 0x1A, 0x2E), HudColors.Bg },
            null, SKShaderTileMode.Clamp);
        Fill.Shader = _bgGlow;
        Fill.Color = SKColors.White;
        c.DrawRect(0, 0, VW, VH, Fill);
        Fill.Shader = null;

        // 下方に流れるパースペクティブグリッド
        const float horizon = 730f;
        c.Save();
        c.ClipRect(new SKRect(0, horizon, VW, VH));
        Stroke.StrokeWidth = 1.4f;
        Stroke.MaskFilter = null;
        Stroke.Color = HudColors.Azure.WithAlpha(26);
        for (int k = -14; k <= 14; k++)
            c.DrawLine(VW / 2f + k * 46f, horizon, VW / 2f + k * 330f, VH + 60f, Stroke);
        for (int i = 0; i < 10; i++)
        {
            float u = (i / 10f + t * 0.14f) % 1f;
            float y = horizon + (VH + 40f - horizon) * u * u;
            Stroke.Color = HudColors.Azure.WithAlpha((byte)(12 + 50 * u));
            c.DrawLine(0, y, VW, y, Stroke);
        }
        c.Restore();
    }

    static void DrawScanlines(SKCanvas c, float t)
    {
        Stroke.StrokeWidth = 1f;
        Stroke.Color = SKColors.Black.WithAlpha(26);
        Stroke.MaskFilter = null;
        for (float y = 0; y < VH; y += 4f)
            c.DrawLine(0, y, VW, y, Stroke);

        // ゆっくり走査する明帯
        float band = (t * 130f) % (VH + 240f) - 120f;
        Fill.Color = SKColors.White.WithAlpha(7);
        c.DrawRect(0, band, VW, 90f, Fill);
        Fill.Color = SKColors.White;
    }

    // ---------------- 上部情報バー ----------------

    static void DrawTopBar(SKCanvas c, VehicleSimulator sim, float t)
    {
        // タイトル
        HudParts.DrawGlowText(c, "TYPE-19 STRATOS", VW / 2f, 64f, 42f, HudColors.Cyan, glow: 9f);
        HudParts.DrawText(c, "FORMULA COCKPIT TELEMETRY SYSTEM", VW / 2f, 93f, 16f, HudColors.Dim);

        // タイトル両脇の装飾ライン
        HudParts.DrawLine(c, 470f, 56f, 690f, 56f, 2f, HudColors.Azure.WithAlpha(120));
        HudParts.DrawLine(c, 690f, 56f, 716f, 80f, 2f, HudColors.Azure.WithAlpha(120));
        HudParts.DrawLine(c, 1450f, 56f, 1230f, 56f, 2f, HudColors.Azure.WithAlpha(120));
        HudParts.DrawLine(c, 1230f, 56f, 1204f, 80f, 2f, HudColors.Azure.WithAlpha(120));

        bool blink = MathF.Sin(t * 4f) > 0f;
        HudParts.FillCircle(c, 1466f, 51f, 5f, blink ? HudColors.Green : HudColors.Green.WithAlpha(70), 5f);

        // 左:ラップ情報
        HudParts.DrawText(c, "LAP", 60f, 56f, 22f, HudColors.Dim, SKTextAlign.Left);
        HudParts.DrawGlowText(c, $"{sim.Lap:00} / {sim.TotalLaps:00}", 130f, 57f, 32f,
            HudColors.White, SKTextAlign.Left, HudParts.MonoFace, 4f);
        HudParts.DrawText(c, "TIME", 60f, 92f, 22f, HudColors.Dim, SKTextAlign.Left);
        HudParts.DrawGlowText(c, FormatLap(sim.LapTime), 130f, 93f, 30f,
            HudColors.Cyan, SKTextAlign.Left, HudParts.MonoFace, 4f);
        HudParts.DrawText(c, "BEST", 60f, 126f, 22f, HudColors.Dim, SKTextAlign.Left);
        HudParts.DrawGlowText(c, FormatLap(sim.BestLap), 130f, 127f, 30f,
            HudColors.Amber, SKTextAlign.Left, HudParts.MonoFace, 4f);

        // 右:ポジションなど
        HudParts.DrawGlowText(c, $"P{sim.Position:00}", 1860f, 57f, 32f,
            HudColors.White, SKTextAlign.Right, HudParts.MonoFace, 4f);
        HudParts.DrawText(c, "POS", 1670f, 56f, 22f, HudColors.Dim, SKTextAlign.Right);
        HudParts.DrawGlowText(c, $"{sim.SpeedMax,3:0} km/h", 1860f, 93f, 30f,
            HudColors.Cyan, SKTextAlign.Right, HudParts.MonoFace, 4f);
        HudParts.DrawText(c, "VMAX", 1670f, 92f, 22f, HudColors.Dim, SKTextAlign.Right);
        HudParts.DrawGlowText(c, "ONLINE", 1860f, 127f, 30f,
            HudColors.Green, SKTextAlign.Right, HudParts.MonoFace, 4f);
        HudParts.DrawText(c, "SYS", 1670f, 126f, 22f, HudColors.Dim, SKTextAlign.Right);
    }

    // ---------------- 警告・フッタ ----------------

    static void DrawWarnings(SKCanvas c, VehicleSimulator sim, float t)
    {
        var warns = new List<string>(4);
        if (sim.WaterTemp > 110f) warns.Add("WATER TEMP");
        if (sim.OilTemp > 145f) warns.Add("OIL TEMP");
        if (sim.Fuel < 0.12f) warns.Add("FUEL LOW");
        if (sim.BoostCharge < 0.10f && !sim.BoostActive) warns.Add("BOOST DEPLETED");
        if (warns.Count == 0) return;

        bool flash = MathF.Sin(t * 10f) > -0.3f;
        string text = "▲ " + string.Join("   ▲ ", warns);
        float w = MathF.Max(360f, 60f + text.Length * 14f);
        HudParts.DrawCutPanel(c, VW / 2f - w / 2f, 168f, w, 40f, 12f,
            new SKColor(0x2A, 0x06, 0x0A, 190),
            HudColors.Red.WithAlpha((byte)(flash ? 220 : 80)), 2f);
        HudParts.DrawGlowText(c, text, VW / 2f, 196f, 24f,
            flash ? HudColors.Red : HudColors.Red.WithAlpha(120),
            SKTextAlign.Center, HudParts.MonoFace, 6f);
    }

    static void DrawFooter(SKCanvas c, VehicleSimulator sim, float t)
    {
        if (t > _nextTelemetry)
        {
            _nextTelemetry = t + 0.28f;
            Span<byte> b = stackalloc byte[6];
            Rng.NextBytes(b);
            _telemetry = $"TLM 0x{Rng.Next(0x1000, 0xFFFF):X4} :: " +
                $"{b[0]:X2} {b[1]:X2} {b[2]:X2} {b[3]:X2} {b[4]:X2} {b[5]:X2}" +
                $"  SYNC:OK  Δ{Rng.NextDouble() * 0.01:F4}";
        }
        HudParts.DrawText(c, _telemetry, 40f, 1058f, 17f, HudColors.Dim.WithAlpha(170),
            SKTextAlign.Left, HudParts.MonoFace);
        HudParts.DrawText(c, "[SPACE] BOOST    [P] HOLD    [ESC] EXIT",
            VW / 2f, 1058f, 17f, HudColors.Dim);
        float link = 96.5f + 2.5f * MathF.Sin(t * 1.7f);
        HudParts.DrawText(c, $"LINK {link:F1}%", 1818f, 1058f, 17f,
            HudColors.Dim.WithAlpha(170), SKTextAlign.Right, HudParts.MonoFace);
        Fill.MaskFilter = null;
        for (int i = 0; i < 5; i++)
        {
            bool on = i < 4 || MathF.Sin(t * 6f) > 0f;
            Fill.Color = on ? HudColors.Green.WithAlpha(170) : HudColors.PanelLine;
            c.DrawRect(1830f + i * 11f, 1046f, 7f, 13f, Fill);
        }
    }

    static void DrawPaused(SKCanvas c)
    {
        Fill.Color = SKColors.Black.WithAlpha(120);
        c.DrawRect(0, 0, VW, VH, Fill);
        HudParts.DrawGlowText(c, "— TELEMETRY HOLD —", VW / 2f, VH / 2f, 54f, HudColors.Amber, glow: 12f);
    }

    static string FormatLap(float sec)
    {
        int m = (int)(sec / 60f);
        float r = sec - m * 60f;
        return $"{m}:{r:00.000}";
    }
}

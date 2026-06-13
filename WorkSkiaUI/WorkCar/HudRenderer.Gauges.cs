using SkiaSharp;

namespace WorkCar;

public static partial class HudRenderer
{
    static SKShader? _tachShader;

    // ---------------- シフトインジケータ(LED列) ----------------

    static void DrawShiftLights(SKCanvas c, VehicleSimulator sim, float t)
    {
        const int n = 15;
        const float y = 142f, spacing = 30f;
        float x0 = VW / 2f - (n - 1) * spacing / 2f;
        float lit = (sim.Rpm - 11000f) / (VehicleSimulator.ShiftRpm - 11000f) * n;
        bool over = sim.Rpm > 17600f;
        bool flash = MathF.Sin(t * 45f) > 0f;

        for (int i = 0; i < n; i++)
        {
            float x = x0 + i * spacing;
            SKColor col = i < 5 ? HudColors.Cyan : i < 10 ? HudColors.Amber : HudColors.Red;
            bool on = over ? flash : i < lit;
            if (on)
            {
                HudParts.FillCircle(c, x, y, 9f, col, 7f);
            }
            else
            {
                Fill.Color = HudColors.Panel;
                c.DrawCircle(x, y, 7f, Fill);
                Stroke.Color = HudColors.PanelLine;
                Stroke.StrokeWidth = 1.5f;
                Stroke.MaskFilter = null;
                c.DrawCircle(x, y, 7f, Stroke);
            }
        }
    }

    // ---------------- タコメーター(左) ----------------

    static void DrawTachometer(SKCanvas c, VehicleSimulator sim, float t)
    {
        const float cx = 430f, cy = 510f, R = 270f;
        const float start = 150f, sweep = 240f;
        float frac = Math.Clamp(sim.Rpm / VehicleSimulator.MaxRpm, 0f, 1f);
        float redFrac = VehicleSimulator.RedlineRpm / VehicleSimulator.MaxRpm;
        bool inRed = sim.Rpm >= VehicleSimulator.RedlineRpm;

        // 外周の装飾と赤域
        HudParts.DrawArc(c, cx, cy, R + 36f, start, sweep, 2f, HudColors.PanelLine);
        HudParts.DrawArc(c, cx, cy, R + 26f, start + sweep * redFrac,
            sweep * (1f - redFrac), 8f, HudColors.Red.WithAlpha(210), 6f);

        // トラックと値アーク(スイープグラデーション)
        HudParts.DrawArc(c, cx, cy, R, start, sweep, 26f, new SKColor(0x0E, 0x1C, 0x2C, 230));
        _tachShader ??= SKShader.CreateSweepGradient(new SKPoint(cx, cy),
            new[] { HudColors.Azure, HudColors.Cyan, HudColors.Amber, HudColors.Red, HudColors.Red },
            new[] { 0f, 0.36f, 0.52f, 0.596f, 0.667f });
        if (frac > 0.004f)
        {
            var rect = new SKRect(cx - R, cy - R, cx + R, cy + R);
            c.Save();
            c.RotateDegrees(start, cx, cy);
            Stroke.Shader = _tachShader;
            Stroke.StrokeWidth = 26f;
            Stroke.StrokeCap = SKStrokeCap.Butt;
            Stroke.Color = SKColors.White.WithAlpha(130);
            Stroke.MaskFilter = HudParts.Blur(10f);
            c.DrawArc(rect, 0f, sweep * frac, false, Stroke);
            Stroke.Color = SKColors.White;
            Stroke.MaskFilter = null;
            c.DrawArc(rect, 0f, sweep * frac, false, Stroke);
            Stroke.Shader = null;
            c.Restore();
        }

        // 目盛(500刻み、1000ごとに大目盛、2000ごとに数字)
        for (int rpm = 0; rpm <= 19000; rpm += 500)
        {
            float f = rpm / VehicleSimulator.MaxRpm;
            float ang = start + sweep * f;
            bool major = rpm % 1000 == 0;
            var col = rpm >= VehicleSimulator.RedlineRpm
                ? HudColors.Red.WithAlpha(220)
                : HudColors.Dim.WithAlpha(major ? (byte)230 : (byte)120);
            var p0 = HudParts.Polar(cx, cy, R + 16f, ang);
            var p1 = HudParts.Polar(cx, cy, R + (major ? 34f : 26f), ang);
            HudParts.DrawLine(c, p0.X, p0.Y, p1.X, p1.Y, major ? 3f : 1.6f, col,
                0f, SKStrokeCap.Butt);
            if (rpm % 2000 == 0)
            {
                var pt = HudParts.Polar(cx, cy, R - 44f, ang);
                HudParts.DrawText(c, (rpm / 1000).ToString(), pt.X, pt.Y + 9f, 27f,
                    rpm >= 18000 ? HudColors.Red : HudColors.Dim, SKTextAlign.Center, HudParts.MonoFace);
            }
        }
        HudParts.DrawText(c, "×1000 r/min", cx, cy - 52f, 18f, HudColors.Dim);

        // 針
        float needleAng = start + sweep * frac;
        c.Save();
        c.RotateDegrees(needleAng, cx, cy);
        using (var needle = new SKPath())
        {
            needle.MoveTo(cx + 64f, cy - 7f);
            needle.LineTo(cx + R - 10f, cy - 1.8f);
            needle.LineTo(cx + R - 10f, cy + 1.8f);
            needle.LineTo(cx + 64f, cy + 7f);
            needle.Close();
            Fill.Color = (inRed ? HudColors.Red : HudColors.Cyan).WithAlpha(160);
            Fill.MaskFilter = HudParts.Blur(8f);
            c.DrawPath(needle, Fill);
            Fill.Color = HudColors.White;
            Fill.MaskFilter = null;
            c.DrawPath(needle, Fill);
        }
        c.Restore();

        // ハブ
        Fill.Color = HudColors.Panel;
        c.DrawCircle(cx, cy, 30f, Fill);
        Stroke.Color = HudColors.PanelLine;
        Stroke.StrokeWidth = 2f;
        c.DrawCircle(cx, cy, 30f, Stroke);
        HudParts.FillCircle(c, cx, cy, 7f, inRed ? HudColors.Red : HudColors.Cyan, 6f);

        // デジタルRPM表示
        HudParts.DrawCutPanel(c, cx - 118f, cy + 116f, 236f, 96f, 14f,
            HudColors.Panel.WithAlpha(220), HudColors.PanelLine);
        HudParts.DrawText(c, "ENGINE RPM", cx, cy + 140f, 18f, HudColors.Dim);
        string rpmText = ((int)sim.Rpm).ToString().PadLeft(5, ' ');
        bool flash = !inRed || MathF.Sin(t * 18f) > -0.4f;
        HudParts.DrawSevenSeg(c, cx - HudParts.SevenSegWidth(42f, 5) / 2f, cy + 152f, 42f,
            rpmText, inRed ? HudColors.Red.WithAlpha((byte)(flash ? 255 : 110)) : HudColors.Cyan);
    }

    // ---------------- ブーストポット(右) ----------------

    static void DrawBoostPot(SKCanvas c, VehicleSimulator sim, float t)
    {
        const float cx = 1490f, cy = 510f;
        const float start = 150f, sweep = 240f;
        float charge = Math.Clamp(sim.BoostCharge, 0f, 1f);
        bool active = sim.BoostActive;
        bool flash = MathF.Sin(t * 14f) > 0f;
        float spin = (active ? t * 320f : t * 36f) % 360f;

        // 回転する装飾リング
        var spinCol = active ? HudColors.Red : HudColors.Azure;
        HudParts.DrawArc(c, cx, cy, 302f, spin, 70f, 2.5f, spinCol.WithAlpha(120));
        HudParts.DrawArc(c, cx, cy, 302f, spin + 180f, 70f, 2.5f, spinCol.WithAlpha(60));
        HudParts.DrawArc(c, cx, cy, 232f, 360f - spin * 0.6f % 360f, 110f, 2f,
            HudColors.Azure.WithAlpha(70));

        // チャージ残量セグメントリング
        const int segs = 26;
        SKColor baseCol = active
            ? (flash ? HudColors.Amber : HudColors.Red)
            : charge < 0.25f ? HudColors.Red
            : charge < 0.50f ? HudColors.Amber
            : HudColors.Cyan;
        for (int i = 0; i < segs; i++)
        {
            float a0 = start + sweep * i / segs + 1.5f;
            float aw = sweep / segs - 3f;
            bool on = (i + 0.5f) / segs <= charge;
            if (on)
                HudParts.DrawArc(c, cx, cy, 266f, a0, aw, 24f, baseCol, 7f);
            else
                HudParts.DrawArc(c, cx, cy, 266f, a0, aw, 24f, HudColors.PanelLine.WithAlpha(80));
        }

        // %目盛ラベル
        for (int p = 0; p <= 100; p += 25)
        {
            var pt = HudParts.Polar(cx, cy, 314f, start + sweep * p / 100f);
            HudParts.DrawText(c, p.ToString(), pt.X, pt.Y + 6f, 17f, HudColors.Dim,
                SKTextAlign.Center, HudParts.MonoFace);
        }

        // ブースト作動時のパルスリング
        if (active)
        {
            Stroke.MaskFilter = null;
            for (int i = 0; i < 3; i++)
            {
                float ph = (t * 1.6f + i / 3f) % 1f;
                Stroke.Color = HudColors.Red.WithAlpha((byte)((1f - ph) * 130f));
                Stroke.StrokeWidth = 3f - ph * 2f;
                c.DrawCircle(cx, cy, 152f + ph * 110f, Stroke);
            }
        }

        // 中央ディスク
        Fill.Color = HudColors.Panel.WithAlpha(208);
        c.DrawCircle(cx, cy, 150f, Fill);
        Stroke.Color = active ? HudColors.Red.WithAlpha(200) : HudColors.PanelLine;
        Stroke.StrokeWidth = 2.5f;
        c.DrawCircle(cx, cy, 150f, Stroke);

        HudParts.DrawText(c, "BOOST POT", cx, cy - 96f, 24f, HudColors.Dim);

        // チャージ%(7セグ)
        string pct = ((int)MathF.Round(charge * 100f)).ToString().PadLeft(3, ' ');
        var digitCol = active ? HudColors.Amber : charge < 0.25f ? HudColors.Red : HudColors.Cyan;
        HudParts.DrawSevenSeg(c, cx - 95f, cy - 64f, 74f, pct, digitCol);
        HudParts.DrawText(c, "%", cx + 80f, cy + 8f, 30f, HudColors.Dim,
            SKTextAlign.Left, HudParts.MonoFace);

        // 状態表示
        if (active)
            HudParts.DrawGlowText(c, "■ DISCHARGE", cx, cy + 56f, 24f,
                flash ? HudColors.Red : HudColors.Red.WithAlpha(130),
                SKTextAlign.Center, HudParts.MonoFace, 7f);
        else if (charge >= 0.985f)
            HudParts.DrawGlowText(c, "READY", cx, cy + 56f, 24f, HudColors.Green, glow: 6f);
        else
            HudParts.DrawText(c, "CHARGING" + new string('.', (int)(t * 2f) % 4), cx, cy + 56f,
                22f, HudColors.Azure);

        HudParts.DrawText(c, active ? "+65% THRUST" : $"CELLS {(int)(charge * segs)}/{segs}",
            cx, cy + 92f, 18f, active ? HudColors.Amber : HudColors.Dim);

        // 下側の開口部にヒント
        HudParts.DrawText(c, "[SPACE] MANUAL OVERRIDE", cx, cy + 228f, 16f,
            HudColors.Dim.WithAlpha(180));
    }

    // ---------------- 速度(中央メイン) ----------------

    static void DrawSpeedCluster(SKCanvas c, VehicleSimulator sim, float t)
    {
        const float cx = VW / 2f;
        HudParts.DrawText(c, "SPEED", cx, 252f, 22f, HudColors.Dim);

        string txt = ((int)MathF.Round(sim.Speed)).ToString().PadLeft(3, ' ');
        const float dh = 168f;
        float dw = HudParts.SevenSegWidth(dh, 3);
        var col = sim.BoostActive ? HudColors.Amber : HudColors.Cyan;
        HudParts.DrawSevenSeg(c, cx - dw / 2f, 270f, dh, txt, col);
        HudParts.DrawText(c, "km/h", cx, 482f, 30f, HudColors.Azure);

        // 速度リボンバー
        const int cells = 34;
        const float cw = 10f, gap = 3.2f;
        float total = cells * (cw + gap) - gap;
        float x0 = cx - total / 2f, y = 504f;
        float frac = sim.Speed / VehicleSimulator.TopSpeed;
        int litCount = (int)(frac * cells + 0.5f);
        Fill.MaskFilter = null;
        for (int i = 0; i < cells; i++)
        {
            bool on = i < litCount;
            SKColor cc = i < cells * 0.6f ? HudColors.Cyan
                       : i < cells * 0.85f ? HudColors.Amber : HudColors.Red;
            Fill.Color = on ? cc : HudColors.PanelLine.WithAlpha(80);
            c.DrawRect(x0 + i * (cw + gap), y, cw, 15f, Fill);
        }
        // 最高速マーカー
        float mx = x0 + total * Math.Clamp(sim.SpeedMax / VehicleSimulator.TopSpeed, 0f, 1f);
        using (var tri = new SKPath())
        {
            tri.MoveTo(mx - 6f, y - 10f);
            tri.LineTo(mx + 6f, y - 10f);
            tri.LineTo(mx, y - 2f);
            tri.Close();
            Fill.Color = HudColors.Amber;
            c.DrawPath(tri, Fill);
        }
        HudParts.DrawText(c, "0", x0, y + 32f, 16f, HudColors.Dim);
        HudParts.DrawText(c, "680", x0 + total, y + 32f, 16f, HudColors.Dim);
    }

    // ---------------- シフト(ギア)表示 ----------------

    static void DrawGearIndicator(SKCanvas c, VehicleSimulator sim, float t)
    {
        const float cx = VW / 2f, cy = 646f;
        HudParts.DrawCutPanel(c, cx - 76f, cy - 78f, 152f, 156f, 26f,
            HudColors.Panel.WithAlpha(225), HudColors.PanelLine, 2f);

        var col = sim.Rpm > VehicleSimulator.RedlineRpm ? HudColors.Red
                : sim.Rpm > 15200f ? HudColors.Amber : HudColors.White;
        if (sim.Rpm > 17600f && MathF.Sin(t * 30f) < 0f) col = col.WithAlpha(120);
        HudParts.DrawGlowText(c, sim.Gear.ToString(), cx, cy + 44f, 122f, col,
            SKTextAlign.Center, HudParts.MonoFace, 14f);
        HudParts.DrawText(c, "GEAR", cx, cy + 100f, 20f, HudColors.Dim);

        // 両脇のギア段インジケータ
        for (int g = 1; g <= VehicleSimulator.TopGear; g++)
        {
            float yy = cy + 64f - (g - 1) * 18f;
            bool cur = g == sim.Gear;
            Fill.Color = cur ? HudColors.Cyan : HudColors.PanelLine.WithAlpha(120);
            Fill.MaskFilter = cur ? HudParts.Blur(4f) : null;
            c.DrawRect(cx - 94f, yy, 8f, 10f, Fill);
            c.DrawRect(cx + 86f, yy, 8f, 10f, Fill);
            Fill.MaskFilter = null;
        }
    }

    // ---------------- ERSダイヤル / Gメーター ----------------

    static void DrawSideDials(SKCanvas c, VehicleSimulator sim, float t)
    {
        // --- ERS 出力 ---
        {
            const float cx = 775f, cy = 648f, r = 58f;
            float frac = Math.Clamp((sim.ErsOutput + 100f) / 400f, 0f, 1f);
            var col = sim.ErsOutput < -5f ? HudColors.Green
                    : sim.BoostActive ? HudColors.Amber : HudColors.Cyan;
            HudParts.DrawArc(c, cx, cy, r, 135f, 270f, 8f, HudColors.PanelLine.WithAlpha(140));
            if (frac > 0.01f)
                HudParts.DrawArc(c, cx, cy, r, 135f, 270f * frac, 8f, col, 6f);
            var p = HudParts.Polar(cx, cy, r, 135f + 270f * frac);
            HudParts.FillCircle(c, p.X, p.Y, 4f, HudColors.White, 4f);
            HudParts.DrawText(c, "ERS", cx, cy - 18f, 16f, HudColors.Dim);
            HudParts.DrawGlowText(c, $"{sim.ErsOutput:+0;-0;0}", cx, cy + 10f, 28f,
                HudColors.White, SKTextAlign.Center, HudParts.MonoFace, 4f);
            HudParts.DrawText(c, "kW", cx, cy + 32f, 15f, HudColors.Dim);
            HudParts.DrawText(c, "ERS OUTPUT", cx, cy + r + 30f, 17f, HudColors.Dim);
        }

        // --- Gメーター ---
        {
            const float cx = 1140f, cy = 648f, r = 58f;
            Stroke.Color = HudColors.PanelLine;
            Stroke.StrokeWidth = 2f;
            Stroke.MaskFilter = null;
            c.DrawCircle(cx, cy, r, Stroke);
            Stroke.Color = HudColors.PanelLine.WithAlpha(120);
            Stroke.StrokeWidth = 1.2f;
            c.DrawCircle(cx, cy, r / 2f, Stroke);
            HudParts.DrawLine(c, cx - r, cy, cx + r, cy, 1f, HudColors.PanelLine.WithAlpha(140));
            HudParts.DrawLine(c, cx, cy - r, cx, cy + r, 1f, HudColors.PanelLine.WithAlpha(140));

            float bx = Math.Clamp(sim.GLat / 3.5f, -1f, 1f) * (r - 12f);
            float by = Math.Clamp(sim.GLong / 3.5f, -1f, 1f) * (r - 12f);
            float total = MathF.Sqrt(sim.GLat * sim.GLat + sim.GLong * sim.GLong);
            var col = total > 3.2f ? HudColors.Red : total > 2.2f ? HudColors.Amber : HudColors.Cyan;
            HudParts.DrawLine(c, cx, cy, cx + bx, cy + by, 1.5f, col.WithAlpha(110));
            HudParts.FillCircle(c, cx + bx, cy + by, 6.5f, col, 7f);
            HudParts.DrawText(c, $"G-FORCE  {total:0.0} G", cx, cy + r + 30f, 17f, HudColors.Dim);
        }
    }

    // ---------------- 下段の小型計器 ----------------

    static void DrawBottomGauges(SKCanvas c, VehicleSimulator sim, float t)
    {
        const float y = 935f;
        HudParts.DrawMiniGauge(c, 335f, y, "WATER TEMP", $"{sim.WaterTemp:0}", "°C",
            (sim.WaterTemp - 50f) / 80f, sim.WaterTemp > 110f, HudColors.Azure, t);
        HudParts.DrawMiniGauge(c, 585f, y, "OIL TEMP", $"{sim.OilTemp:0}", "°C",
            (sim.OilTemp - 60f) / 100f, sim.OilTemp > 145f, HudColors.Amber, t);
        HudParts.DrawMiniGauge(c, 835f, y, "OIL PRESS", $"{sim.OilPressure:0.0}", "bar",
            sim.OilPressure / 10f, sim.OilPressure < 1.5f && sim.Rpm > 3000f, HudColors.Cyan, t);
        HudParts.DrawMiniGauge(c, 1085f, y, "FUEL", $"{sim.Fuel * 100f:0}", "%",
            sim.Fuel, sim.Fuel < 0.12f, HudColors.Green, t);
        HudParts.DrawMiniGauge(c, 1335f, y, "BATTERY", $"{sim.Battery:0.0}", "V",
            (sim.Battery - 10f) / 6f, sim.Battery < 12.2f, HudColors.Cyan, t);
        HudParts.DrawMiniGauge(c, 1585f, y, "TURBO", $"{sim.TurboPressure:0.00}", "bar",
            sim.TurboPressure / 3f, sim.TurboPressure > 2.6f, HudColors.Amber, t);
    }
}

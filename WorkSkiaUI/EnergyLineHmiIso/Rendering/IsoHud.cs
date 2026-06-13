using EnergyLineHmiIso.Simulation;
using SkiaSharp;

namespace EnergyLineHmiIso.Rendering;

/// <summary>近未来風 HUD（ヘッダ / KPI / デマンドリング / 計器 / トレンド / イベント）。</summary>
public sealed partial class IsoRenderer
{
    void DrawHeader(SKCanvas c, PlantSimulation sim, double t)
    {
        using var g = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(0, 70),
            new[] { new SKColor(0x0A, 0x14, 0x22, 0xF2), new SKColor(0x06, 0x0B, 0x14, 0xE6) },
            null, SKShaderTileMode.Clamp);
        _fill.Shader = g;
        _fill.Color = SKColors.White;
        c.DrawRect(new SKRect(0, 0, VW, 70), _fill);
        _fill.Shader = null;

        c.DrawRect(new SKRect(0, 0, 5, 70), Fill(IsoTheme.Cyan));
        Text(c, "PLANT ENERGY GRID", 28, 31, 21, IsoTheme.TextMain, bold: true);
        Text(c, "第1工場 エネルギーライン監視 ─ QUARTER VIEW", 28, 53, 12, IsoTheme.TextDim);

        // 下端の走査ライン
        c.DrawRect(new SKRect(0, 68, VW, 69.5f), Fill(IsoTheme.Cyan.WithAlpha(55)));
        float sx = (float)((t * 260) % (VW + 420)) - 210;
        using var scan = SKShader.CreateLinearGradient(
            new SKPoint(sx - 130, 0), new SKPoint(sx + 130, 0),
            new[] { IsoTheme.Cyan.WithAlpha(0), IsoTheme.Cyan, IsoTheme.Cyan.WithAlpha(0) },
            null, SKShaderTileMode.Clamp);
        _fill.Shader = scan;
        _fill.Color = SKColors.White;
        c.DrawRect(new SKRect(sx - 130, 66.5f, sx + 130, 70), _fill);
        _fill.Shader = null;

        Text(c, DateTime.Now.ToString("HH:mm:ss"), 1576, 34, 22, IsoTheme.TextMain, SKTextAlign.Right, bold: true);
        Text(c, DateTime.Now.ToString("yyyy/MM/dd (ddd)"), 1576, 54, 11.5f, IsoTheme.TextDim, SKTextAlign.Right);

        // アラームチップ
        int n = sim.ActiveAlarmCount;
        var chipR = new SKRect(1304, 20, 1448, 48);
        using var chip = CutCorner(chipR, 7);
        bool blink = (t % 0.9) < 0.5;
        if (n > 0)
        {
            c.DrawPath(chip, Fill(IsoTheme.Alarm.WithAlpha(blink ? (byte)60 : (byte)25)));
            c.DrawPath(chip, Stroke(IsoTheme.Alarm.WithAlpha(blink ? (byte)255 : (byte)120), 1.4f));
            Text(c, $"アラーム {n} 件", chipR.MidX, 39, 13.5f,
                blink ? IsoTheme.Alarm : IsoTheme.Alarm.WithAlpha(150), SKTextAlign.Center, bold: true);
        }
        else
        {
            c.DrawPath(chip, Stroke(IsoTheme.Green.WithAlpha(140), 1.2f));
            Text(c, "システム正常", chipR.MidX, 39, 13, IsoTheme.Green, SKTextAlign.Center, bold: true);
        }
    }

    void DrawHud(SKCanvas c, PlantSimulation sim, double t)
    {
        var pr = new SKRect(1188, 84, 1584, 876);
        using var panel = CutCorner(pr, 14);
        c.DrawPath(panel, Fill(IsoTheme.PanelBg));
        c.DrawPath(panel, Stroke(IsoTheme.PanelEdge, 1.2f));

        // ---- OVERVIEW ----
        Section(c, 1204, 108, "OVERVIEW", "エネルギー概況");
        Kpi(c, new SKRect(1204, 120, 1382, 196), "受電電力", sim.GridKw, "N0", "kW", IsoTheme.Electric);
        Kpi(c, new SKRect(1390, 120, 1568, 196), "構内発電", sim.OnSiteGenKw, "N0", "kW", IsoTheme.Green);
        Kpi(c, new SKRect(1204, 204, 1382, 280), "ガス使用量", sim.GasTotal, "N0", "m³/h", IsoTheme.Gas);
        Kpi(c, new SKRect(1390, 204, 1568, 280), "蒸気供給", sim.SteamSupply, "F1", "t/h", IsoTheme.Steam);

        // ---- DEMAND ----
        Section(c, 1204, 312, "DEMAND", "受電デマンド");
        DrawDemandRing(c, new SKPoint(1288, 414), 70, sim);
        double ratio = sim.GridKw / PlantSimulation.ContractKw;
        InfoRow(c, 376, "契約電力", $"{PlantSimulation.ContractKw:N0} kW", IsoTheme.TextMain);
        InfoRow(c, 408, "使用率", $"{ratio * 100:F1} %",
            ratio > 0.95 ? IsoTheme.Alarm : ratio > 0.8 ? IsoTheme.Warn : IsoTheme.Green);
        InfoRow(c, 440, "余裕電力", $"{Math.Max(0, PlantSimulation.ContractKw - sim.GridKw):N0} kW", IsoTheme.TextMain);
        InfoRow(c, 472, "総負荷", $"{sim.TotalLoadKw:N0} kW", IsoTheme.TextMain);

        // ---- 計器 ----
        Section(c, 1204, 528, "INSTRUMENTS", "計器");
        DrawMicroGauge(c, 1258, 596, 30, sim.VoltageKv, 6.0, 7.2, "F2", "電圧 kV", 6.4, 6.8);
        DrawMicroGauge(c, 1386, 596, 30, sim.FrequencyHz, 49.5, 50.5, "F1", "周波数 Hz", 49.9, 50.1);
        DrawMicroGauge(c, 1514, 596, 30, sim.SteamPressure, 0.0, 1.0, "F2", "蒸気圧 MPa", 0.66, 0.92);

        // ---- トレンド ----
        Section(c, 1204, 672, "TREND", "総消費電力（直近 90 秒）");
        DrawTrend(c, new SKRect(1204, 684, 1568, 762), sim);

        // ---- イベント ----
        Section(c, 1204, 794, "EVENT LOG", "アラーム / イベント");
        DrawEvents(c, new SKRect(1204, 802, 1568, 872), sim, t);
    }

    void Section(SKCanvas c, float x, float y, string en, string jp)
    {
        c.DrawRect(new SKRect(x, y - 11, x + 3, y + 1), Fill(IsoTheme.Cyan));
        Text(c, en, x + 10, y, 11.5f, IsoTheme.Cyan, bold: true);
        _text.TextSize = 11.5f;
        _text.Typeface = IsoTheme.JpBold;
        float w = _text.MeasureText(en);
        Text(c, jp, x + 18 + w, y, 11, IsoTheme.TextDim);
        c.DrawLine(x, y + 7, 1568, y + 7, Stroke(IsoTheme.PanelEdge.WithAlpha(160), 1));
    }

    void Kpi(SKCanvas c, SKRect r, string label, double value, string fmt, string unit, SKColor color)
    {
        using var p = CutCorner(r, 8);
        c.DrawPath(p, Fill(IsoTheme.CardBg));
        c.DrawPath(p, Stroke(IsoTheme.PanelEdge, 1));
        c.DrawRect(new SKRect(r.Left, r.Top + 8, r.Left + 3, r.Bottom - 8), Fill(color.WithAlpha(220)));

        Text(c, label, r.Left + 14, r.Top + 22, 12, IsoTheme.TextDim);
        Text(c, value.ToString(fmt), r.Right - 50, r.Bottom - 14, 26, color, SKTextAlign.Right, bold: true);
        Text(c, unit, r.Right - 12, r.Bottom - 14, 11.5f, IsoTheme.TextDim, SKTextAlign.Right);
    }

    void InfoRow(SKCanvas c, float y, string label, string value, SKColor valueColor)
    {
        Text(c, label, 1382, y, 12.5f, IsoTheme.TextDim);
        Text(c, value, 1568, y, 14.5f, valueColor, SKTextAlign.Right, bold: true);
    }

    void DrawDemandRing(SKCanvas c, SKPoint ctr, float r, PlantSimulation sim)
    {
        double ratio = Math.Clamp(sim.GridKw / PlantSimulation.ContractKw, 0, 1);
        var col = ratio > 0.95 ? IsoTheme.Alarm : ratio > 0.8 ? IsoTheme.Warn : IsoTheme.Green;
        var rect = new SKRect(ctr.X - r, ctr.Y - r, ctr.X + r, ctr.Y + r);

        c.DrawCircle(ctr, r, Stroke(new SKColor(0x14, 0x22, 0x34), 13));
        if (ratio > 0.01)
        {
            var gp = Stroke(col.WithAlpha(120), 13, SKStrokeCap.Round);
            gp.MaskFilter = IsoTheme.GlowM;
            c.DrawArc(rect, -90, (float)(360 * ratio), false, gp);
            gp.MaskFilter = null;
            c.DrawArc(rect, -90, (float)(360 * ratio), false, Stroke(col, 6.5f, SKStrokeCap.Round));
        }

        foreach (var (zr, zc) in new[] { (0.80f, IsoTheme.Warn), (0.95f, IsoTheme.Alarm) })
        {
            float a = (-90 + 360 * zr) * MathF.PI / 180f;
            c.DrawLine(
                ctr.X + MathF.Cos(a) * (r - 11), ctr.Y + MathF.Sin(a) * (r - 11),
                ctr.X + MathF.Cos(a) * (r + 11), ctr.Y + MathF.Sin(a) * (r + 11),
                Stroke(zc, 2.2f));
        }

        Text(c, $"{ratio * 100:F0}%", ctr.X, ctr.Y + 4, 24, col, SKTextAlign.Center, bold: true);
        Text(c, $"{sim.GridKw:N0} kW", ctr.X, ctr.Y + 26, 12, IsoTheme.TextDim, SKTextAlign.Center);
    }

    void DrawMicroGauge(SKCanvas c, float cx, float cy, float r,
        double v, double min, double max, string fmt, string label, double okLo, double okHi)
    {
        const float start = 135f, sweep = 270f;
        var rect = new SKRect(cx - r, cy - r, cx + r, cy + r);

        c.DrawArc(rect, start, sweep, false, Stroke(new SKColor(0x14, 0x22, 0x34), 5, SKStrokeCap.Round));

        double frac = Math.Clamp((v - min) / (max - min), 0, 1);
        bool ok = v >= okLo && v <= okHi;
        var col = ok ? IsoTheme.Cyan : IsoTheme.Warn;

        var gp = Stroke(col.WithAlpha(110), 5, SKStrokeCap.Round);
        gp.MaskFilter = IsoTheme.GlowS;
        c.DrawArc(rect, start, (float)(sweep * frac), false, gp);
        gp.MaskFilter = null;
        c.DrawArc(rect, start, (float)(sweep * frac), false, Stroke(col, 3, SKStrokeCap.Round));

        Text(c, v.ToString(fmt), cx, cy + 5, 13, IsoTheme.TextMain, SKTextAlign.Center, bold: true);
        Text(c, label, cx, cy + r + 18, 10.5f, IsoTheme.TextDim, SKTextAlign.Center);
    }

    void DrawTrend(SKCanvas c, SKRect r, PlantSimulation sim)
    {
        using var bg = CutCorner(r, 6);
        c.DrawPath(bg, Fill(new SKColor(0x08, 0x0F, 0x18, 0xE6)));
        c.DrawPath(bg, Stroke(IsoTheme.PanelEdge, 1));

        const float max = 2600f;
        for (int i = 1; i <= 3; i++)
        {
            float gy = r.Bottom - r.Height * i / 4f;
            c.DrawLine(r.Left + 4, gy, r.Right - 4, gy, Stroke(IsoTheme.PanelEdge.WithAlpha(100), 1));
        }

        var vals = sim.Trend.ToArray();
        if (vals.Length >= 2)
        {
            using var line = new SKPath();
            for (int i = 0; i < vals.Length; i++)
            {
                float x = r.Left + r.Width * i / (vals.Length - 1);
                float y = r.Bottom - 4 - Math.Clamp(vals[i] / max, 0f, 1f) * (r.Height - 10);
                if (i == 0) line.MoveTo(x, y); else line.LineTo(x, y);
            }

            using var area = new SKPath(line);
            area.LineTo(r.Right, r.Bottom - 1);
            area.LineTo(r.Left, r.Bottom - 1);
            area.Close();
            using var grad = SKShader.CreateLinearGradient(
                new SKPoint(0, r.Top), new SKPoint(0, r.Bottom),
                new[] { IsoTheme.Electric.WithAlpha(70), IsoTheme.Electric.WithAlpha(0) },
                null, SKShaderTileMode.Clamp);
            _fill.Shader = grad;
            _fill.Color = SKColors.White;
            c.DrawPath(area, _fill);
            _fill.Shader = null;

            var gp = Stroke(IsoTheme.Electric.WithAlpha(120), 3.5f, SKStrokeCap.Round);
            gp.MaskFilter = IsoTheme.GlowS;
            c.DrawPath(line, gp);
            gp.MaskFilter = null;
            c.DrawPath(line, Stroke(IsoTheme.Electric, 1.8f, SKStrokeCap.Round));
        }

        Text(c, $"{sim.TotalLoadKw:N0} kW", r.Right - 10, r.Top + 18, 13.5f,
            IsoTheme.Electric, SKTextAlign.Right, bold: true);
    }

    void DrawEvents(SKCanvas c, SKRect r, PlantSimulation sim, double t)
    {
        bool blink = (t % 0.9) < 0.5;
        float y = r.Top + 16;
        foreach (var ev in sim.Events.Take(4))
        {
            var col = ev.Severity switch
            {
                Severity.Alarm => IsoTheme.Alarm,
                Severity.Warn => IsoTheme.Warn,
                _ => IsoTheme.TextDim,
            };
            bool hot = ev.Active && ev.Severity != Severity.Info;
            c.DrawRect(new SKRect(r.Left, y - 10, r.Left + 3, y + 3),
                Fill(hot && !blink ? col.WithAlpha(80) : col));
            Text(c, ev.Time.ToString("HH:mm:ss"), r.Left + 12, y, 11, IsoTheme.TextDim);
            Text(c, ev.Message, r.Left + 74, y, 11.5f, hot ? col : col.WithAlpha(200), bold: hot);
            y += 19;
        }
    }

    void DrawLegend(SKCanvas c)
    {
        (string Name, SKColor Col)[] items =
        {
            ("電力", IsoTheme.Electric), ("都市ガス", IsoTheme.Gas), ("蒸気", IsoTheme.Steam),
            ("給水・復水", IsoTheme.Water), ("冷却水", IsoTheme.Cool),
        };
        float x = 40;
        foreach (var (name, col) in items)
        {
            var g = Stroke(col.WithAlpha(150), 4, SKStrokeCap.Round);
            g.MaskFilter = IsoTheme.GlowS;
            c.DrawLine(x, 858, x + 24, 858, g);
            g.MaskFilter = null;
            c.DrawLine(x, 858, x + 24, 858, Stroke(col, 2.5f, SKStrokeCap.Round));
            Text(c, name, x + 30, 862, 12.5f, IsoTheme.TextDim);
            x += 30 + name.Length * 12.5f + 26;
        }
        Text(c, "建物をクリックすると起動 / 停止を切替できます", x + 14, 862, 12, IsoTheme.TextDim.WithAlpha(180));
    }
}

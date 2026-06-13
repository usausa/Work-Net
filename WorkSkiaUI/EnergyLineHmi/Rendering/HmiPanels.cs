using EnergyLineHmi.Simulation;
using SkiaSharp;

namespace EnergyLineHmi.Rendering;

/// <summary>ヘッダバーと右側サイドパネル（KPI / デマンドゲージ / トレンド / アラーム）。</summary>
public sealed partial class HmiRenderer
{
    void DrawHeader(SKCanvas c, PlantSimulation sim, double t)
    {
        c.DrawRect(new SKRect(0, 0, VW, 64), Fill(Theme.HeaderBg));
        c.DrawRect(new SKRect(0, 62, VW, 64), Fill(Theme.Accent.WithAlpha(120)));
        c.DrawRect(new SKRect(0, 0, 6, 64), Fill(Theme.Accent));

        Text(c, "第1工場 エネルギーライン監視", 26, 41, 25, Theme.TextMain, bold: true);
        Text(c, "PLANT ENERGY MANAGEMENT - DEMO", 420, 41, 12.5f, Theme.TextDim);

        Text(c, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), 1576, 30, 17, Theme.TextMain, SKTextAlign.Right, bold: true);

        int n = sim.ActiveAlarmCount;
        bool blink = (t % 0.9) < 0.5;
        if (n > 0)
            Text(c, $"● アラーム {n} 件", 1576, 52, 13.5f,
                blink ? Theme.StateAlarm : Theme.StateAlarm.WithAlpha(90), SKTextAlign.Right, bold: true);
        else
            Text(c, "● 正常監視中", 1576, 52, 13.5f, Theme.StateRun, SKTextAlign.Right);
    }

    void DrawSidePanel(SKCanvas c, PlantSimulation sim, double t)
    {
        var panel = new SKRect(1180, 84, 1580, 880);
        c.DrawRoundRect(panel, 10, 10, Fill(Theme.PanelBg));
        c.DrawRoundRect(panel, 10, 10, Stroke(Theme.PanelBorder, 1.2f));

        // ---- KPI カード ----
        KpiCard(c, new SKRect(1196, 100, 1376, 190), "受電電力", sim.GridKw.ToString("N0"), "kW", Theme.Electric);
        KpiCard(c, new SKRect(1384, 100, 1564, 190), "構内発電", sim.OnSiteGenKw.ToString("N0"), "kW", Theme.GenGreen);
        KpiCard(c, new SKRect(1196, 198, 1376, 288), "ガス使用量", sim.GasTotal.ToString("N0"), "m³/h", Theme.Gas);
        KpiCard(c, new SKRect(1384, 198, 1564, 288), "蒸気供給", sim.SteamSupply.ToString("F1"), "t/h", Theme.Steam);

        // ---- 受電デマンドゲージ ----
        Text(c, $"受電デマンド（契約 {PlantSimulation.ContractKw:N0} kW）", 1196, 322, 15, Theme.TextDim, bold: true);
        DrawGauge(c, new SKPoint(1296, 448), 88, sim);

        double ratio = sim.GridKw / PlantSimulation.ContractKw;
        InfoRow(c, 404, "契約電力", $"{PlantSimulation.ContractKw:N0} kW", Theme.TextMain);
        InfoRow(c, 442, "使用率", $"{ratio * 100:F1} %",
            ratio > 0.95 ? Theme.StateAlarm : ratio > 0.8 ? Theme.Warn : Theme.StateRun);
        InfoRow(c, 480, "余裕電力", $"{Math.Max(0, PlantSimulation.ContractKw - sim.GridKw):N0} kW", Theme.TextMain);

        // ---- トレンド ----
        Text(c, "総消費電力トレンド（直近 90 秒）", 1196, 572, 15, Theme.TextDim, bold: true);
        DrawTrend(c, new SKRect(1196, 584, 1564, 690), sim);

        // ---- アラーム / イベント ----
        Text(c, "アラーム / イベント履歴", 1196, 716, 15, Theme.TextDim, bold: true);
        DrawEvents(c, new SKRect(1196, 726, 1564, 866), sim, t);
    }

    void KpiCard(SKCanvas c, SKRect r, string title, string value, string unit, SKColor color)
    {
        c.DrawRoundRect(r, 8, 8, Fill(Theme.CardBg));
        c.DrawRoundRect(r, 8, 8, Stroke(Theme.PanelBorder, 1));
        c.DrawRoundRect(new SKRect(r.Left, r.Top, r.Left + 4, r.Bottom), 2, 2, Fill(color));

        Text(c, title, r.Left + 14, r.Top + 26, 13.5f, Theme.TextDim);
        Text(c, value, r.Right - 52, r.Bottom - 16, 30, color, SKTextAlign.Right, bold: true);
        Text(c, unit, r.Right - 12, r.Bottom - 16, 13, Theme.TextDim, SKTextAlign.Right);
    }

    void InfoRow(SKCanvas c, float y, string label, string value, SKColor valueColor)
    {
        Text(c, label, 1408, y, 13.5f, Theme.TextDim);
        Text(c, value, 1556, y, 15, valueColor, SKTextAlign.Right, bold: true);
    }

    void DrawGauge(SKCanvas c, SKPoint ctr, float radius, PlantSimulation sim)
    {
        var rect = new SKRect(ctr.X - radius, ctr.Y - radius, ctr.X + radius, ctr.Y + radius);
        const float start = 135, sweep = 270;

        c.DrawArc(rect, start, sweep, false, Stroke(new SKColor(0x1E, 0x29, 0x38), 16, SKStrokeCap.Round));

        double ratio = Math.Clamp(sim.GridKw / PlantSimulation.ContractKw, 0, 1);
        var col = ratio > 0.95 ? Theme.StateAlarm : ratio > 0.8 ? Theme.Warn : Theme.StateRun;
        if (ratio > 0.01)
            c.DrawArc(rect, start, (float)(sweep * ratio), false, Stroke(col, 16, SKStrokeCap.Round));

        // 80% / 95% しきい値マーク
        foreach (var (zr, zc) in new[] { (0.80f, Theme.Warn), (0.95f, Theme.StateAlarm) })
        {
            float a = (start + sweep * zr) * MathF.PI / 180f;
            c.DrawLine(
                ctr.X + MathF.Cos(a) * (radius - 13), ctr.Y + MathF.Sin(a) * (radius - 13),
                ctr.X + MathF.Cos(a) * (radius + 12), ctr.Y + MathF.Sin(a) * (radius + 12),
                Stroke(zc, 2.5f));
        }

        Text(c, sim.GridKw.ToString("N0"), ctr.X, ctr.Y + 4, 34, Theme.TextMain, SKTextAlign.Center, bold: true);
        Text(c, "kW", ctr.X, ctr.Y + 26, 14, Theme.TextDim, SKTextAlign.Center);
        Text(c, $"{ratio * 100:F0}%", ctr.X, ctr.Y + radius - 6, 18, col, SKTextAlign.Center, bold: true);
    }

    void DrawTrend(SKCanvas c, SKRect r, PlantSimulation sim)
    {
        c.DrawRoundRect(r, 6, 6, Fill(new SKColor(0x0D, 0x13, 0x1C)));
        c.DrawRoundRect(r, 6, 6, Stroke(Theme.PanelBorder, 1));

        const float max = 2600f;
        for (int i = 1; i <= 3; i++)
        {
            float gy = r.Bottom - r.Height * i / 4f;
            c.DrawLine(r.Left + 4, gy, r.Right - 4, gy, Stroke(Theme.PanelBorder.WithAlpha(110), 1));
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
            area.LineTo(r.Right, r.Bottom - 2);
            area.LineTo(r.Left, r.Bottom - 2);
            area.Close();
            c.DrawPath(area, Fill(Theme.Electric.WithAlpha(34)));
            c.DrawPath(line, Stroke(Theme.Electric, 2, SKStrokeCap.Round));
        }

        Text(c, $"{sim.TotalLoadKw:N0} kW", r.Right - 10, r.Top + 20, 14.5f, Theme.Electric, SKTextAlign.Right, bold: true);
        Text(c, "0", r.Left + 6, r.Bottom - 6, 10.5f, Theme.TextDim.WithAlpha(150));
        Text(c, $"{max:N0}", r.Left + 6, r.Top + 14, 10.5f, Theme.TextDim.WithAlpha(150));
    }

    void DrawEvents(SKCanvas c, SKRect r, PlantSimulation sim, double t)
    {
        c.DrawRoundRect(r, 6, 6, Fill(new SKColor(0x0D, 0x13, 0x1C)));
        c.DrawRoundRect(r, 6, 6, Stroke(Theme.PanelBorder, 1));

        bool blink = (t % 0.9) < 0.5;
        float y = r.Top + 24;
        foreach (var ev in sim.Events.Take(6))
        {
            var col = ev.Severity switch
            {
                Severity.Alarm => Theme.StateAlarm,
                Severity.Warn => Theme.Warn,
                _ => Theme.TextDim,
            };
            if (ev.Active && ev.Severity != Severity.Info)
            {
                if (blink) c.DrawCircle(r.Left + 12, y - 5, 4, Fill(col));
            }
            else if (ev.Severity != Severity.Info)
            {
                c.DrawCircle(r.Left + 12, y - 5, 4, Stroke(col.WithAlpha(150), 1.4f));
            }

            var txtCol = ev.Active && ev.Severity != Severity.Info ? col : col.WithAlpha(190);
            Text(c, $"{ev.Time:HH:mm:ss}  {ev.Message}", r.Left + 24, y, 13, txtCol,
                bold: ev.Active && ev.Severity != Severity.Info);
            y += 22;
        }
    }
}

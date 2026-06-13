using EnergyLineHmi.Simulation;
using SkiaSharp;

namespace EnergyLineHmi.Rendering;

/// <summary>タンク・ファン・ポンプ・冷却塔・アナログメーターなどのプロセス要素。</summary>
public sealed partial class HmiRenderer
{
    void DrawProcessElements(SKCanvas c, PlantSimulation sim, double t)
    {
        // 冷却塔（CGS 冷却水系・上部ファン回転）
        DrawCoolingTower(c, new SKRect(300, 455, 400, 545), sim.IsOn("CGS"), t);

        // ボイラ燃焼空気 FD ファン
        DrawFan(c, 415, 684, 18, sim.IsOn("BLR"), t);
        Text(c, "FDファン F-1", 415, 721, 11.5f, Theme.TextDim, SKTextAlign.Center);

        // ポンプ（給水 / 補給水）
        DrawPump(c, 470, 735, sim.FeedwaterFlow > 0.2, "P-1", dirRight: false);
        DrawPump(c, 300, 790, sim.MakeupFlow > 0.15, "P-2", dirRight: true);

        // タンク（水位アニメーション付き）
        DrawTank(c, new SKRect(500, 690, 620, 800), "給水タンク T-1", sim.TankLevel, t);
        DrawTank(c, new SKRect(40, 690, 150, 800), "補給水タンク T-2", sim.MakeupLevel, t);

        // アナログメーター
        DrawAnalogMeter(c, 1125, 165, 36, sim.VoltageKv, 6.0, 7.2, "F2", "kV", "母線電圧", 6.4, 6.8);
        DrawAnalogMeter(c, 1125, 290, 36, sim.FrequencyHz, 49.5, 50.5, "F2", "Hz", "周波数", 49.9, 50.1);
        DrawAnalogMeter(c, 1125, 490, 36, sim.SteamPressure, 0.0, 1.0, "F2", "MPa", "蒸気圧力", 0.66, 0.92);
    }

    /// <summary>軸流ファン。運転中は羽根が回転する。</summary>
    void DrawFan(SKCanvas c, float cx, float cy, float r, bool running, double t)
    {
        c.DrawCircle(cx, cy, r + 4, Fill(Theme.BoxBottom));
        c.DrawCircle(cx, cy, r + 4, Stroke(running ? Theme.Accent : Theme.BoxBorder, running ? 1.8f : 1.4f));

        var blade = running ? Theme.Accent : Theme.StateStop;
        c.Save();
        c.Translate(cx, cy);
        if (running) c.RotateDegrees((float)((t * 230) % 360));
        for (int i = 0; i < 3; i++)
        {
            c.RotateDegrees(120);
            c.DrawOval(new SKRect(-r * 0.22f, -r * 0.95f, r * 0.22f, -r * 0.10f),
                Fill(blade.WithAlpha(210)));
        }
        c.Restore();
        c.DrawCircle(cx, cy, r * 0.24f, Fill(Theme.TextMain));
    }

    /// <summary>冷却塔（本体 + 上部ファン + 水槽）。</summary>
    void DrawCoolingTower(SKCanvas c, SKRect r, bool running, double t)
    {
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(r.Left, r.Top), new SKPoint(r.Left, r.Bottom),
            new[] { Theme.BoxTop, Theme.BoxBottom }, null, SKShaderTileMode.Clamp);
        _fill.Color = SKColors.White;
        _fill.Shader = shader;
        c.DrawRoundRect(r, 6, 6, _fill);
        _fill.Shader = null;
        c.DrawRoundRect(r, 6, 6, Stroke(Theme.BoxBorder, 1.3f));

        DrawFan(c, r.MidX, r.Top + 30, 17, running, t);
        Text(c, "冷却塔 CT-1", r.MidX, r.Bottom - 26, 11.5f, Theme.TextMain, SKTextAlign.Center, bold: true);

        // 下部水槽
        var basin = new SKRect(r.Left + 4, r.Bottom - 18, r.Right - 4, r.Bottom - 4);
        c.DrawRoundRect(basin, 3, 3, Fill(Theme.Condensate.WithAlpha(running ? (byte)110 : (byte)50)));
    }

    /// <summary>渦巻ポンプ記号（円 + 流れ方向の三角）。</summary>
    void DrawPump(SKCanvas c, float cx, float cy, bool running, string name, bool dirRight)
    {
        c.DrawCircle(cx, cy, 13, Fill(Theme.BoxBottom));
        c.DrawCircle(cx, cy, 13, Stroke(running ? Theme.StateRun : Theme.StateStop, 2f));

        float d = dirRight ? 1f : -1f;
        using var tri = new SKPath();
        tri.MoveTo(cx - 6 * d, cy - 7);
        tri.LineTo(cx - 6 * d, cy + 7);
        tri.LineTo(cx + 8 * d, cy);
        tri.Close();
        c.DrawPath(tri, Fill(running ? Theme.Condensate : Theme.StateStop));

        Text(c, name, cx, cy + 29, 11, Theme.TextDim, SKTextAlign.Center);
    }

    /// <summary>水位表示付きタンク。低水位（25% 未満）で警告色。</summary>
    void DrawTank(SKCanvas c, SKRect r, string name, double levelPct, double t)
    {
        Text(c, name, r.MidX, r.Top - 8, 12.5f, Theme.TextDim, SKTextAlign.Center, bold: true);

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(r.Left, r.Top), new SKPoint(r.Left, r.Bottom),
            new[] { Theme.BoxTop, Theme.BoxBottom }, null, SKShaderTileMode.Clamp);
        _fill.Color = SKColors.White;
        _fill.Shader = shader;
        c.DrawRoundRect(r, 9, 9, _fill);
        _fill.Shader = null;

        bool low = levelPct < 25;

        // 液面（内側をクリップして描画。表面はゆっくり波打つ）
        float h = (float)((r.Height - 8) * Math.Clamp(levelPct, 0, 100) / 100.0);
        if (h > 2)
        {
            var inner = new SKRect(r.Left + 4, r.Top + 4, r.Right - 4, r.Bottom - 4);
            c.Save();
            c.ClipRoundRect(new SKRoundRect(inner, 6), SKClipOperation.Intersect, true);

            float surfY = r.Bottom - 4 - h;
            using var liquid = new SKPath();
            liquid.MoveTo(inner.Left, inner.Bottom);
            liquid.LineTo(inner.Left, surfY);
            const int seg = 8;
            for (int i = 0; i <= seg; i++)
            {
                float x = inner.Left + inner.Width * i / seg;
                float y = surfY + 1.6f * MathF.Sin((float)(t * 2.2) + i * 1.1f);
                liquid.LineTo(x, y);
            }
            liquid.LineTo(inner.Right, inner.Bottom);
            liquid.Close();

            var liqCol = low ? Theme.Warn : Theme.Condensate;
            c.DrawPath(liquid, Fill(liqCol.WithAlpha(70)));
            c.DrawPath(liquid, Stroke(liqCol.WithAlpha(200), 1.6f));
            c.Restore();
        }

        // 目盛（25 / 50 / 75%）
        for (int i = 1; i <= 3; i++)
        {
            float y = r.Bottom - 4 - (r.Height - 8) * i / 4f;
            c.DrawLine(r.Left + 4, y, r.Left + 12, y, Stroke(Theme.TextDim.WithAlpha(140), 1));
        }

        c.DrawRoundRect(r, 9, 9, Stroke(low ? Theme.Warn : Theme.BoxBorder, low ? 2f : 1.3f));
        Text(c, $"{levelPct:F0}%", r.MidX, r.MidY + 6, 17,
            low ? Theme.Warn : Theme.TextMain, SKTextAlign.Center, bold: true);
    }

    /// <summary>アナログ指針メーター（270° スケール + 正常域の緑帯）。</summary>
    void DrawAnalogMeter(SKCanvas c, float cx, float cy, float r,
        double value, double min, double max, string fmt, string unit, string label,
        double okLo, double okHi)
    {
        const float start = 135f, sweep = 270f;

        c.DrawCircle(cx, cy, r, Fill(new SKColor(0x13, 0x1B, 0x26)));
        c.DrawCircle(cx, cy, r, Stroke(Theme.BoxBorder, 1.6f));

        // 正常域（緑帯）
        float a0 = start + sweep * (float)((okLo - min) / (max - min));
        float a1 = start + sweep * (float)((okHi - min) / (max - min));
        var zone = new SKRect(cx - r + 7, cy - r + 7, cx + r - 7, cy + r - 7);
        c.DrawArc(zone, a0, a1 - a0, false, Stroke(Theme.StateRun.WithAlpha(170), 3.5f));

        // 目盛
        for (int i = 0; i <= 10; i++)
        {
            float a = (start + sweep * i / 10f) * MathF.PI / 180f;
            bool major = i % 5 == 0;
            float r1 = r - (major ? 12 : 8), r2 = r - 3;
            c.DrawLine(
                cx + MathF.Cos(a) * r1, cy + MathF.Sin(a) * r1,
                cx + MathF.Cos(a) * r2, cy + MathF.Sin(a) * r2,
                Stroke(Theme.TextDim.WithAlpha(major ? (byte)220 : (byte)120), major ? 1.8f : 1f));
        }

        // 指針
        double frac = Math.Clamp((value - min) / (max - min), 0, 1);
        float na = (start + sweep * (float)frac) * MathF.PI / 180f;
        c.DrawLine(
            cx - MathF.Cos(na) * 6, cy - MathF.Sin(na) * 6,
            cx + MathF.Cos(na) * (r - 14), cy + MathF.Sin(na) * (r - 14),
            Stroke(SKColors.White, 2.2f, SKStrokeCap.Round));
        c.DrawCircle(cx, cy, 4, Fill(Theme.Accent));

        Text(c, $"{value.ToString(fmt)} {unit}", cx, cy + r * 0.62f, 11.5f, Theme.TextMain,
            SKTextAlign.Center, bold: true);
        Text(c, label, cx, cy + r + 17, 12, Theme.TextDim, SKTextAlign.Center, bold: true);
    }
}

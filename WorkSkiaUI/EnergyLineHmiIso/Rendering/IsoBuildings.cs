using EnergyLineHmiIso.Simulation;
using SkiaSharp;

namespace EnergyLineHmiIso.Rendering;

/// <summary>建物・タンク・冷却塔・ソーラーなど構造物のアイソメ描画。</summary>
public sealed partial class IsoRenderer
{
    void DrawStructures(SKCanvas c, PlantSimulation sim, double t, string? hoverId)
    {
        _hitRects.Clear();

        foreach (var b in DrawOrderBlds)
        {
            var st = StateOf(b.Id, sim);
            var accent = AccentOf(b.Id);
            var edge = EdgeColor(st, accent, t);
            bool hover = hoverId == b.Id;
            bool running = st != EquipState.Stopped;

            if (st == EquipState.Alarm) DrawAlarmRings(c, b, t);
            if (hover) DrawFootprintGlow(c, b);

            switch (b.Kind)
            {
                case "tank":
                    DrawTankStructure(c, b, sim, accent, edge);
                    break;
                case "solar":
                    DrawSolar(c, b, edge, running);
                    break;
                default:
                    DrawIsoBox(c, b.X0, b.Y0, b.X1, b.Y1, 0, b.H, accent, edge, hover);
                    DrawAddons(c, b, running, t, accent, edge);
                    break;
            }

            _hitRects[b.Id] = ProjRect(b);
        }
    }

    // ---------------- 直方体 ----------------

    void DrawIsoBox(SKCanvas c, float x0, float y0, float x1, float y1, float zb, float zt,
        SKColor accent, SKColor edge, bool hover = false)
    {
        var b0 = W(x1, y0, zb); var c0 = W(x1, y1, zb); var d0 = W(x0, y1, zb);
        var a1 = W(x0, y0, zt); var b1 = W(x1, y0, zt); var c1 = W(x1, y1, zt); var d1 = W(x0, y1, zt);

        using var top = Quad(a1, b1, c1, d1);
        using var right = Quad(b1, c1, c0, b0);   // x = x1 面（右下向き）
        using var left = Quad(d1, c1, c0, d0);    // y = y1 面（左下向き）

        c.DrawPath(right, Fill(Mix(new SKColor(0x10, 0x19, 0x26), accent, 0.10f)));
        c.DrawPath(left, Fill(Mix(new SKColor(0x0B, 0x12, 0x1C), accent, 0.07f)));
        c.DrawPath(top, Fill(Mix(new SKColor(0x1A, 0x28, 0x3A), accent, 0.16f)));

        if (hover)
        {
            var g = Stroke(SKColors.White.WithAlpha(160), 2.4f);
            g.MaskFilter = IsoTheme.GlowM;
            c.DrawPath(top, g);
            g.MaskFilter = null;
        }

        var e = Stroke(edge.WithAlpha(220), 1.4f);
        c.DrawPath(top, e);
        c.DrawLine(b1, b0, e);
        c.DrawLine(c1, c0, e);
        c.DrawLine(d1, d0, e);
        c.DrawLine(b0, c0, e);
        c.DrawLine(c0, d0, e);
    }

    static SKPath Quad(SKPoint p1, SKPoint p2, SKPoint p3, SKPoint p4)
    {
        var p = new SKPath();
        p.MoveTo(p1); p.LineTo(p2); p.LineTo(p3); p.LineTo(p4);
        p.Close();
        return p;
    }

    // ---------------- 種別ごとの装飾 ----------------

    void DrawAddons(SKCanvas c, Bld b, bool running, double t, SKColor accent, SKColor edge)
    {
        float cx = (b.X0 + b.X1) / 2, cy = (b.Y0 + b.Y1) / 2;
        switch (b.Kind)
        {
            case "sub":
            {
                // 引込鉄構（マスト + 腕金 + 碍子）
                var mTop = W(cx, cy, b.H + 0.55f);
                c.DrawLine(W(cx, cy, b.H), mTop, Stroke(edge.WithAlpha(200), 2));
                var armL = W(cx - 0.5f, cy, b.H + 0.25f);
                var armR = W(cx + 0.5f, cy, b.H + 0.25f);
                c.DrawLine(armL, armR, Stroke(edge.WithAlpha(200), 2));
                foreach (var p in new[] { armL, armR, mTop })
                {
                    var g = Fill(IsoTheme.Electric.WithAlpha(160));
                    g.MaskFilter = IsoTheme.GlowS;
                    c.DrawCircle(p, 3.5f, g);
                    g.MaskFilter = null;
                }
                break;
            }
            case "trafo":
            {
                // 放熱フィン（右面の縦リブ）とブッシング
                foreach (var f in new[] { 0.3f, 0.5f, 0.7f })
                {
                    float fy = b.Y0 + (b.Y1 - b.Y0) * f;
                    c.DrawLine(W(b.X1, fy, 0.12f), W(b.X1, fy, b.H - 0.15f),
                        Stroke(accent.WithAlpha(90), 1.2f));
                }
                foreach (var dx in new[] { -0.3f, 0.3f })
                {
                    var bp = W(cx + dx, cy, b.H + 0.18f);
                    c.DrawLine(W(cx + dx, cy, b.H), bp, Stroke(edge.WithAlpha(180), 1.6f));
                    c.DrawCircle(bp, 2.5f, Fill(IsoTheme.Electric.WithAlpha(200)));
                }
                break;
            }
            case "cgs":
            {
                // 吸気ルーバー（左面）+ 排気スタック
                foreach (var f in new[] { 0.35f, 0.55f, 0.75f })
                    c.DrawLine(W(b.X0 + 0.25f, b.Y1, b.H * f), W(b.X1 - 0.25f, b.Y1, b.H * f),
                        Stroke(accent.WithAlpha(80), 1.1f));
                DrawCylinder(c, 6.55f, 4.35f, 0.17f, b.H, b.H + 0.85f, IsoTheme.Stop, edge);
                break;
            }
            case "boiler":
            {
                // 煙突 + 頂部の点滅灯
                DrawCylinder(c, 6.5f, 7.5f, 0.28f, b.H, b.H + 2.2f, IsoTheme.Stop, edge);
                var top = W(6.5f, 7.5f, b.H + 2.28f);
                byte a = (byte)(120 + 110 * Math.Sin(t * 4) * 0.5 + 55);
                var g = Fill(IsoTheme.Alarm.WithAlpha(a));
                g.MaskFilter = IsoTheme.GlowS;
                c.DrawCircle(top, 3.2f, g);
                g.MaskFilter = null;
                break;
            }
            case "ct":
            {
                // 上面の大型ファン + 下部水槽の発光ライン
                DrawRoofFan(c, cx, cy, b.H, 0.62f, running, t, IsoTheme.Cool);
                var w1 = W(b.X1, b.Y0, 0.16f); var w2 = W(b.X1, b.Y1, 0.16f); var w3 = W(b.X0, b.Y1, 0.16f);
                var wp = Stroke(IsoTheme.Cool.WithAlpha(running ? (byte)190 : (byte)70), 2);
                c.DrawLine(w1, w2, wp);
                c.DrawLine(w2, w3, wp);
                break;
            }
            case "factory":
            {
                // 屋上の採光スリット + 換気ファン
                foreach (var f in new[] { 0.33f, 0.66f })
                {
                    float fy = b.Y0 + (b.Y1 - b.Y0) * f;
                    c.DrawLine(W(b.X0 + 0.25f, fy, b.H), W(b.X1 - 0.25f, fy, b.H),
                        Stroke(accent.WithAlpha(70), 1.2f));
                }
                DrawRoofFan(c, b.X1 - 0.55f, b.Y0 + 0.55f, b.H, 0.3f, running, t, accent);
                break;
            }
            case "util":
            {
                // 屋上の空調ユニット
                DrawIsoBox(c, b.X0 + 0.4f, b.Y0 + 0.4f, b.X0 + 1.1f, b.Y0 + 1.0f, b.H, b.H + 0.35f,
                    accent, edge.WithAlpha(170));
                DrawIsoBox(c, b.X1 - 1.2f, b.Y1 - 1.1f, b.X1 - 0.5f, b.Y1 - 0.5f, b.H, b.H + 0.3f,
                    accent, edge.WithAlpha(170));
                break;
            }
            case "dry":
            {
                // 排気ベント + 炉内の熱発光
                DrawCylinder(c, 9.3f, 10.7f, 0.13f, b.H, b.H + 0.7f, IsoTheme.Stop, edge);
                if (running)
                {
                    byte a = (byte)(90 + 60 * Math.Sin(t * 2.4));
                    var g = Fill(IsoTheme.Steam.WithAlpha(a));
                    g.MaskFilter = IsoTheme.GlowM;
                    c.DrawCircle(W(cx, b.Y1, b.H * 0.45f), 7, g);
                    g.MaskFilter = null;
                }
                break;
            }
            case "gasin":
            {
                // ガバナステーションのマニホールド（小シリンダ列）
                for (int k = 0; k < 3; k++)
                    DrawCylinder(c, 0.9f + k * 0.5f, 8.4f, 0.13f, b.H, b.H + 0.45f, IsoTheme.Gas, edge);
                break;
            }
        }
    }

    // ---------------- 円筒（タンク・煙突） ----------------

    void DrawCylinder(SKCanvas c, float wx, float wy, float r, float zb, float zt,
        SKColor accent, SKColor edge, float? levelPct = null)
    {
        float rx = r * T * 1.414f, ry = r * T * 0.707f;
        var pb = W(wx, wy, zb);
        var pt = W(wx, wy, zt);

        using var body = new SKPath();
        body.MoveTo(pt.X - rx, pt.Y);
        body.LineTo(pb.X - rx, pb.Y);
        body.ArcTo(new SKRect(pb.X - rx, pb.Y - ry, pb.X + rx, pb.Y + ry), 180, -180, false);
        body.LineTo(pt.X + rx, pt.Y);
        body.ArcTo(new SKRect(pt.X - rx, pt.Y - ry, pt.X + rx, pt.Y + ry), 0, 180, false);
        body.Close();

        using var grad = SKShader.CreateLinearGradient(
            new SKPoint(pb.X - rx, 0), new SKPoint(pb.X + rx, 0),
            new[] { Mix(IsoTheme.Bg, accent, 0.08f), Mix(IsoTheme.Bg, accent, 0.26f), Mix(IsoTheme.Bg, accent, 0.05f) },
            new float[] { 0, 0.45f, 1 }, SKShaderTileMode.Clamp);
        _fill.Shader = grad;
        _fill.Color = SKColors.White.WithAlpha(240);
        c.DrawPath(body, _fill);
        _fill.Shader = null;

        // 液面（ホログラム風に内部が透ける）
        if (levelPct is { } lv && lv > 1)
        {
            float zl = zb + (zt - zb) * (lv / 100f);
            var pl = W(wx, wy, zl);
            float rx2 = rx * 0.9f, ry2 = ry * 0.9f;

            using var liquid = new SKPath();
            liquid.MoveTo(pl.X - rx2, pl.Y);
            liquid.LineTo(pb.X - rx2, pb.Y);
            liquid.ArcTo(new SKRect(pb.X - rx2, pb.Y - ry2, pb.X + rx2, pb.Y + ry2), 180, -180, false);
            liquid.LineTo(pl.X + rx2, pl.Y);
            liquid.ArcTo(new SKRect(pl.X - rx2, pl.Y - ry2, pl.X + rx2, pl.Y + ry2), 0, 180, false);
            liquid.Close();
            c.DrawPath(liquid, Fill(IsoTheme.Water.WithAlpha(60)));

            var surf = new SKRect(pl.X - rx2, pl.Y - ry2, pl.X + rx2, pl.Y + ry2);
            c.DrawOval(surf, Fill(IsoTheme.Water.WithAlpha(110)));
            var g = Stroke(IsoTheme.Water.WithAlpha(200), 1.6f);
            g.MaskFilter = IsoTheme.GlowS;
            c.DrawOval(surf, g);
            g.MaskFilter = null;
        }

        // 上面と輪郭
        var topRect = new SKRect(pt.X - rx, pt.Y - ry, pt.X + rx, pt.Y + ry);
        c.DrawOval(topRect, Fill(Mix(new SKColor(0x1A, 0x28, 0x3A), accent, 0.18f)));
        c.DrawOval(topRect, Stroke(edge.WithAlpha(220), 1.4f));
        c.DrawLine(pt.X - rx, pt.Y, pb.X - rx, pb.Y, Stroke(edge.WithAlpha(160), 1.2f));
        c.DrawLine(pt.X + rx, pt.Y, pb.X + rx, pb.Y, Stroke(edge.WithAlpha(160), 1.2f));
        c.DrawArc(new SKRect(pb.X - rx, pb.Y - ry, pb.X + rx, pb.Y + ry), 0, 180, false,
            Stroke(edge.WithAlpha(120), 1.1f));
    }

    void DrawTankStructure(SKCanvas c, Bld b, PlantSimulation sim, SKColor accent, SKColor edge)
    {
        float cx = (b.X0 + b.X1) / 2, cy = (b.Y0 + b.Y1) / 2, r = (b.X1 - b.X0) / 2;
        float lv = (float)(b.Id == "T1" ? sim.TankLevel : sim.MakeupLevel);
        bool low = lv < 25;

        DrawCylinder(c, cx, cy, r, 0, b.H, accent, low ? IsoTheme.Warn : edge, lv);

        var pb = W(cx, cy, 0);
        var pt = W(cx, cy, b.H);
        Text(c, $"{lv:F0}%", pb.X, (pb.Y + pt.Y) / 2 + r * T * 0.35f, 12.5f,
            low ? IsoTheme.Warn : IsoTheme.TextMain, SKTextAlign.Center, bold: true);
    }

    // ---------------- 屋上ファン（アイソメ平面で回転） ----------------

    void DrawRoofFan(SKCanvas c, float wx, float wy, float z, float rWorld, bool running, double t, SKColor accent)
    {
        var ctr = W(wx, wy, z);
        float r = rWorld * T;

        // 屋根平面（ワールド XY）への射影行列: (u,v) → (u−v+cx, 0.5u+0.5v+cy)
        var m = new SKMatrix(1, -1, ctr.X, 0.5f, 0.5f, ctr.Y, 0, 0, 1);
        c.Save();
        c.Concat(ref m);

        c.DrawCircle(0, 0, r, Fill(new SKColor(0x0A, 0x14, 0x20, 0xF0)));
        c.DrawCircle(0, 0, r, Stroke(accent.WithAlpha(running ? (byte)220 : (byte)110), 1.6f));

        if (running) c.RotateDegrees((float)((t * 240) % 360));
        var blade = running ? accent : IsoTheme.Stop;
        for (int i = 0; i < 3; i++)
        {
            c.RotateDegrees(120);
            c.DrawOval(new SKRect(-r * 0.2f, -r * 0.9f, r * 0.2f, -r * 0.12f), Fill(blade.WithAlpha(190)));
        }
        c.Restore();

        c.DrawCircle(ctr, 2.5f, Fill(IsoTheme.TextMain));
    }

    // ---------------- ソーラーアレイ ----------------

    void DrawSolar(SKCanvas c, Bld b, SKColor edge, bool on)
    {
        DrawIsoBox(c, b.X0, b.Y0, b.X1, b.Y1, 0, 0.12f, IsoTheme.Electric, edge.WithAlpha(130));

        const int cols = 4, rows = 2;
        float cw = (b.X1 - b.X0) / cols, ch = (b.Y1 - b.Y0) / rows;
        var panelEdge = (on ? IsoTheme.Cyan : IsoTheme.Stop).WithAlpha(on ? (byte)170 : (byte)110);

        for (int i = 0; i < cols; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                float px0 = b.X0 + i * cw + 0.08f, px1 = b.X0 + (i + 1) * cw - 0.08f;
                float py0 = b.Y0 + j * ch + 0.10f, py1 = b.Y0 + (j + 1) * ch - 0.10f;
                using var q = Quad(W(px0, py0, 0.52f), W(px1, py0, 0.52f), W(px1, py1, 0.18f), W(px0, py1, 0.18f));
                c.DrawPath(q, Fill(new SKColor(0x0D, 0x2A, 0x4E, 0xE6)));
                c.DrawPath(q, Stroke(panelEdge, 1.1f));
                c.DrawLine(W((px0 + px1) / 2, py0, 0.52f), W((px0 + px1) / 2, py1, 0.18f),
                    Stroke(IsoTheme.Cyan.WithAlpha(60), 1));
            }
        }
    }

    // ---------------- エフェクト ----------------

    void DrawAlarmRings(SKCanvas c, Bld b, double t)
    {
        float cx = (b.X0 + b.X1) / 2, cy = (b.Y0 + b.Y1) / 2;
        var ctr = W(cx, cy);
        for (int k = 0; k < 2; k++)
        {
            double p = (t * 0.8 + k * 0.5) % 1.0;
            float w = (float)(0.6 + p * 1.8);
            byte a = (byte)(170 * (1 - p));
            c.DrawOval(new SKRect(
                ctr.X - w * T * 1.414f, ctr.Y - w * T * 0.707f,
                ctr.X + w * T * 1.414f, ctr.Y + w * T * 0.707f),
                Stroke(IsoTheme.Alarm.WithAlpha(a), 2f));
        }
    }

    void DrawFootprintGlow(SKCanvas c, Bld b)
    {
        using var q = Quad(W(b.X0, b.Y0), W(b.X1, b.Y0), W(b.X1, b.Y1), W(b.X0, b.Y1));
        var g = Stroke(IsoTheme.Cyan.WithAlpha(170), 2.2f);
        g.MaskFilter = IsoTheme.GlowM;
        c.DrawPath(q, g);
        g.MaskFilter = null;
        c.DrawPath(q, Fill(IsoTheme.Cyan.WithAlpha(22)));
    }

    SKRect ProjRect(Bld b)
    {
        var pts = new[]
        {
            W(b.X0, b.Y0), W(b.X1, b.Y0), W(b.X1, b.Y1), W(b.X0, b.Y1),
            W(b.X0, b.Y0, b.H), W(b.X1, b.Y0, b.H), W(b.X1, b.Y1, b.H), W(b.X0, b.Y1, b.H),
        };
        float minX = pts[0].X, maxX = pts[0].X, minY = pts[0].Y, maxY = pts[0].Y;
        foreach (var p in pts)
        {
            minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X);
            minY = Math.Min(minY, p.Y); maxY = Math.Max(maxY, p.Y);
        }
        return new SKRect(minX, minY, maxX, maxY);
    }

    // ---------------- ホロタグ ----------------

    void DrawTags(SKCanvas c, PlantSimulation sim, double t, string? hoverId)
    {
        foreach (var b in DrawOrderBlds)
        {
            var st = StateOf(b.Id, sim);
            var accent = AccentOf(b.Id);
            float cx = (b.X0 + b.X1) / 2, cy = (b.Y0 + b.Y1) / 2;

            float zTop = b.H + b.Kind switch
            {
                "boiler" => 2.4f,
                "sub" => 0.7f,
                "cgs" => 0.95f,
                "dry" => 0.8f,
                _ => 0.15f,
            };
            var roof = W(cx, cy, zTop);
            float tx = roof.X, ty = roof.Y - 30;

            string val = ValueOf(b.Id, sim);
            _text.TextSize = 10.5f; _text.Typeface = IsoTheme.Jp;
            float w1 = _text.MeasureText(b.Name);
            _text.TextSize = 12.5f; _text.Typeface = IsoTheme.JpBold;
            float w2 = _text.MeasureText(val);
            float w = Math.Max(w1, w2) + 28;
            var r = new SKRect(tx - w / 2, ty - 32, tx + w / 2, ty);

            bool hover = hoverId == b.Id;

            // リーダー線
            c.DrawLine(tx, ty, roof.X, roof.Y - 3, Stroke(accent.WithAlpha(90), 1));
            c.DrawCircle(roof.X, roof.Y - 3, 1.8f, Fill(accent.WithAlpha(170)));

            using var path = CutCorner(r, 5);
            c.DrawPath(path, Fill(new SKColor(0x07, 0x0D, 0x16, hover ? (byte)0xF5 : (byte)0xD2)));
            c.DrawPath(path, Stroke(accent.WithAlpha(hover ? (byte)235 : (byte)110), 1));

            bool blink = (t % 0.9) < 0.5;
            var dot = st switch
            {
                EquipState.Running => IsoTheme.Green,
                EquipState.Stopped => IsoTheme.Stop,
                _ => blink ? IsoTheme.Alarm : IsoTheme.Alarm.WithAlpha(70),
            };
            c.DrawCircle(r.Left + 10, r.Top + 9, 3, Fill(dot));
            Text(c, b.Name, r.Left + 18, r.Top + 13, 10.5f, IsoTheme.TextDim);
            Text(c, val, r.Left + 18, r.Top + 28, 12.5f, accent, bold: true);
        }
    }

    // ---------------- 属性ヘルパ ----------------

    static SKColor AccentOf(string id) => id switch
    {
        "GASIN" => IsoTheme.Gas,
        "BLR" or "DRY" => IsoTheme.Steam,
        "CT" => IsoTheme.Cool,
        "T1" or "T2" => IsoTheme.Water,
        "PV" or "CGS" => IsoTheme.Green,
        _ => IsoTheme.Electric,
    };

    static EquipState StateOf(string id, PlantSimulation sim) => id switch
    {
        "CT" => sim.IsOn("CGS") ? EquipState.Running : EquipState.Stopped,
        "T1" or "T2" => EquipState.Running,
        _ => sim.Equip.TryGetValue(id, out var e) ? e.State : EquipState.Running,
    };

    static SKColor EdgeColor(EquipState st, SKColor accent, double t) => st switch
    {
        EquipState.Stopped => IsoTheme.Stop,
        EquipState.Alarm => (t % 0.8) < 0.45 ? IsoTheme.Alarm : IsoTheme.Alarm.WithAlpha(90),
        _ => accent,
    };

    static string ValueOf(string id, PlantSimulation s) => id switch
    {
        "GRID" => $"{s.GridKw:N0} kW",
        "TR1" => $"負荷率 {s.GridKw / PlantSimulation.ContractKw * 100:N0} %",
        "PV" => $"{s.PvKw:N0} kW",
        "CGS" => $"{s.CgsKw:N0} kW",
        "CT" => $"{s.CoolingFlow:N0} m³/h",
        "GASIN" => $"{s.GasTotal:N0} m³/h",
        "BLR" => $"{s.SteamBlr:F1} t/h",
        "LINEA" => $"{s.LineAKw:N0} kW",
        "LINEB" => $"{s.LineBKw:N0} kW",
        "UTIL" => $"{s.UtilKw:N0} kW",
        "DRY" => $"{s.SteamDry:F1} t/h",
        "T1" => $"{s.TankLevel:F0} % / {s.FeedwaterFlow:F1} t/h",
        "T2" => $"{s.MakeupLevel:F0} %",
        _ => "--",
    };
}

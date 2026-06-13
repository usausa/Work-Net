using EnergyLineHmiIso.Simulation;
using SkiaSharp;

namespace EnergyLineHmiIso.Rendering;

/// <summary>
/// クォータービュー（2:1 アイソメトリック）プラントレンダラ。
/// ワールド座標は敷地グリッド 16 x 13（z は高さ）、仮想キャンバスは 1600x900。
/// </summary>
public sealed partial class IsoRenderer
{
    public const float VW = 1600f;
    public const float VH = 900f;

    const float T = 38f;    // ワールド 1 単位の横半幅 px
    const float TZ = 30f;   // 高さ 1 単位の縦 px
    const float OX = 535f, OY = 180f;
    const float GX = 16f, GY = 13f;   // 敷地グリッド数

    float _scale = 1, _ox, _oy;

    readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    readonly SKPaint _text = new() { IsAntialias = true };

    readonly Dictionary<string, SKRect> _hitRects = new();

    /// <summary>ワールド座標 → 仮想キャンバス座標（2:1 投影）。</summary>
    internal static SKPoint W(float x, float y, float z = 0) =>
        new(OX + (x - y) * T, OY + (x + y) * (T * 0.5f) - z * TZ);

    // ---------------- 構造物定義 ----------------

    internal sealed record Bld(string Id, string Name,
        float X0, float Y0, float X1, float Y1, float H, string Kind);

    internal static readonly Bld[] Blds =
    {
        new("GRID",  "受電変電所 66kV",     0.5f, 0.5f, 2.5f, 2.5f, 1.2f,  "sub"),
        new("TR1",   "主変圧器 TR-1",       3.5f, 1.0f, 5.0f, 2.5f, 1.0f,  "trafo"),
        new("PV",    "太陽光発電 PV-1",     6.0f, 0.5f, 9.5f, 2.5f, 0.35f, "solar"),
        new("CGS",   "ガスエンジン CGS-1",  4.5f, 4.0f, 7.0f, 6.0f, 1.5f,  "cgs"),
        new("CT",    "冷却塔 CT-1",         8.0f, 4.0f, 9.8f, 5.8f, 1.1f,  "ct"),
        new("GASIN", "都市ガス受入",        0.5f, 7.5f, 2.3f, 9.3f, 0.8f,  "gasin"),
        new("BLR",   "貫流ボイラ B-1",      4.5f, 7.0f, 7.0f, 9.5f, 1.7f,  "boiler"),
        new("LINEA", "生産ライン A",        12f,  4.5f, 15f,  6.5f, 1.9f,  "factory"),
        new("LINEB", "生産ライン B",        12f,  7.3f, 15f,  9.3f, 1.9f,  "factory"),
        new("UTIL",  "ユーティリティ",      12f,  10.1f, 14.5f, 11.9f, 1.3f, "util"),
        new("DRY",   "乾燥炉 D-1",          7.5f, 10.3f, 10f, 12.3f, 1.4f, "dry"),
        new("T1",    "給水タンク T-1",      3.3f, 10.0f, 5.1f, 11.8f, 1.7f, "tank"),
        new("T2",    "補給水タンク T-2",    0.9f, 10.7f, 2.3f, 12.1f, 1.3f, "tank"),
    };

    static readonly Bld[] DrawOrderBlds = Blds.OrderBy(b => b.X1 + b.Y1).ToArray();

    // ---------------- エネルギーフロー定義 ----------------

    sealed record Flow(SKColor Color, float Width,
        (float X, float Y, float Z)[] Pts,
        Func<PlantSimulation, double> Value, double Nominal,
        string? Unit = null, (float X, float Y)? LabelAt = null, string Fmt = "N0");

    static readonly Flow[] Flows =
    {
        // 電力（受電架空線 → 変電所 → 変圧器 → 幹線 → 各負荷）
        new(IsoTheme.Electric, 3f,   new[]{ (-2.2f, 1.5f, 2.4f), (1.5f, 1.5f, 1.45f) }, s => s.GridKw, 2000),
        new(IsoTheme.Electric, 3f,   new[]{ (2.5f, 1.5f, 0f), (3.5f, 1.5f, 0f) }, s => s.GridKw, 2000),
        new(IsoTheme.Electric, 3f,   new[]{ (4.25f, 2.5f, 0f), (4.25f, 3.2f, 0f) }, s => s.GridKw, 2000),
        new(IsoTheme.Electric, 3.5f, new[]{ (4.25f, 3.2f, 0f), (11.5f, 3.2f, 0f) }, s => s.TotalLoadKw, 2400),
        new(IsoTheme.Electric, 3f,   new[]{ (7.75f, 2.5f, 0f), (7.75f, 3.2f, 0f) }, s => s.PvKw, 350),
        new(IsoTheme.Electric, 3f,   new[]{ (5.75f, 4f, 0f), (5.75f, 3.2f, 0f) }, s => s.CgsKw, 500),
        new(IsoTheme.Electric, 3f,   new[]{ (11.5f, 3.2f, 0f), (11.5f, 5.5f, 0f), (12f, 5.5f, 0f) }, s => s.LineAKw, 800),
        new(IsoTheme.Electric, 3f,   new[]{ (11.5f, 5.5f, 0f), (11.5f, 8.3f, 0f), (12f, 8.3f, 0f) }, s => s.LineBKw, 650),
        new(IsoTheme.Electric, 3f,   new[]{ (11.5f, 8.3f, 0f), (11.5f, 11f, 0f), (12f, 11f, 0f) }, s => s.UtilKw, 600),

        // 都市ガス
        new(IsoTheme.Gas, 3.5f, new[]{ (2.3f, 8.4f, 0f), (3.7f, 8.4f, 0f) }, s => s.GasTotal, 700, "m³/h", (2.9f, 8.4f)),
        new(IsoTheme.Gas, 3f,   new[]{ (3.7f, 8.4f, 0f), (4.5f, 8.4f, 0f) }, s => s.GasBlr, 550),
        new(IsoTheme.Gas, 3f,   new[]{ (3.7f, 8.4f, 0f), (3.7f, 5f, 0f), (4.5f, 5f, 0f) }, s => s.GasCgs, 150),

        // 蒸気（ボイラ → ヘッダ分岐、CGS 排熱合流）
        new(IsoTheme.Steam, 3.5f, new[]{ (7f, 8.25f, 0f), (9.5f, 8.25f, 0f) }, s => s.SteamBlr, 8, "t/h", (8.3f, 8.25f), "F1"),
        new(IsoTheme.Steam, 3f,   new[]{ (7f, 4.7f, 0f), (7.6f, 4.7f, 0f), (7.6f, 8.25f, 0f) }, s => s.SteamCgs, 2),
        new(IsoTheme.Steam, 3f,   new[]{ (9.5f, 8.25f, 0f), (9.5f, 6f, 0f), (12f, 6f, 0f) }, s => s.SteamHeatA, 5),
        new(IsoTheme.Steam, 3f,   new[]{ (9.5f, 8.25f, 0f), (9.5f, 10.3f, 0f) }, s => s.SteamDry, 3),

        // 復水 / 給水 / 補給水
        new(IsoTheme.Water, 3f, new[]{ (7.5f, 11.5f, 0f), (4.95f, 11.5f, 0f) }, s => s.CondensateFlow, 5, "t/h", (6.3f, 11.5f), "F1"),
        new(IsoTheme.Water, 3f, new[]{ (4.2f, 10f, 0f), (4.2f, 9f, 0f), (4.5f, 9f, 0f) }, s => s.FeedwaterFlow, 8),
        new(IsoTheme.Water, 3f, new[]{ (2.3f, 11.4f, 0f), (3.5f, 11.4f, 0f) }, s => s.MakeupFlow, 4),

        // CGS 冷却水
        new(IsoTheme.Cool, 3f, new[]{ (7f, 5.3f, 0f), (8f, 5.3f, 0f) }, s => s.CoolingFlow, 50),
    };

    /// <summary>幹線・ヘッダの分岐ノード（発光ポイント）。</summary>
    static readonly (float X, float Y)[] Nodes =
    {
        (4.25f, 3.2f), (5.75f, 3.2f), (7.75f, 3.2f), (11.5f, 3.2f),
        (11.5f, 5.5f), (11.5f, 8.3f), (3.7f, 8.4f), (7.6f, 8.25f), (9.5f, 8.25f),
    };

    readonly SKPath[] _paths;
    readonly SKPathMeasure[] _measures;
    readonly float[] _lengths;

    public IsoRenderer()
    {
        _paths = new SKPath[Flows.Length];
        _measures = new SKPathMeasure[Flows.Length];
        _lengths = new float[Flows.Length];
        for (int i = 0; i < Flows.Length; i++)
        {
            var path = new SKPath();
            var pts = Flows[i].Pts;
            path.MoveTo(W(pts[0].X, pts[0].Y, pts[0].Z));
            for (int k = 1; k < pts.Length; k++)
                path.LineTo(W(pts[k].X, pts[k].Y, pts[k].Z));
            _paths[i] = path;
            _measures[i] = new SKPathMeasure(path);
            _lengths[i] = _measures[i].Length;
        }
    }

    // ---------------- メイン描画 ----------------

    public void Render(SKCanvas canvas, SKImageInfo info, PlantSimulation sim, double t, string? hoverId)
    {
        canvas.Clear(IsoTheme.Bg);

        _scale = Math.Min(info.Width / VW, info.Height / VH);
        _ox = (info.Width - VW * _scale) / 2f;
        _oy = (info.Height - VH * _scale) / 2f;

        canvas.Save();
        canvas.Translate(_ox, _oy);
        canvas.Scale(_scale);

        DrawBackdrop(canvas);
        DrawFlows(canvas, sim, t);
        DrawStructures(canvas, sim, t, hoverId);
        DrawTags(canvas, sim, t, hoverId);
        DrawHeader(canvas, sim, t);
        DrawHud(canvas, sim, t);
        DrawLegend(canvas);

        canvas.Restore();
    }

    /// <summary>デバイス座標 → 機器 ID（操作可能なもののみ、手前優先）。</summary>
    public string? HitTest(SKPoint device, PlantSimulation sim)
    {
        if (_scale <= 0) return null;
        var v = new SKPoint((device.X - _ox) / _scale, (device.Y - _oy) / _scale);
        for (int i = DrawOrderBlds.Length - 1; i >= 0; i--)
        {
            var b = DrawOrderBlds[i];
            if (_hitRects.TryGetValue(b.Id, out var r) && r.Contains(v.X, v.Y)
                && sim.Equip.TryGetValue(b.Id, out var eq) && eq.CanToggle)
                return b.Id;
        }
        return null;
    }

    // ---------------- 背景 ----------------

    void DrawBackdrop(SKCanvas c)
    {
        // 中央が淡く明るいラジアルグラデーション
        using var sh = SKShader.CreateRadialGradient(new SKPoint(592, 450), 720,
            new[] { IsoTheme.BgGlow, IsoTheme.Bg }, null, SKShaderTileMode.Clamp);
        _fill.Shader = sh;
        _fill.Color = SKColors.White;
        c.DrawRect(new SKRect(0, 0, VW, VH), _fill);
        _fill.Shader = null;

        // 敷地グリッド
        for (int x = 0; x <= GX; x++)
            c.DrawLine(W(x, 0), W(x, GY),
                Stroke(IsoTheme.GridLine.WithAlpha(x % 4 == 0 ? (byte)95 : (byte)45), 1));
        for (int y = 0; y <= GY; y++)
            c.DrawLine(W(0, y), W(GX, y),
                Stroke(IsoTheme.GridLine.WithAlpha(y % 4 == 0 ? (byte)95 : (byte)45), 1));

        // 敷地外周（グロー付き）
        using var border = new SKPath();
        border.MoveTo(W(0, 0));
        border.LineTo(W(GX, 0));
        border.LineTo(W(GX, GY));
        border.LineTo(W(0, GY));
        border.Close();
        var g = Stroke(IsoTheme.Cyan.WithAlpha(70), 1.6f);
        g.MaskFilter = IsoTheme.GlowS;
        c.DrawPath(border, g);
        g.MaskFilter = null;
        c.DrawPath(border, Stroke(IsoTheme.Cyan.WithAlpha(120), 1));

        // ビューポート四隅のブラケット
        DrawBracket(c, 18, 86, 1, 1);
        DrawBracket(c, 1166, 86, -1, 1);
        DrawBracket(c, 18, 846, 1, -1);
        DrawBracket(c, 1166, 846, -1, -1);
    }

    void DrawBracket(SKCanvas c, float x, float y, int dx, int dy)
    {
        var p = Stroke(IsoTheme.Cyan.WithAlpha(150), 2);
        c.DrawLine(x, y, x + 26 * dx, y, p);
        c.DrawLine(x, y, x, y + 26 * dy, p);
    }

    // ---------------- エネルギーフロー ----------------

    void DrawFlows(SKCanvas c, PlantSimulation sim, double t)
    {
        for (int i = 0; i < Flows.Length; i++)
        {
            var f = Flows[i];
            double norm = Math.Clamp(f.Value(sim) / f.Nominal, 0, 1.25);
            bool on = norm > 0.03;

            // ベース配管 + 色付きコア
            c.DrawPath(_paths[i], Stroke(IsoTheme.PipeBase, f.Width + 3.5f, SKStrokeCap.Round));
            c.DrawPath(_paths[i], Stroke(f.Color.WithAlpha(on ? (byte)95 : (byte)30), f.Width, SKStrokeCap.Round));

            if (on)
            {
                // ライングロー
                var g = Stroke(f.Color.WithAlpha(50), f.Width + 5f, SKStrokeCap.Round);
                g.MaskFilter = IsoTheme.GlowS;
                c.DrawPath(_paths[i], g);
                g.MaskFilter = null;

                // エネルギーパルス（流量に比例した速度で移動する光点）
                float len = _lengths[i];
                float speed = (float)(45 + 130 * norm);
                int count = Math.Max(1, (int)(len / 110));
                float gap = len / count;
                for (int k = 0; k < count; k++)
                {
                    float d = (float)((t * speed + k * gap) % len);
                    if (!_measures[i].GetPosition(d, out var pos)) continue;
                    var gp = Fill(f.Color.WithAlpha(150));
                    gp.MaskFilter = IsoTheme.GlowM;
                    c.DrawCircle(pos, 5f, gp);
                    gp.MaskFilter = null;
                    c.DrawCircle(pos, 2.2f, Fill(SKColors.White.WithAlpha(235)));
                }
            }

            if (f.Unit is { } u && f.LabelAt is { } la)
            {
                var lp = W(la.X, la.Y);
                Chip(c, $"{f.Value(sim).ToString(f.Fmt)} {u}", lp.X, lp.Y - 16, f.Color);
            }
        }

        // 分岐ノード
        foreach (var (nx, ny) in Nodes)
        {
            var p = W(nx, ny);
            var g = Fill(IsoTheme.Cyan.WithAlpha(110));
            g.MaskFilter = IsoTheme.GlowS;
            c.DrawCircle(p, 4.5f, g);
            g.MaskFilter = null;
            c.DrawCircle(p, 1.8f, Fill(IsoTheme.TextMain));
        }
    }

    // ---------------- 共通ヘルパ ----------------

    /// <summary>角を斜めにカットした近未来風パネルパス。</summary>
    static SKPath CutCorner(SKRect r, float k)
    {
        var p = new SKPath();
        p.MoveTo(r.Left + k, r.Top);
        p.LineTo(r.Right - k, r.Top);
        p.LineTo(r.Right, r.Top + k);
        p.LineTo(r.Right, r.Bottom - k);
        p.LineTo(r.Right - k, r.Bottom);
        p.LineTo(r.Left + k, r.Bottom);
        p.LineTo(r.Left, r.Bottom - k);
        p.LineTo(r.Left, r.Top + k);
        p.Close();
        return p;
    }

    /// <summary>フロー流量などの小型ラベルチップ。</summary>
    void Chip(SKCanvas c, string s, float cx, float cy, SKColor color)
    {
        _text.TextSize = 11.5f;
        _text.Typeface = IsoTheme.JpBold;
        float w = _text.MeasureText(s);
        var r = new SKRect(cx - w / 2 - 8, cy - 9, cx + w / 2 + 8, cy + 9);
        using var path = CutCorner(r, 5);
        c.DrawPath(path, Fill(new SKColor(0x07, 0x0D, 0x16, 0xE6)));
        c.DrawPath(path, Stroke(color.WithAlpha(140), 1));
        Text(c, s, cx, cy + 4.5f, 11.5f, color, SKTextAlign.Center, bold: true);
    }

    static SKColor Mix(SKColor a, SKColor b, float f) => new(
        (byte)(a.Red + (b.Red - a.Red) * f),
        (byte)(a.Green + (b.Green - a.Green) * f),
        (byte)(a.Blue + (b.Blue - a.Blue) * f));

    SKPaint Fill(SKColor c)
    {
        _fill.Color = c;
        _fill.Shader = null;
        _fill.MaskFilter = null;
        return _fill;
    }

    SKPaint Stroke(SKColor c, float w, SKStrokeCap cap = SKStrokeCap.Butt)
    {
        _stroke.Color = c;
        _stroke.StrokeWidth = w;
        _stroke.StrokeCap = cap;
        _stroke.StrokeJoin = SKStrokeJoin.Round;
        _stroke.PathEffect = null;
        _stroke.MaskFilter = null;
        return _stroke;
    }

    void Text(SKCanvas c, string s, float x, float y, float size, SKColor color,
              SKTextAlign align = SKTextAlign.Left, bool bold = false)
    {
        _text.Color = color;
        _text.TextSize = size;
        _text.TextAlign = align;
        _text.Typeface = bold ? IsoTheme.JpBold : IsoTheme.Jp;
        c.DrawText(s, x, y, _text);
    }
}

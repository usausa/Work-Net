using EnergyLineHmi.Simulation;
using SkiaSharp;

namespace EnergyLineHmi.Rendering;

/// <summary>
/// SkiaSharp によるエネルギーライン HMI の描画。
/// 仮想座標 1600x900 でレイアウトし、実キャンバスへ等倍スケールで貼り付ける。
/// </summary>
public sealed partial class HmiRenderer
{
    public const float VW = 1600f;
    public const float VH = 900f;

    float _scale = 1, _ox, _oy;

    readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke };
    readonly SKPaint _text = new() { IsAntialias = true };

    sealed record Pipe(
        SKColor Color, float Width, SKPoint[] Pts,
        Func<PlantSimulation, double> Flow, double Nominal,
        string? Unit = null, SKPoint? LabelPos = null, string Format = "N0");

    static SKPoint P(float x, float y) => new(x, y);

    // ---- 機器ボックス配置（仮想座標） ----
    static readonly Dictionary<string, SKRect> Boxes = new()
    {
        ["GRID"]  = new(40, 120, 200, 184),
        ["TR1"]   = new(290, 120, 450, 184),
        ["PV"]    = new(290, 228, 450, 292),
        ["CGS"]   = new(290, 336, 450, 400),
        ["LINEA"] = new(880, 120, 1060, 184),
        ["LINEB"] = new(880, 228, 1060, 292),
        ["UTIL"]  = new(880, 336, 1060, 400),
        ["GASIN"] = new(40, 560, 200, 624),
        ["BLR"]   = new(290, 560, 450, 624),
        ["HEATA"] = new(880, 508, 1060, 572),
        ["DRY"]   = new(880, 604, 1060, 668),
    };

    static readonly SKRect ElecBus  = new(560, 118, 572, 442);   // 構内母線
    static readonly SKRect SteamHdr = new(700, 500, 712, 700);   // 蒸気ヘッダ

    // ---- 配管 / 電力ライン定義 ----
    static readonly Pipe[] Pipes =
    {
        // 電力（黄）
        new(Theme.Electric, 5, new[]{ P(200,152), P(290,152) }, s => s.GridKw, 2000),
        new(Theme.Electric, 5, new[]{ P(450,152), P(562,152) }, s => s.GridKw, 2000),
        new(Theme.Electric, 4, new[]{ P(450,260), P(562,260) }, s => s.PvKw, 350),
        new(Theme.Electric, 4, new[]{ P(450,368), P(562,368) }, s => s.CgsKw, 500),
        new(Theme.Electric, 4, new[]{ P(570,152), P(880,152) }, s => s.LineAKw, 800),
        new(Theme.Electric, 4, new[]{ P(570,260), P(880,260) }, s => s.LineBKw, 650),
        new(Theme.Electric, 4, new[]{ P(570,368), P(880,368) }, s => s.UtilKw, 600),

        // 都市ガス（青）
        new(Theme.Gas, 5, new[]{ P(200,592), P(245,592) }, s => s.GasTotal, 700),
        new(Theme.Gas, 4, new[]{ P(245,592), P(245,378), P(290,378) },
            s => s.GasCgs, 150, "m³/h", P(245,480)),
        new(Theme.Gas, 4, new[]{ P(245,592), P(290,592) }, s => s.GasBlr, 550),

        // 蒸気（橙）
        new(Theme.Steam, 5, new[]{ P(450,592), P(700,592) },
            s => s.SteamBlr, 8, "t/h", P(565,576), "F1"),
        new(Theme.Steam, 4, new[]{ P(410,400), P(410,530), P(700,530) },
            s => s.SteamCgs, 2, "t/h", P(480,514), "F1"),
        new(Theme.Steam, 4, new[]{ P(712,540), P(880,540) }, s => s.SteamHeatA, 5),
        new(Theme.Steam, 4, new[]{ P(712,636), P(880,636) }, s => s.SteamDry, 3),

        // 復水回収（シアン）→ 給水タンク T-1 へ
        new(Theme.Condensate, 4,
            new[]{ P(1060,636), P(1110,636), P(1110,830), P(660,830), P(660,760), P(620,760) },
            s => s.CondensateFlow, 5, "t/h", P(885,812), "F1"),

        // 給水（T-1 → ポンプ P-1 → ボイラ）/ 補給水（T-2 → P-2 → T-1）
        new(Theme.Condensate, 4, new[]{ P(500,735), P(330,735), P(330,624) }, s => s.FeedwaterFlow, 8),
        new(Theme.Condensate, 4, new[]{ P(150,790), P(500,790) }, s => s.MakeupFlow, 4),

        // 燃焼空気（FD ファン → ボイラ）
        new(Theme.Air, 5, new[]{ P(415,662), P(415,624) }, s => s.AirFlow, 5200),

        // CGS 冷却水（往き / 戻り）
        new(Theme.CoolWater, 4, new[]{ P(340,400), P(340,455) }, s => s.CoolingFlow, 50),
        new(Theme.CoolWater, 4, new[]{ P(375,455), P(375,400) }, s => s.CoolingFlow, 50),
    };

    public void Render(SKCanvas canvas, SKImageInfo info, PlantSimulation sim, double t, string? hoverId)
    {
        canvas.Clear(Theme.Bg);

        _scale = Math.Min(info.Width / VW, info.Height / VH);
        _ox = (info.Width - VW * _scale) / 2f;
        _oy = (info.Height - VH * _scale) / 2f;

        canvas.Save();
        canvas.Translate(_ox, _oy);
        canvas.Scale(_scale);

        DrawBackdrop(canvas);
        foreach (var p in Pipes) DrawPipe(canvas, p, sim, t);
        DrawBuses(canvas, sim);
        DrawProcessElements(canvas, sim, t);
        foreach (var id in Boxes.Keys) DrawEquipBox(canvas, id, sim, t, hoverId == id);
        DrawHeader(canvas, sim, t);
        DrawSidePanel(canvas, sim, t);
        DrawLegend(canvas);

        canvas.Restore();
    }

    /// <summary>デバイス座標 → 機器 ID（操作可能なもののみ）。</summary>
    public string? HitTest(SKPoint device, PlantSimulation sim)
    {
        if (_scale <= 0) return null;
        var v = new SKPoint((device.X - _ox) / _scale, (device.Y - _oy) / _scale);
        foreach (var (id, r) in Boxes)
            if (r.Contains(v.X, v.Y) && sim.Equip[id].CanToggle) return id;
        return null;
    }

    // ================= 描画パーツ =================

    void DrawBackdrop(SKCanvas c)
    {
        for (float x = 40; x < 1170; x += 40)
            for (float y = 100; y < 880; y += 40)
                c.DrawCircle(x, y, 1.3f, Fill(Theme.GridDot));

        Text(c, "電 力 系 統", 40, 104, 15, Theme.TextDim, bold: true);
        Text(c, "ガ ス ・ 蒸 気 系 統", 40, 544, 15, Theme.TextDim, bold: true);
    }

    void DrawPipe(SKCanvas c, Pipe p, PlantSimulation sim, double t)
    {
        using var path = new SKPath();
        path.MoveTo(p.Pts[0]);
        for (int i = 1; i < p.Pts.Length; i++) path.LineTo(p.Pts[i]);

        double flow = p.Flow(sim);
        double norm = Math.Clamp(flow / p.Nominal, 0, 1.3);
        bool flowing = norm > 0.03;

        // 外殻 → 本体 → 流れの順に重ね描き
        c.DrawPath(path, Stroke(Theme.PipeCasing, p.Width + 6, SKStrokeCap.Round));
        c.DrawPath(path, Stroke(p.Color.WithAlpha(flowing ? (byte)110 : (byte)45), p.Width, SKStrokeCap.Round));

        if (flowing)
        {
            float speed = (float)(30 + 70 * norm);
            float phase = (float)(-((t * speed) % 30.0));
            using var dash = SKPathEffect.CreateDash(new float[] { 13, 17 }, phase);
            var s = Stroke(p.Color, p.Width, SKStrokeCap.Round);
            s.PathEffect = dash;
            c.DrawPath(path, s);
            s.PathEffect = null;
        }

        DrawArrow(c, p.Pts[^2], p.Pts[^1], p.Color.WithAlpha(flowing ? (byte)255 : (byte)70));

        if (p.Unit is { } unit && p.LabelPos is { } lp)
            Label(c, $"{flow.ToString(p.Format)} {unit}", lp.X, lp.Y, p.Color);
    }

    void DrawArrow(SKCanvas c, SKPoint from, SKPoint to, SKColor color)
    {
        float dx = to.X - from.X, dy = to.Y - from.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1) return;
        dx /= len; dy /= len;
        const float s = 9f;

        using var path = new SKPath();
        path.MoveTo(to);
        path.LineTo(to.X - dx * s - dy * s * 0.55f, to.Y - dy * s + dx * s * 0.55f);
        path.LineTo(to.X - dx * s + dy * s * 0.55f, to.Y - dy * s - dx * s * 0.55f);
        path.Close();
        c.DrawPath(path, Fill(color));
    }

    void DrawBuses(SKCanvas c, PlantSimulation sim)
    {
        bool energized = sim.GridKw > 1 || sim.OnSiteGenKw > 1;
        var busCol = energized ? Theme.Electric : Theme.StateStop;
        c.DrawRect(ElecBus, Fill(busCol.WithAlpha(230)));
        Text(c, "構内母線 6.6kV", 566, 110, 13, Theme.TextDim, SKTextAlign.Center);

        c.DrawRect(SteamHdr, Fill(Theme.Steam.WithAlpha(230)));
        Text(c, $"蒸気ヘッダ {sim.SteamPressure:F2} MPa", 706, 492, 13, Theme.TextDim, SKTextAlign.Center);
    }

    void DrawEquipBox(SKCanvas c, string id, PlantSimulation sim, double t, bool hover)
    {
        var r = Boxes[id];
        var eq = sim.Equip[id];

        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(r.Left, r.Top), new SKPoint(r.Left, r.Bottom),
            new[] { Theme.BoxTop, Theme.BoxBottom }, null, SKShaderTileMode.Clamp);
        _fill.Color = SKColors.White;
        _fill.Shader = shader;
        c.DrawRoundRect(r, 8, 8, _fill);
        _fill.Shader = null;

        c.DrawRoundRect(r, 8, 8, Stroke(hover ? Theme.Accent : Theme.BoxBorder, hover ? 2.2f : 1.3f));

        bool blink = (t % 0.9) < 0.5;
        var (lampCol, stateTxt) = eq.State switch
        {
            EquipState.Running => (Theme.StateRun, "運転"),
            EquipState.Stopped => (Theme.StateStop, "停止"),
            _ => (blink ? Theme.StateAlarm : Theme.StateAlarm.WithAlpha(60), "異常"),
        };
        c.DrawCircle(r.Left + 16, r.Top + 17, 6, Fill(lampCol));
        c.DrawCircle(r.Left + 16, r.Top + 17, 6, Stroke(SKColors.Black.WithAlpha(80), 1));

        Text(c, eq.Name, r.Left + 30, r.Top + 22, 14.5f, Theme.TextMain, bold: true);
        var stCol = eq.State == EquipState.Alarm ? Theme.StateAlarm : Theme.TextDim;
        Text(c, stateTxt, r.Right - 10, r.Top + 22, 12, stCol, SKTextAlign.Right);

        var (val, unit) = ValueOf(id, sim);
        Text(c, val, r.Right - 46, r.Bottom - 12, 22, ValueColor(id), SKTextAlign.Right, bold: true);
        Text(c, unit, r.Right - 10, r.Bottom - 12, 12.5f, Theme.TextDim, SKTextAlign.Right);
    }

    static (string, string) ValueOf(string id, PlantSimulation s) => id switch
    {
        "GRID"  => (s.GridKw.ToString("N0"), "kW"),
        "TR1"   => ((s.GridKw / PlantSimulation.ContractKw * 100).ToString("N0"), "%"),
        "PV"    => (s.PvKw.ToString("N0"), "kW"),
        "CGS"   => (s.CgsKw.ToString("N0"), "kW"),
        "LINEA" => (s.LineAKw.ToString("N0"), "kW"),
        "LINEB" => (s.LineBKw.ToString("N0"), "kW"),
        "UTIL"  => (s.UtilKw.ToString("N0"), "kW"),
        "GASIN" => (s.GasTotal.ToString("N0"), "m³/h"),
        "BLR"   => (s.SteamBlr.ToString("F1"), "t/h"),
        "HEATA" => (s.SteamHeatA.ToString("F1"), "t/h"),
        "DRY"   => (s.SteamDry.ToString("F1"), "t/h"),
        _ => ("--", ""),
    };

    static SKColor ValueColor(string id) => id switch
    {
        "GASIN" => Theme.Gas,
        "BLR" or "HEATA" or "DRY" => Theme.Steam,
        "PV" or "CGS" => Theme.GenGreen,
        "TR1" => Theme.TextMain,
        _ => Theme.Electric,
    };

    void DrawLegend(SKCanvas c)
    {
        (string Name, SKColor Col)[] items =
        {
            ("電力", Theme.Electric), ("都市ガス", Theme.Gas), ("蒸気", Theme.Steam),
            ("給水・復水", Theme.Condensate), ("冷却水", Theme.CoolWater), ("空気", Theme.Air),
        };
        float x = 44;
        foreach (var (name, col) in items)
        {
            c.DrawLine(x, 856, x + 26, 856, Stroke(col, 4, SKStrokeCap.Round));
            Text(c, name, x + 32, 861, 13, Theme.TextDim);
            x += 32 + name.Length * 13.5f + 26;
        }
        Text(c, "機器ボックスをクリックすると起動 / 停止を切替できます",
            x + 14, 861, 12.5f, Theme.TextDim.WithAlpha(170));
    }

    // ================= ペイント / テキスト ヘルパ =================

    SKPaint Fill(SKColor c)
    {
        _fill.Color = c;
        _fill.Shader = null;
        return _fill;
    }

    SKPaint Stroke(SKColor c, float w, SKStrokeCap cap = SKStrokeCap.Butt)
    {
        _stroke.Color = c;
        _stroke.StrokeWidth = w;
        _stroke.StrokeCap = cap;
        _stroke.StrokeJoin = SKStrokeJoin.Round;
        _stroke.PathEffect = null;
        return _stroke;
    }

    void Text(SKCanvas c, string s, float x, float y, float size, SKColor color,
              SKTextAlign align = SKTextAlign.Left, bool bold = false)
    {
        _text.Color = color;
        _text.TextSize = size;
        _text.TextAlign = align;
        _text.Typeface = bold ? Theme.JpBold : Theme.Jp;
        c.DrawText(s, x, y, _text);
    }

    /// <summary>配管上の流量バッジ。</summary>
    void Label(SKCanvas c, string s, float cx, float cy, SKColor color)
    {
        _text.TextSize = 12.5f;
        _text.Typeface = Theme.JpBold;
        float w = _text.MeasureText(s);
        var r = new SKRect(cx - w / 2 - 7, cy - 10, cx + w / 2 + 7, cy + 9);
        c.DrawRoundRect(r, 5, 5, Fill(Theme.BadgeBg.WithAlpha(235)));
        c.DrawRoundRect(r, 5, 5, Stroke(color.WithAlpha(90), 1));
        Text(c, s, cx, cy + 5, 12.5f, color, SKTextAlign.Center, bold: true);
    }
}

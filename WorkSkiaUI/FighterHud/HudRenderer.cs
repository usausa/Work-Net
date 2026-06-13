using SkiaSharp;

namespace FighterHud;

/// <summary>
/// Draws the whole HUD with SkiaSharp. All geometry is anchored to the window
/// edges / center and scaled by <c>_s</c> so the layout survives resizing.
/// </summary>
public sealed class HudRenderer : IDisposable
{
    private static readonly SKColor Bg = new(0x03, 0x08, 0x0C);
    private static readonly SKColor Main = new(0x46, 0xF1, 0xC8);
    private static readonly SKColor Bright = new(0xDB, 0xFF, 0xF4);
    private static readonly SKColor Amber = new(0xFF, 0xB4, 0x3E);
    private static readonly SKColor Red = new(0xFF, 0x55, 0x55);
    private static readonly SKColor Blue = new(0x58, 0xB6, 0xFF);
    private static readonly SKColor Panel = new(0x05, 0x10, 0x14, 0xB4);

    private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
    private readonly SKPaint _under = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
    private readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _text = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKTypeface _mono = SKTypeface.FromFamilyName("Consolas");
    private readonly SKTypeface _monoBold = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);
    private readonly SKPathEffect _dash = SKPathEffect.CreateDash(new float[] { 10f, 7f }, 0f);
    private readonly SKPath _path = new();
    private readonly Random _rng = new();

    private SKPath? _hexGrid;
    private int _hexW, _hexH;
    private SKShader? _vignette;
    private int _vigW, _vigH;
    private SKPath? _sweepPath;
    private SKShader? _sweepShader;
    private float _sweepR;

    private float _t;          // sim time of the current frame
    private float _s;          // ui scale factor
    private float W, H, CX, CY;

    public void Render(SKCanvas c, int width, int height, FlightSim s)
    {
        W = width;
        H = height;
        CX = W / 2f;
        CY = H * 0.46f;
        _s = Math.Clamp(MathF.Min(W / 1560f, H / 980f), 0.5f, 1.7f);
        _t = s.Time;

        c.Clear(Bg);
        DrawBackdrop(c);
        DrawFrame(c);
        DrawPitchLadder(c, s);
        DrawRollScale(c, s);
        DrawFlightPathMarker(c, s);
        DrawHeadingTape(c, s);
        DrawSpeedTape(c, s);
        DrawAltitudeTape(c, s);
        DrawTargets(c, s);
        DrawRadar(c, s);
        DrawWeapons(c, s);
        DrawStatus(c, s);
        DrawWarnings(c, s);
        DrawCrtOverlay(c);
    }

    // ---------------------------------------------------------------- helpers

    private bool Blink(float hz) => _t * hz % 1f < 0.55f;

    private void Line(SKCanvas c, float x1, float y1, float x2, float y2, SKColor col, float wd = 1.6f)
    {
        _under.Color = col.WithAlpha(55);
        _under.StrokeWidth = (wd + 4.5f) * _s;
        c.DrawLine(x1, y1, x2, y2, _under);
        _stroke.Color = col;
        _stroke.StrokeWidth = wd * _s;
        _stroke.PathEffect = null;
        c.DrawLine(x1, y1, x2, y2, _stroke);
    }

    private void StrokePath(SKCanvas c, SKPath p, SKColor col, float wd = 1.6f, bool glow = true)
    {
        if (glow)
        {
            _under.Color = col.WithAlpha(50);
            _under.StrokeWidth = (wd + 4f) * _s;
            c.DrawPath(p, _under);
        }
        _stroke.Color = col;
        _stroke.StrokeWidth = wd * _s;
        _stroke.PathEffect = null;
        c.DrawPath(p, _stroke);
    }

    private void Text(SKCanvas c, string str, float x, float y, float size, SKColor col,
                      SKTextAlign align = SKTextAlign.Left, bool bold = false)
    {
        _text.Typeface = bold ? _monoBold : _mono;
        _text.TextSize = size * _s;
        _text.TextAlign = align;
        _text.Color = col;
        c.DrawText(str, x, y, _text);
    }

    /// <summary>Box with two chamfered corners, dark fill, glowing outline.</summary>
    private void DrawTechBox(SKCanvas c, float cx, float cy, float w, float h, string txt, float ts, SKColor txtCol)
    {
        float cut = 7f * _s;
        float l = cx - w / 2f, t = cy - h / 2f, r = cx + w / 2f, b = cy + h / 2f;
        _path.Reset();
        _path.MoveTo(l + cut, t);
        _path.LineTo(r, t);
        _path.LineTo(r, b - cut);
        _path.LineTo(r - cut, b);
        _path.LineTo(l, b);
        _path.LineTo(l, t + cut);
        _path.Close();
        _fill.Color = Bg.WithAlpha(215);
        c.DrawPath(_path, _fill);
        StrokePath(c, _path, Main);
        Text(c, txt, cx, cy + ts * _s * 0.35f, ts, txtCol, SKTextAlign.Center, bold: true);
    }

    private void DrawBracket(SKCanvas c, float x, float y, float r, SKColor col, float wd)
    {
        float seg = r * 0.55f;
        _path.Reset();
        _path.MoveTo(x - r + seg, y - r); _path.LineTo(x - r, y - r); _path.LineTo(x - r, y - r + seg);
        _path.MoveTo(x + r - seg, y - r); _path.LineTo(x + r, y - r); _path.LineTo(x + r, y - r + seg);
        _path.MoveTo(x + r, y + r - seg); _path.LineTo(x + r, y + r); _path.LineTo(x + r - seg, y + r);
        _path.MoveTo(x - r + seg, y + r); _path.LineTo(x - r, y + r); _path.LineTo(x - r, y + r - seg);
        StrokePath(c, _path, col, wd);
    }

    // ------------------------------------------------------------- background

    private void DrawBackdrop(SKCanvas c)
    {
        if (_hexGrid == null || _hexW != (int)W || _hexH != (int)H) BuildHexGrid();
        _stroke.Color = Main.WithAlpha(13);
        _stroke.StrokeWidth = 1f;
        _stroke.PathEffect = null;
        c.DrawPath(_hexGrid!, _stroke);

        // slow vertical refresh band
        float band = H * 0.16f;
        float y = _t * 0.05f % 1f * (H + band * 2f) - band;
        using var sh = SKShader.CreateLinearGradient(
            new SKPoint(0f, y), new SKPoint(0f, y + band),
            new[] { Main.WithAlpha(0), Main.WithAlpha(15), Main.WithAlpha(0) },
            null, SKShaderTileMode.Clamp);
        _fill.Shader = sh;
        _fill.Color = SKColors.White;
        c.DrawRect(0f, y, W, band, _fill);
        _fill.Shader = null;
    }

    private void BuildHexGrid()
    {
        _hexGrid?.Dispose();
        var p = new SKPath();
        float r = 34f * MathF.Max(_s, 0.8f);
        float dx = r * 1.7320508f;
        float dy = r * 1.5f;
        for (int row = -1; row * dy < H + r; row++)
        {
            float off = (row & 1) == 0 ? 0f : dx / 2f;
            float cy = row * dy;
            for (float x = -dx + off; x < W + dx; x += dx)
            {
                for (int i = 0; i <= 6; i++)
                {
                    float a = MathF.PI / 3f * i + MathF.PI / 6f;
                    float px = x + r * MathF.Cos(a);
                    float py = cy + r * MathF.Sin(a);
                    if (i == 0) p.MoveTo(px, py);
                    else p.LineTo(px, py);
                }
            }
        }
        _hexGrid = p;
        _hexW = (int)W;
        _hexH = (int)H;
    }

    private void DrawFrame(SKCanvas c)
    {
        float m = 16f * _s, len = 64f * _s;
        void Corner(float x, float y, float sx, float sy)
        {
            _path.Reset();
            _path.MoveTo(x + sx * len, y);
            _path.LineTo(x, y);
            _path.LineTo(x, y + sy * len);
            StrokePath(c, _path, Main.WithAlpha(170), 2.2f, glow: false);
        }
        Corner(m, m, 1f, 1f);
        Corner(W - m, m, -1f, 1f);
        Corner(m, H - m, 1f, -1f);
        Corner(W - m, H - m, -1f, -1f);

        // decorative arcs around the boresight
        float R = 300f * _s;
        var rect = SKRect.Create(CX - R, CY - R, R * 2f, R * 2f);
        _stroke.Color = Main.WithAlpha(60);
        _stroke.StrokeWidth = 1.5f * _s;
        _stroke.PathEffect = null;
        c.DrawArc(rect, 150f, 60f, false, _stroke);
        c.DrawArc(rect, -30f, 60f, false, _stroke);

        float R2 = R - 10f * _s;
        var rect2 = SKRect.Create(CX - R2, CY - R2, R2 * 2f, R2 * 2f);
        _stroke.Color = Main.WithAlpha(95);
        float rot = _t * 14f;
        for (int i = 0; i < 3; i++)
            c.DrawArc(rect2, rot + i * 120f, 26f, false, _stroke);
    }

    // ---------------------------------------------------------- center symbols

    private void DrawPitchLadder(SKCanvas c, FlightSim s)
    {
        float pxDeg = 13f * _s;
        c.Save();
        c.ClipRect(SKRect.Create(CX - 240f * _s, CY - 190f * _s, 480f * _s, 380f * _s));
        c.Translate(CX, CY);
        c.RotateDegrees(-s.RollDeg);

        for (int p = -30; p <= 30; p += 5)
        {
            float yy = (s.PitchDeg - p) * pxDeg;
            bool horizon = p == 0;
            float half = (horizon ? 210f : 84f) * _s;
            float gap = (horizon ? 58f : 40f) * _s;
            var col = Main.WithAlpha(horizon ? (byte)235 : (byte)170);

            if (horizon)
            {
                _under.Color = Main.WithAlpha(60);
                _under.StrokeWidth = 7f * _s;
                c.DrawLine(-half, yy, -gap, yy, _under);
                c.DrawLine(gap, yy, half, yy, _under);
            }

            _stroke.Color = col;
            _stroke.StrokeWidth = (horizon ? 2.2f : 1.5f) * _s;
            _stroke.PathEffect = p < 0 ? _dash : null;
            c.DrawLine(-half, yy, -gap, yy, _stroke);
            c.DrawLine(gap, yy, half, yy, _stroke);
            _stroke.PathEffect = null;

            if (!horizon)
            {
                float tick = 9f * _s * MathF.Sign(p);
                _stroke.StrokeWidth = 1.5f * _s;
                c.DrawLine(-gap, yy, -gap, yy + tick, _stroke);
                c.DrawLine(gap, yy, gap, yy + tick, _stroke);
                Text(c, p.ToString(), -half - 8f * _s, yy + 4f * _s, 11f, col, SKTextAlign.Right);
                Text(c, p.ToString(), half + 8f * _s, yy + 4f * _s, 11f, col);
            }
        }
        c.Restore();
    }

    private void DrawRollScale(SKCanvas c, FlightSim s)
    {
        float R = 232f * _s;
        foreach (int a in new[] { -45, -30, -20, -10, 0, 10, 20, 30, 45 })
        {
            float ar = a * MathF.PI / 180f;
            float dx = MathF.Sin(ar), dy = MathF.Cos(ar);
            float len = (a % 30 == 0 ? 12f : 7f) * _s;
            Line(c, CX + dx * R, CY + dy * R, CX + dx * (R + len), CY + dy * (R + len), Main.WithAlpha(150), 1.4f);
        }

        float pr = -s.RollDeg * MathF.PI / 180f;
        float px = MathF.Sin(pr), py = MathF.Cos(pr);
        float bx = CX + px * (R - 4f * _s), by = CY + py * (R - 4f * _s);
        _path.Reset();
        _path.MoveTo(bx, by);
        _path.LineTo(bx - py * 6f * _s - px * 12f * _s, by + px * 6f * _s - py * 12f * _s);
        _path.LineTo(bx + py * 6f * _s - px * 12f * _s, by - px * 6f * _s - py * 12f * _s);
        _path.Close();
        _fill.Color = Main;
        c.DrawPath(_path, _fill);
    }

    private void DrawFlightPathMarker(SKCanvas c, FlightSim s)
    {
        float x = CX + MathF.Sin(_t * 0.40f) * 6f * _s;
        float y = CY + MathF.Cos(_t * 0.31f) * 5f * _s;
        float r = 11f * _s;

        _under.Color = Main.WithAlpha(70);
        _under.StrokeWidth = 6f * _s;
        c.DrawCircle(x, y, r, _under);
        _stroke.Color = Bright;
        _stroke.StrokeWidth = 2f * _s;
        _stroke.PathEffect = null;
        c.DrawCircle(x, y, r, _stroke);

        Line(c, x - r - 16f * _s, y, x - r, y, Bright, 2f);
        Line(c, x + r, y, x + r + 16f * _s, y, Bright, 2f);
        Line(c, x, y - r - 9f * _s, x, y - r, Bright, 2f);

        // fixed boresight cross
        Line(c, CX - 7f * _s, CY, CX + 7f * _s, CY, Main.WithAlpha(150), 1.4f);
        Line(c, CX, CY - 7f * _s, CX, CY + 7f * _s, Main.WithAlpha(150), 1.4f);
    }

    // ----------------------------------------------------------------- tapes

    private void DrawHeadingTape(SKCanvas c, FlightSim s)
    {
        float y = 64f * _s;
        float halfW = 270f * _s;
        float pxDeg = 7.4f * _s;
        float h = s.HeadingDeg;

        c.Save();
        c.ClipRect(SKRect.Create(CX - halfW, y - 30f * _s, halfW * 2f, 60f * _s));
        Line(c, CX - halfW, y, CX + halfW, y, Main.WithAlpha(200));

        int start = (int)MathF.Floor((h - halfW / pxDeg) / 5f) * 5;
        int end = (int)MathF.Ceiling((h + halfW / pxDeg) / 5f) * 5;
        for (int d = start; d <= end; d += 5)
        {
            float x = CX + (d - h) * pxDeg;
            int dd = (d % 360 + 360) % 360;
            bool major = dd % 10 == 0;
            Line(c, x, y, x, y - (major ? 12f : 7f) * _s, Main, major ? 1.8f : 1.2f);
            if (major)
            {
                bool cardinal = dd % 90 == 0;
                string label = dd switch { 0 => "N", 90 => "E", 180 => "S", 270 => "W", _ => (dd / 10).ToString("00") };
                Text(c, label, x, y - 17f * _s, 13f, cardinal ? Amber : Main, SKTextAlign.Center, bold: cardinal);
            }
        }
        c.Restore();

        _path.Reset();
        _path.MoveTo(CX, y + 4f * _s);
        _path.LineTo(CX - 7f * _s, y + 13f * _s);
        _path.LineTo(CX + 7f * _s, y + 13f * _s);
        _path.Close();
        _fill.Color = Main;
        c.DrawPath(_path, _fill);

        int hdg = ((int)MathF.Round(h) % 360 + 360) % 360;
        DrawTechBox(c, CX, y + 32f * _s, 86f * _s, 26f * _s, $"{hdg:000}°", 16f, Bright);
        Text(c, "HDG", CX - 52f * _s, y + 37f * _s, 11f, Main.WithAlpha(160), SKTextAlign.Right);
    }

    private void DrawSpeedTape(SKCanvas c, FlightSim s)
    {
        float x = CX - 415f * _s;
        float halfH = 190f * _s;
        float pxKt = 3.3f * _s;
        float v = s.SpeedKt;

        Text(c, "SPD KT", x, CY - halfH - 12f * _s, 12f, Main, SKTextAlign.Center, bold: true);

        c.Save();
        c.ClipRect(SKRect.Create(x - 70f * _s, CY - halfH, 110f * _s, halfH * 2f));
        Line(c, x, CY - halfH, x, CY + halfH, Main.WithAlpha(200));

        int lo = Math.Max(0, (int)MathF.Floor((v - halfH / pxKt) / 10f) * 10);
        int hi = (int)MathF.Ceiling((v + halfH / pxKt) / 10f) * 10;
        for (int k = lo; k <= hi; k += 10)
        {
            float y = CY + (v - k) * pxKt;
            bool major = k % 50 == 0;
            Line(c, x - (major ? 14f : 8f) * _s, y, x, y, Main, major ? 1.8f : 1.2f);
            if (major) Text(c, k.ToString(), x - 18f * _s, y + 4.5f * _s, 13f, Main, SKTextAlign.Right);
        }
        c.Restore();

        _path.Reset();
        _path.MoveTo(x - 2f * _s, CY);
        _path.LineTo(x - 12f * _s, CY - 7f * _s);
        _path.LineTo(x - 12f * _s, CY + 7f * _s);
        _path.Close();
        _fill.Color = Main;
        c.DrawPath(_path, _fill);
        DrawTechBox(c, x - 53f * _s, CY, 80f * _s, 30f * _s, ((int)MathF.Round(v)).ToString(), 17f, Bright);

        Text(c, $"M {s.Mach:0.00}", x, CY + halfH + 22f * _s, 13f, Bright, SKTextAlign.Center, bold: true);
        Text(c, $"AOA {s.AoaDeg:0.0}", x, CY + halfH + 40f * _s, 11f, Main.WithAlpha(170), SKTextAlign.Center);
    }

    private void DrawAltitudeTape(SKCanvas c, FlightSim s)
    {
        float x = CX + 415f * _s;
        float halfH = 190f * _s;
        float pxFt = 0.22f * _s;
        float v = s.AltitudeFt;

        Text(c, "ALT FT", x, CY - halfH - 12f * _s, 12f, Main, SKTextAlign.Center, bold: true);

        c.Save();
        c.ClipRect(SKRect.Create(x - 40f * _s, CY - halfH, 150f * _s, halfH * 2f));
        Line(c, x, CY - halfH, x, CY + halfH, Main.WithAlpha(200));

        int lo = Math.Max(0, (int)MathF.Floor((v - halfH / pxFt) / 100f) * 100);
        int hi = (int)MathF.Ceiling((v + halfH / pxFt) / 100f) * 100;
        for (int k = lo; k <= hi; k += 100)
        {
            float y = CY + (v - k) * pxFt;
            bool major = k % 500 == 0;
            Line(c, x, y, x + (major ? 14f : 8f) * _s, y, Main, major ? 1.8f : 1.2f);
            if (major) Text(c, k.ToString("N0"), x + 18f * _s, y + 4.5f * _s, 13f, Main);
        }
        c.Restore();

        _path.Reset();
        _path.MoveTo(x + 2f * _s, CY);
        _path.LineTo(x + 12f * _s, CY - 7f * _s);
        _path.LineTo(x + 12f * _s, CY + 7f * _s);
        _path.Close();
        _fill.Color = Main;
        c.DrawPath(_path, _fill);
        DrawTechBox(c, x + 62f * _s, CY, 100f * _s, 30f * _s, ((int)MathF.Round(v)).ToString("N0"), 16f, Bright);

        Text(c, $"VS {s.ClimbFpm:+0;-0}", x, CY + halfH + 22f * _s, 13f, s.ClimbFpm < -800f ? Amber : Bright, SKTextAlign.Center, bold: true);
        Text(c, "BARO 29.92", x, CY + halfH + 40f * _s, 11f, Main.WithAlpha(170), SKTextAlign.Center);
    }

    // ---------------------------------------------------------------- targets

    private void DrawTargets(SKCanvas c, FlightSim s)
    {
        float pxDeg = 13f * _s;
        foreach (var ct in s.Contacts)
        {
            float relB = FlightSim.AngDiff(ct.BearingDeg, s.HeadingDeg);
            float relE = ct.ElevationDeg(s.AltitudeFt) - s.PitchDeg;
            if (MathF.Abs(relB) > 24f || MathF.Abs(relE) > 15f)
            {
                if (ct == s.LockedTarget) DrawOffscreenCue(c, s, relB);
                continue;
            }

            float x = CX + relB * pxDeg;
            float y = CY - relE * pxDeg;
            if (ct == s.LockedTarget)
            {
                DrawLockBox(c, s, ct, x, y);
            }
            else
            {
                var col = ct.Iff switch { Iff.Hostile => Red, Iff.Friendly => Blue, _ => Amber };
                DrawBracket(c, x, y, 15f * _s, col, 1.6f);
                Text(c, ct.Name, x, y - 22f * _s, 11f, col, SKTextAlign.Center);
                Text(c, $"{ct.RangeNm:0.0}", x, y + 30f * _s, 11f, col.WithAlpha(190), SKTextAlign.Center);
            }
        }
    }

    private void DrawLockBox(SKCanvas c, FlightSim s, Contact ct, float x, float y)
    {
        bool steady = s.LockSteady;
        float r = (steady ? 26f : 32f + 4f * MathF.Sin(_t * 9f)) * _s;
        DrawBracket(c, x, y, r, Red, 2.2f);

        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(_t * 80f);
        float d = 10f * _s;
        _path.Reset();
        _path.MoveTo(0f, -d); _path.LineTo(d, 0f); _path.LineTo(0f, d); _path.LineTo(-d, 0f);
        _path.Close();
        StrokePath(c, _path, Red, 1.8f);
        c.Restore();

        float lx = x + r + 14f * _s, ly = y - r - 6f * _s;
        Line(c, x + r * 0.7f, y - r * 0.7f, lx, ly, Red.WithAlpha(180), 1.3f);
        Line(c, lx, ly, lx + 96f * _s, ly, Red.WithAlpha(180), 1.3f);
        Text(c, ct.Name, lx + 4f * _s, ly - 5f * _s, 12f, Red, bold: true);
        Text(c, $"RNG {ct.RangeNm,4:0.0}NM", lx + 4f * _s, ly + 13f * _s, 11f, Bright);
        Text(c, $"CLS {ct.ClosureKt,4:+0;-0}KT", lx + 4f * _s, ly + 27f * _s, 11f, Bright);
        Text(c, $"ALT {ct.AltitudeFt / 1000f,4:0.0}K", lx + 4f * _s, ly + 41f * _s, 11f, Bright);

        if (steady)
        {
            bool shoot = ct.RangeNm < 6f;
            if (Blink(shoot ? 6f : 2.5f))
                Text(c, shoot ? "SHOOT" : "LOCK ON", x, y + r + 22f * _s, 14f, shoot ? Red : Amber, SKTextAlign.Center, bold: true);
        }
        else
        {
            Text(c, "ACQ...", x, y + r + 22f * _s, 12f, Amber, SKTextAlign.Center);
        }

        // sparks around the target while the gun is firing
        if (s.GunFiring)
        {
            for (int i = 0; i < 4; i++)
            {
                float gx = x + ((float)_rng.NextDouble() * 2f - 1f) * 22f * _s;
                float gy = y + ((float)_rng.NextDouble() * 2f - 1f) * 22f * _s;
                _fill.Color = Bright.WithAlpha((byte)_rng.Next(120, 255));
                c.DrawCircle(gx, gy, 1.6f * _s, _fill);
            }
        }
    }

    private void DrawOffscreenCue(SKCanvas c, FlightSim s, float relB)
    {
        float side = MathF.Sign(relB);
        float x = CX + side * 296f * _s, y = CY;
        _path.Reset();
        _path.MoveTo(x, y - 10f * _s);
        _path.LineTo(x + side * 14f * _s, y);
        _path.LineTo(x, y + 10f * _s);
        StrokePath(c, _path, Red, 2f);
        if (s.LockedTarget != null)
            Text(c, $"TGT {FlightSim.Wrap360(s.LockedTarget.BearingDeg):000}", x - side * 6f * _s, y + 28f * _s, 11f, Red.WithAlpha(200), SKTextAlign.Center);
    }

    // ----------------------------------------------------------------- radar

    private void DrawRadar(SKCanvas c, FlightSim s)
    {
        float R = 128f * _s;
        float cx = 36f * _s + R;
        float cy = H - 40f * _s - R;

        _fill.Color = Panel;
        c.DrawCircle(cx, cy, R, _fill);
        _stroke.Color = Main;
        _stroke.StrokeWidth = 1.8f * _s;
        _stroke.PathEffect = null;
        c.DrawCircle(cx, cy, R, _stroke);
        _stroke.Color = Main.WithAlpha(90);
        _stroke.StrokeWidth = 1f * _s;
        c.DrawCircle(cx, cy, R + 5f * _s, _stroke);

        _stroke.Color = Main.WithAlpha(70);
        c.DrawCircle(cx, cy, R / 3f, _stroke);
        c.DrawCircle(cx, cy, R * 2f / 3f, _stroke);
        c.DrawLine(cx - R, cy, cx + R, cy, _stroke);
        c.DrawLine(cx, cy - R, cx, cy + R, _stroke);

        for (int a = 0; a < 360; a += 30)
        {
            float ar = a * MathF.PI / 180f;
            float dx = MathF.Sin(ar), dy = -MathF.Cos(ar);
            Line(c, cx + dx * (R - 6f * _s), cy + dy * (R - 6f * _s), cx + dx * R, cy + dy * R, Main.WithAlpha(140), 1.2f);
        }

        // rotating sweep with a fading trail (heading-up display)
        if (_sweepPath == null || MathF.Abs(_sweepR - R) > 0.5f)
        {
            _sweepPath?.Dispose();
            _sweepShader?.Dispose();
            _sweepPath = new SKPath();
            _sweepPath.MoveTo(0f, 0f);
            _sweepPath.ArcTo(SKRect.Create(-R, -R, R * 2f, R * 2f), 260f, 100f, false);
            _sweepPath.Close();
            _sweepShader = SKShader.CreateSweepGradient(
                new SKPoint(0f, 0f),
                new[] { Main.WithAlpha(0), Main.WithAlpha(0), Main.WithAlpha(115) },
                new[] { 0f, 0.72f, 1f });
            _sweepR = R;
        }
        c.Save();
        c.Translate(cx, cy);
        c.RotateDegrees(s.RadarSweepDeg - 90f);
        _fill.Shader = _sweepShader;
        _fill.Color = SKColors.White;
        c.DrawPath(_sweepPath, _fill);
        _fill.Shader = null;
        _stroke.Color = Main.WithAlpha(220);
        _stroke.StrokeWidth = 1.6f * _s;
        c.DrawLine(0f, 0f, R, 0f, _stroke);
        c.Restore();

        foreach (var ct in s.Contacts)
        {
            float disp = FlightSim.Wrap360(ct.BearingDeg - s.HeadingDeg);
            float dist = MathF.Min(ct.RangeNm / 40f, 0.94f) * R;
            float ar = disp * MathF.PI / 180f;
            float bx = cx + MathF.Sin(ar) * dist;
            float by = cy - MathF.Cos(ar) * dist;

            float since = FlightSim.Wrap360(s.RadarSweepDeg - disp);
            byte alpha = (byte)Math.Clamp(235f - since * 0.55f, 50f, 235f);
            var col = (ct.Iff switch { Iff.Hostile => Red, Iff.Friendly => Blue, _ => Amber }).WithAlpha(alpha);
            DrawBlip(c, bx, by, ct.Iff, col);

            if (ct == s.LockedTarget)
            {
                _stroke.Color = Red.WithAlpha(Blink(2.5f) ? (byte)230 : (byte)90);
                _stroke.StrokeWidth = 1.4f * _s;
                c.DrawCircle(bx, by, 9f * _s, _stroke);
            }
        }

        // own ship
        _path.Reset();
        _path.MoveTo(cx, cy - 7f * _s);
        _path.LineTo(cx + 5.5f * _s, cy + 6f * _s);
        _path.LineTo(cx, cy + 2.5f * _s);
        _path.LineTo(cx - 5.5f * _s, cy + 6f * _s);
        _path.Close();
        _fill.Color = Bright;
        c.DrawPath(_path, _fill);

        float na = FlightSim.Wrap360(-s.HeadingDeg) * MathF.PI / 180f;
        Text(c, "N", cx + MathF.Sin(na) * (R - 16f * _s), cy - MathF.Cos(na) * (R - 16f * _s) + 4f * _s, 11f, Amber, SKTextAlign.Center, bold: true);

        Text(c, "RDR // A-A", cx - R, cy - R - 16f * _s, 12f, Main, bold: true);
        Text(c, "RNG 40NM", cx + R, cy - R - 16f * _s, 12f, Main.WithAlpha(170), SKTextAlign.Right);
        Text(c, $"CONTACTS {s.Contacts.Count}   IFF ON", cx - R, cy + R + 20f * _s, 11f, Main.WithAlpha(150));
    }

    private void DrawBlip(SKCanvas c, float x, float y, Iff iff, SKColor col)
    {
        _fill.Color = col;
        switch (iff)
        {
            case Iff.Hostile:
                _path.Reset();
                _path.MoveTo(x, y - 5.5f * _s);
                _path.LineTo(x + 5f * _s, y + 4f * _s);
                _path.LineTo(x - 5f * _s, y + 4f * _s);
                _path.Close();
                c.DrawPath(_path, _fill);
                break;
            case Iff.Friendly:
                c.DrawCircle(x, y, 4f * _s, _fill);
                break;
            default:
                c.DrawRect(x - 3.5f * _s, y - 3.5f * _s, 7f * _s, 7f * _s, _fill);
                break;
        }
    }

    // ---------------------------------------------------------------- weapons

    private void DrawWeapons(SKCanvas c, FlightSim s)
    {
        float pw = 250f * _s, ph = 150f * _s;
        float x = W - 36f * _s - pw;
        float y = H - 40f * _s - ph;
        float cut = 14f * _s;

        _path.Reset();
        _path.MoveTo(x + cut, y);
        _path.LineTo(x + pw, y);
        _path.LineTo(x + pw, y + ph - cut);
        _path.LineTo(x + pw - cut, y + ph);
        _path.LineTo(x, y + ph);
        _path.LineTo(x, y + cut);
        _path.Close();
        _fill.Color = Panel;
        c.DrawPath(_path, _fill);
        StrokePath(c, _path, Main);

        float pad = 14f * _s;
        Text(c, "WPN", x + pad, y + 22f * _s, 13f, Main, bold: true);
        Text(c, "MASTER ARM: ON", x + pw - pad, y + 22f * _s, 11f, Amber, SKTextAlign.Right, bold: true);
        Line(c, x + pad, y + 30f * _s, x + pw - pad, y + 30f * _s, Main.WithAlpha(120), 1f);

        // gun row + segmented ammo bar
        float gy = y + 56f * _s;
        var gunCol = s.GunFiring ? (Blink(8f) ? Bright : Amber) : Main;
        Text(c, "GUN", x + pad, gy, 14f, gunCol, bold: true);
        Text(c, $"{s.GunAmmo:000}", x + pad + 110f * _s, gy, 16f, Bright, SKTextAlign.Right, bold: true);
        if (s.GunFiring) Text(c, "FIRING", x + pw - pad, gy, 11f, Red, SKTextAlign.Right, bold: true);

        float bw = pw - pad * 2f, bh = 9f * _s, by = gy + 10f * _s;
        const int cells = 10;
        float cw = (bw - (cells - 1) * 3f * _s) / cells;
        float frac = s.GunAmmo / (float)FlightSim.GunAmmoMax;
        for (int i = 0; i < cells; i++)
        {
            float cx = x + pad + i * (cw + 3f * _s);
            bool on = frac * cells > i + 0.5f || (frac * cells > i && Blink(6f));
            _fill.Color = on ? gunCol.WithAlpha(220) : Main.WithAlpha(40);
            c.DrawRect(cx, by, cw, bh, _fill);
        }

        // missile row
        float my = by + 38f * _s;
        Text(c, "AAM", x + pad, my, 14f, Main, bold: true);
        for (int i = 0; i < FlightSim.MissilesMax; i++)
            DrawMissileIcon(c, x + pad + 64f * _s + i * 28f * _s, my - 5f * _s, i < s.Missiles);

        float sy = my + 30f * _s;
        Text(c, "SEL AAM-4 [IR]", x + pad, sy, 12f, Bright);
        if (s.Fox2Timer > 0f && Blink(5f))
            Text(c, "FOX 2!", x + pw - pad, sy, 13f, Amber, SKTextAlign.Right, bold: true);
        else if (s.Missiles == 0)
            Text(c, "WPN OUT", x + pw - pad, sy, 12f, Red, SKTextAlign.Right, bold: Blink(2f));
    }

    private void DrawMissileIcon(SKCanvas c, float x, float y, bool live)
    {
        var col = live ? Main : Main.WithAlpha(45);
        float hh = 11f * _s, w = 3.2f * _s;
        _path.Reset();
        _path.MoveTo(x, y - hh);
        _path.LineTo(x + w, y - hh + 5f * _s);
        _path.LineTo(x + w, y + hh - 3f * _s);
        _path.LineTo(x + w + 2.5f * _s, y + hh);
        _path.LineTo(x - w - 2.5f * _s, y + hh);
        _path.LineTo(x - w, y + hh - 3f * _s);
        _path.LineTo(x - w, y - hh + 5f * _s);
        _path.Close();
        if (live)
        {
            _fill.Color = col.WithAlpha(70);
            c.DrawPath(_path, _fill);
        }
        StrokePath(c, _path, col, 1.3f, glow: live);
    }

    // ----------------------------------------------------------------- status

    private void DrawStatus(SKCanvas c, FlightSim s)
    {
        float m = 36f * _s;
        float lh = 17f * _s;
        float y = m + 14f * _s;

        Text(c, "MODE A/A COMBAT", m, y, 12f, Amber, bold: true);
        Text(c, "SYS  NOMINAL", m, y + lh, 12f, Main);
        Text(c, $"T+{TimeSpan.FromSeconds(s.Time):mm\\:ss}", m, y + lh * 2f, 12f, Main);
        Text(c, $"FUEL {s.FuelLbs,5:0} LBS", m, y + lh * 3f, 12f, s.FuelLbs < 2000f ? Amber : Main);

        Text(c, DateTime.Now.ToString("HH:mm:ss"), W - m, y, 12f, Main, SKTextAlign.Right, bold: true);
        Text(c, "DLNK ACTIVE", W - m, y + lh, 12f, Main, SKTextAlign.Right);
        Text(c, "ECM  STBY", W - m, y + lh * 2f, 12f, Main.WithAlpha(170), SKTextAlign.Right);
        Text(c, $"LOCK {(s.LockSteady ? s.LockedTarget!.Name : "----")}", W - m, y + lh * 3f, 12f,
             s.LockSteady ? Red : Main.WithAlpha(170), SKTextAlign.Right);

        float by = H - 34f * _s;
        Text(c, $"G {s.GForce:0.0}", CX - 90f * _s, by, 14f, Bright, SKTextAlign.Center, bold: true);
        Text(c, $"THR {s.ThrottlePct:0}%", CX + 90f * _s, by, 14f, Bright, SKTextAlign.Center, bold: true);
        Text(c, "TWS AUTO   CHAFF 24   FLARE 24   AP OFF", CX, H - 14f * _s, 10f, Main.WithAlpha(110), SKTextAlign.Center);
    }

    private void DrawWarnings(SKCanvas c, FlightSim s)
    {
        if (s.WarnTimer <= 0f || !Blink(4f)) return;
        Text(c, ">> MISSILE ALERT <<", CX, 132f * _s, 20f, Amber, SKTextAlign.Center, bold: true);
        _stroke.Color = Amber.WithAlpha(160);
        _stroke.StrokeWidth = 3f * _s;
        _stroke.PathEffect = null;
        c.DrawRect(8f * _s, 8f * _s, W - 16f * _s, H - 16f * _s, _stroke);
    }

    // ------------------------------------------------------------ crt overlay

    private void DrawCrtOverlay(SKCanvas c)
    {
        _fill.Color = new SKColor(0, 0, 0, 26);
        for (float y = 0f; y < H; y += 4f)
            c.DrawRect(0f, y, W, 1.4f, _fill);

        if (_vignette == null || _vigW != (int)W || _vigH != (int)H)
        {
            _vignette?.Dispose();
            _vignette = SKShader.CreateRadialGradient(
                new SKPoint(CX, H / 2f), MathF.Max(W, H) * 0.72f,
                new[] { SKColors.Transparent, new SKColor(0, 0, 0, 150) },
                new[] { 0.62f, 1f }, SKShaderTileMode.Clamp);
            _vigW = (int)W;
            _vigH = (int)H;
        }
        _fill.Shader = _vignette;
        _fill.Color = SKColors.White;
        c.DrawRect(0f, 0f, W, H, _fill);
        _fill.Shader = null;
    }

    public void Dispose()
    {
        _stroke.Dispose();
        _under.Dispose();
        _fill.Dispose();
        _text.Dispose();
        _mono.Dispose();
        _monoBold.Dispose();
        _dash.Dispose();
        _path.Dispose();
        _hexGrid?.Dispose();
        _vignette?.Dispose();
        _sweepPath?.Dispose();
        _sweepShader?.Dispose();
    }
}

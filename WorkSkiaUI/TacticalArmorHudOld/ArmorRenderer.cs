using SkiaSharp;

namespace TacticalArmorHud;

/// <summary>
/// Renders the tactical-armor cockpit HUD. The canvas is letterboxed to a 4:3
/// content rect, then translated + scaled so every draw call works in a fixed
/// 800x600 reference space. Stroke widths and text sizes are therefore in
/// reference pixels.
/// </summary>
public sealed class ArmorRenderer : IDisposable
{
    private const float RW = 800f, RH = 600f;
    private const float CX = 380f;   // center-view center (between the tapes)
    private const float CYV = 296f;

    private static readonly SKColor Bg = new(0x03, 0x0C, 0x11);
    private static readonly SKColor Cyan = new(0x4C, 0xD6, 0xE6);
    private static readonly SKColor CyanDim = new(0x2E, 0x8C, 0x9A);
    private static readonly SKColor Bright = new(0xD7, 0xF7, 0xFB);
    private static readonly SKColor Yellow = new(0xF1, 0xD9, 0x55);
    private static readonly SKColor Red = new(0xFF, 0x55, 0x4D);
    private static readonly SKColor Panel = new(0x05, 0x12, 0x18, 0xC0);

    private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
    private readonly SKPaint _under = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round };
    private readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _text = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKTypeface _mono = SKTypeface.FromFamilyName("Consolas");
    private readonly SKTypeface _monoB = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);
    private readonly SKTypeface _jp = SKTypeface.FromFamilyName("Yu Gothic UI");
    private readonly SKTypeface _jpB = SKTypeface.FromFamilyName("Yu Gothic UI", SKFontStyle.Bold);
    private readonly SKPathEffect _dash = SKPathEffect.CreateDash(new float[] { 7f, 5f }, 0f);
    private readonly SKPath _path = new();

    private SKPath? _mapContour;
    private int _mapKey;
    private SKShader? _vignette;
    private int _vigW, _vigH;

    private float _t;
    private float _s, _ox, _oy;

    public void Render(SKCanvas c, int width, int height, ArmorSim s)
    {
        _t = s.Time;
        _s = MathF.Max(0.0001f, MathF.Min(width / RW, height / RH));
        _ox = (width - RW * _s) / 2f;
        _oy = (height - RH * _s) / 2f;

        c.Clear(Bg);
        c.Save();
        c.Translate(_ox, _oy);
        c.Scale(_s);

        DrawBackdrop(c);
        DrawCenterView(c, s);
        DrawTapes(c, s);
        DrawHeadingStrip(c, s);
        DrawRadar(c, s);
        DrawMap(c, s);
        DrawComm(c, s);
        DrawDamagePanel(c, s);
        DrawArmWeapons(c, s);
        DrawFrame(c);
        DrawWarnings(c, s);
        DrawFox(c, s);
        DrawCrt(c);

        c.Restore();
    }

    // ---------------------------------------------------------------- helpers

    private bool Blink(float hz) => _t * hz % 1f < 0.55f;

    private static SKColor HpCol(float hp) => hp > 70f ? Cyan : hp > 35f ? Yellow : Red;

    private void Line(SKCanvas c, float x1, float y1, float x2, float y2, SKColor col, float w = 1.2f, bool glow = true)
    {
        if (glow)
        {
            _under.Color = col.WithAlpha(45);
            _under.StrokeWidth = w + 2.4f;
            _under.PathEffect = null;
            c.DrawLine(x1, y1, x2, y2, _under);
        }
        _stroke.Color = col;
        _stroke.StrokeWidth = w;
        _stroke.PathEffect = null;
        c.DrawLine(x1, y1, x2, y2, _stroke);
    }

    private void Stroke(SKCanvas c, SKPath p, SKColor col, float w = 1.2f, bool glow = true)
    {
        if (glow)
        {
            _under.Color = col.WithAlpha(40);
            _under.StrokeWidth = w + 2.2f;
            _under.PathEffect = null;
            c.DrawPath(p, _under);
        }
        _stroke.Color = col;
        _stroke.StrokeWidth = w;
        _stroke.PathEffect = null;
        c.DrawPath(p, _stroke);
    }

    private void T(SKCanvas c, string str, float x, float y, float size, SKColor col,
                   SKTextAlign align = SKTextAlign.Left, bool bold = false, bool jp = false)
    {
        _text.Typeface = jp ? (bold ? _jpB : _jp) : (bold ? _monoB : _mono);
        _text.TextSize = size;
        _text.TextAlign = align;
        _text.Color = col;
        c.DrawText(str, x, y, _text);
    }

    private void Rect(SKCanvas c, float x, float y, float w, float h, SKColor col, float sw = 1.1f, bool glow = false)
    {
        _path.Reset();
        _path.AddRect(SKRect.Create(x, y, w, h));
        Stroke(c, _path, col, sw, glow);
    }

    private void FillRect(SKCanvas c, float x, float y, float w, float h, SKColor col)
    {
        _fill.Color = col;
        _fill.Shader = null;
        c.DrawRect(x, y, w, h, _fill);
    }

    private void Corners(SKCanvas c, float x, float y, float w, float h, float len, SKColor col, float sw)
    {
        _path.Reset();
        _path.MoveTo(x + len, y); _path.LineTo(x, y); _path.LineTo(x, y + len);
        _path.MoveTo(x + w - len, y); _path.LineTo(x + w, y); _path.LineTo(x + w, y + len);
        _path.MoveTo(x + w, y + h - len); _path.LineTo(x + w, y + h); _path.LineTo(x + w - len, y + h);
        _path.MoveTo(x + len, y + h); _path.LineTo(x, y + h); _path.LineTo(x, y + h - len);
        Stroke(c, _path, col, sw, glow: false);
    }

    private void Chamfer(SKCanvas c, float x, float y, float w, float h, float cut, SKColor outline, SKColor? fill)
    {
        _path.Reset();
        _path.MoveTo(x + cut, y);
        _path.LineTo(x + w, y);
        _path.LineTo(x + w, y + h - cut);
        _path.LineTo(x + w - cut, y + h);
        _path.LineTo(x, y + h);
        _path.LineTo(x, y + cut);
        _path.Close();
        if (fill.HasValue) { _fill.Color = fill.Value; _fill.Shader = null; c.DrawPath(_path, _fill); }
        Stroke(c, _path, outline, 1.3f, glow: false);
    }

    // ------------------------------------------------------------- background

    private void DrawBackdrop(SKCanvas c)
    {
        _stroke.Color = Cyan.WithAlpha(10);
        _stroke.StrokeWidth = 0.7f;
        _stroke.PathEffect = null;
        for (float x = 0; x <= RW; x += 40f) c.DrawLine(x, 0, x, RH, _stroke);
        for (float y = 0; y <= RH; y += 40f) c.DrawLine(0, y, RW, y, _stroke);
    }

    private void DrawFrame(SKCanvas c)
    {
        float m = 9f, len = 30f;
        void Corner(float x, float y, float sx, float sy)
        {
            _path.Reset();
            _path.MoveTo(x + sx * len, y);
            _path.LineTo(x, y);
            _path.LineTo(x, y + sy * len);
            Stroke(c, _path, Cyan.WithAlpha(150), 1.4f, glow: false);
        }
        Corner(m, m, 1, 1);
        Corner(RW - m, m, -1, 1);
        Corner(m, RH - m, 1, -1);
        Corner(RW - m, RH - m, -1, -1);
    }

    // --------------------------------------------------- center external view

    private void DrawCenterView(SKCanvas c, ArmorSim s)
    {
        float vx = 266f, vy = 116f, vw = 228f, vh = 322f;
        c.Save();
        c.ClipRect(SKRect.Create(vx, vy, vw, vh));

        float horizon = CYV + s.PitchDeg * 4f;
        using (var sky = SKShader.CreateLinearGradient(
                   new SKPoint(0, vy), new SKPoint(0, horizon),
                   new[] { new SKColor(0x06, 0x1C, 0x28), new SKColor(0x04, 0x12, 0x1A) }, null, SKShaderTileMode.Clamp))
        {
            _fill.Shader = sky; _fill.Color = SKColors.White;
            c.DrawRect(vx, vy, vw, horizon - vy, _fill);
        }
        using (var gnd = SKShader.CreateLinearGradient(
                   new SKPoint(0, horizon), new SKPoint(0, vy + vh),
                   new[] { new SKColor(0x05, 0x16, 0x18), new SKColor(0x03, 0x0B, 0x0C) }, null, SKShaderTileMode.Clamp))
        {
            _fill.Shader = gnd; _fill.Color = SKColors.White;
            c.DrawRect(vx, horizon, vw, vy + vh - horizon, _fill);
        }
        _fill.Shader = null;

        // distant ridge line
        _path.Reset();
        _path.MoveTo(vx, horizon);
        for (float x = vx; x <= vx + vw; x += 16f)
            _path.LineTo(x, horizon - 6f - 7f * MathF.Sin(x * 0.05f) - 4f * MathF.Sin(x * 0.11f + 1f));
        _path.LineTo(vx + vw, horizon);
        _stroke.Color = CyanDim.WithAlpha(120);
        _stroke.StrokeWidth = 1f;
        _stroke.PathEffect = null;
        c.DrawPath(_path, _stroke);

        // yellow pitch ladder (rolls with the airframe)
        c.Save();
        c.Translate(CX, horizon);
        c.RotateDegrees(-s.RollDeg);
        for (int p = -10; p <= 10; p += 5)
        {
            float yy = (s.PitchDeg - p) * 6f - (horizon - CYV);
            bool zero = p == 0;
            float half = zero ? 96f : 54f;
            float gap = zero ? 30f : 26f;
            _stroke.Color = zero ? Yellow : Yellow.WithAlpha(150);
            _stroke.StrokeWidth = zero ? 1.7f : 1.2f;
            _stroke.PathEffect = zero ? null : _dash;
            c.DrawLine(-half, yy, -gap, yy, _stroke);
            c.DrawLine(gap, yy, half, yy, _stroke);
        }
        _stroke.PathEffect = null;
        c.Restore();

        // fixed boresight pipper
        _stroke.Color = Cyan.WithAlpha(200);
        _stroke.StrokeWidth = 1.2f;
        c.DrawCircle(CX, CYV, 4f, _stroke);
        Line(c, CX - 12f, CYV, CX - 5f, CYV, Cyan, 1.1f, glow: false);
        Line(c, CX + 5f, CYV, CX + 12f, CYV, Cyan, 1.1f, glow: false);
        Line(c, CX, CYV - 12f, CX, CYV - 5f, Cyan, 1.1f, glow: false);
        Line(c, CX, CYV + 5f, CX, CYV + 12f, Cyan, 1.1f, glow: false);

        c.Restore();
        Rect(c, vx, vy, vw, vh, Cyan.WithAlpha(60), 1f);
    }

    // ----------------------------------------------------------- center tapes

    private void DrawTapes(SKCanvas c, ArmorSim s)
    {
        float top = 122f, bot = 416f;
        float disp = s.AltM / 10f;
        float vmin = 358f, vmax = 387f;
        float Y(float v) => bot - (v - vmin) / (vmax - vmin) * (bot - top);

        // left tape (altitude)
        float lx = 258f;
        Line(c, lx, top - 6f, lx, bot + 6f, Cyan, 1.4f);
        for (int v = 360; v <= 385; v += 5)
        {
            float yy = Y(v);
            Line(c, lx, yy, lx + 13f, yy, Cyan, 1.3f, glow: false);
            T(c, v.ToString(), lx + 18f, yy + 3.5f, 11f, Bright);
        }
        for (int v = 360; v <= 385; v++)
            if (v % 5 != 0) { float yy = Y(v); Line(c, lx, yy, lx + 6f, yy, Cyan.WithAlpha(120), 1f, glow: false); }

        float cy = Y(disp);
        _fill.Color = Cyan.WithAlpha(70);
        _fill.Shader = null;
        c.DrawRect(lx - 10f, cy, 9f, bot - cy, _fill);
        _path.Reset();
        _path.MoveTo(lx - 1f, cy); _path.LineTo(lx - 12f, cy - 6f); _path.LineTo(lx - 12f, cy + 6f); _path.Close();
        _fill.Color = Bright;
        c.DrawPath(_path, _fill);

        // right tape (altitude grid + fine climb scale)
        float rx = 502f;
        Line(c, rx, top - 6f, rx, bot + 6f, Cyan, 1.4f);
        for (int v = 360; v <= 385; v += 5)
        {
            float yy = Y(v);
            Line(c, rx - 13f, yy, rx, yy, Cyan, 1.3f, glow: false);
            T(c, v.ToString(), rx - 18f, yy + 3.5f, 11f, Bright, SKTextAlign.Right);
        }
        float fineMid = (top + bot) / 2f, fineSpan = (bot - top) * 0.42f;
        float FineY(float val) => fineMid - val / 10f * fineSpan;
        foreach (int val in new[] { 10, 5, 0, -5, -10 })
        {
            float yy = FineY(val);
            Line(c, rx, yy, rx + 9f, yy, Cyan.WithAlpha(150), 1f, glow: false);
            if (val % 10 == 0) T(c, val == 0 ? "00" : val.ToString(), rx + 13f, yy + 3.5f, 10f, Cyan);
        }
        float climbY = FineY(s.ClimbRel);
        _path.Reset();
        _path.MoveTo(rx + 1f, climbY); _path.LineTo(rx + 10f, climbY - 5f); _path.LineTo(rx + 10f, climbY + 5f); _path.Close();
        _fill.Color = Yellow;
        c.DrawPath(_path, _fill);

        float racY = Y(disp);
        _path.Reset();
        _path.MoveTo(rx + 1f, racY); _path.LineTo(rx + 11f, racY - 5f); _path.LineTo(rx + 11f, racY + 5f); _path.Close();
        _fill.Color = Bright;
        c.DrawPath(_path, _fill);

        T(c, "ALT", lx + 6f, top - 12f, 9f, Cyan.WithAlpha(180));
        T(c, "VS", rx + 4f, top - 12f, 9f, Cyan.WithAlpha(180));
    }

    private void DrawHeadingStrip(SKCanvas c, ArmorSim s)
    {
        float y = 432f, halfW = 112f, pxDeg = 2.0f;
        float h = s.HeadingDeg;
        c.Save();
        c.ClipRect(SKRect.Create(CX - halfW, y - 26f, halfW * 2f, 42f));
        Line(c, CX - halfW, y, CX + halfW, y, Cyan, 1.3f);

        int start = (int)MathF.Floor((h - halfW / pxDeg) / 5f) * 5;
        int end = (int)MathF.Ceiling((h + halfW / pxDeg) / 5f) * 5;
        for (int d = start; d <= end; d += 5)
        {
            float x = CX + ArmorSim.AngDiff(d, h) * pxDeg;
            int dd = (d % 360 + 360) % 360;
            bool major = dd % 45 == 0;
            Line(c, x, y, x, y - (major ? 9f : 5f), Cyan, major ? 1.5f : 1f, glow: false);
            if (major)
            {
                string lbl = dd switch
                {
                    0 => "N", 45 => "NE", 90 => "E", 135 => "SE",
                    180 => "S", 225 => "SW", 270 => "W", 315 => "NW", _ => ""
                };
                T(c, lbl, x, y - 12f, 11f, dd % 90 == 0 ? Yellow : Cyan, SKTextAlign.Center, bold: dd % 90 == 0);
            }
        }
        c.Restore();

        _path.Reset();
        _path.MoveTo(CX, y + 3f); _path.LineTo(CX - 5f, y + 11f); _path.LineTo(CX + 5f, y + 11f); _path.Close();
        _fill.Color = Yellow;
        _fill.Shader = null;
        c.DrawPath(_path, _fill);
    }

    // ------------------------------------------------------- radar (top-down)

    private void DrawRadar(SKCanvas c, ArmorSim s)
    {
        float x0 = 14f, y0 = 46f, sz = 132f;
        float cx = x0 + sz / 2f, cy = y0 + sz / 2f;
        float R = 60f;

        _fill.Color = Panel; _fill.Shader = null;
        c.DrawRect(x0, y0, sz, sz, _fill);
        Rect(c, x0, y0, sz, sz, Cyan.WithAlpha(70), 1f);
        Corners(c, x0, y0, sz, sz, 12f, Cyan, 1.4f);

        c.Save();
        _path.Reset();
        _path.AddCircle(cx, cy, R);
        c.ClipPath(_path, antialias: true);

        // top-down concentric rings + crosshair
        _stroke.Color = Cyan.WithAlpha(95);
        _stroke.StrokeWidth = 1f;
        _stroke.PathEffect = null;
        c.DrawCircle(cx, cy, R, _stroke);
        c.DrawCircle(cx, cy, R * 2f / 3f, _stroke);
        c.DrawCircle(cx, cy, R / 3f, _stroke);
        _stroke.Color = Cyan.WithAlpha(70);
        c.DrawLine(cx - R, cy, cx + R, cy, _stroke);
        c.DrawLine(cx, cy - R, cx, cy + R, _stroke);

        // rotating sweep (true top-down, no perspective squash)
        c.Save();
        c.Translate(cx, cy);
        c.RotateDegrees(s.RadarSweepDeg);
        using (var sweep = SKShader.CreateSweepGradient(new SKPoint(0, 0),
                   new[] { Cyan.WithAlpha(0), Cyan.WithAlpha(0), Cyan.WithAlpha(110) }, new[] { 0f, 0.74f, 1f }))
        {
            _fill.Shader = sweep; _fill.Color = SKColors.White;
            _path.Reset();
            _path.MoveTo(0, 0);
            _path.ArcTo(SKRect.Create(-R, -R, R * 2, R * 2), 250f, 110f, false);
            _path.Close();
            c.DrawPath(_path, _fill);
            _fill.Shader = null;
        }
        _stroke.Color = Cyan.WithAlpha(220);
        _stroke.StrokeWidth = 1.3f;
        c.DrawLine(0, 0, R, 0, _stroke);
        c.Restore();

        // blips (heading-up, top-down)
        foreach (var ct in s.Contacts)
        {
            float rel = ArmorSim.AngDiff(ct.Bearing, s.HeadingDeg) * MathF.PI / 180f;
            float rr = MathF.Min(ct.Range / 2900f, 0.96f) * R;
            float bx = cx + MathF.Sin(rel) * rr;
            float by = cy - MathF.Cos(rel) * rr;
            float since = ArmorSim.Wrap360(s.RadarSweepDeg - ArmorSim.AngDiff(ct.Bearing, s.HeadingDeg));
            byte al = (byte)Math.Clamp(235f - since * 0.5f, 70f, 235f);
            var col = ct.Iff == Iff.Friend ? Cyan : Red;
            _fill.Color = col.WithAlpha(al);
            _fill.Shader = null;
            if (ct.Iff == Iff.Friend)
            {
                c.DrawCircle(bx, by, 3f, _fill);
            }
            else
            {
                _path.Reset();
                _path.MoveTo(bx, by - 4.5f); _path.LineTo(bx + 4f, by + 3.5f); _path.LineTo(bx - 4f, by + 3.5f); _path.Close();
                c.DrawPath(_path, _fill);
            }
        }
        c.Restore();

        // own chevron at center (points up = forward)
        _path.Reset();
        _path.MoveTo(cx, cy - 6f); _path.LineTo(cx + 5f, cy + 5f); _path.LineTo(cx, cy + 2f); _path.LineTo(cx - 5f, cy + 5f);
        _path.Close();
        _fill.Color = Bright;
        c.DrawPath(_path, _fill);

        // north marker (heading-up -> north rotates)
        float na = ArmorSim.Wrap360(-s.HeadingDeg) * MathF.PI / 180f;
        T(c, "N", cx + MathF.Sin(na) * (R - 9f), cy - MathF.Cos(na) * (R - 9f) + 3.5f, 10f, Yellow, SKTextAlign.Center, bold: true);

        T(c, "RADAR", x0 + 4f, y0 - 3f, 9f, Cyan, bold: true);
        T(c, "3.0KM", x0 + sz, y0 - 3f, 8f, Cyan.WithAlpha(160), SKTextAlign.Right);
    }

    // ------------------------------------------------------------- tactical map

    private void DrawMap(SKCanvas c, ArmorSim s)
    {
        float x0 = 12f, y0 = 184f, w = 222f, h = 168f;

        _fill.Color = new SKColor(0x04, 0x14, 0x10, 0xD0);
        _fill.Shader = null;
        c.DrawRect(x0, y0, w, h, _fill);
        Rect(c, x0, y0, w, h, Cyan, 1.3f);

        c.Save();
        c.ClipRect(SKRect.Create(x0, y0, w, h));

        if (_mapContour == null || _mapKey != (int)(w * 1000 + h)) BuildMapContour(w, h);
        c.Save();
        c.Translate(x0, y0);
        _stroke.Color = new SKColor(0x3A, 0x7A, 0x52, 0xAA);
        _stroke.StrokeWidth = 0.8f;
        _stroke.PathEffect = null;
        c.DrawPath(_mapContour!, _stroke);
        c.Restore();

        _stroke.Color = Cyan.WithAlpha(28);
        _stroke.StrokeWidth = 0.7f;
        for (float gx = x0; gx <= x0 + w; gx += w / 6f) c.DrawLine(gx, y0, gx, y0 + h, _stroke);
        for (float gy = y0; gy <= y0 + h; gy += h / 4f) c.DrawLine(x0, gy, x0 + w, gy, _stroke);

        // ridge road
        _path.Reset();
        _path.MoveTo(x0 + 10f, y0 + 60f);
        _path.CubicTo(x0 + 70f, y0 + 30f, x0 + 90f, y0 + 110f, x0 + 150f, y0 + 100f);
        _path.CubicTo(x0 + 190f, y0 + 92f, x0 + 200f, y0 + 150f, x0 + w - 6f, y0 + 140f);
        _stroke.Color = new SKColor(0x6F, 0xB8, 0x9A, 0xC8);
        _stroke.StrokeWidth = 1.2f;
        c.DrawPath(_path, _stroke);
        T(c, "旧連絡道 R-7", x0 + 22f, y0 + 20f, 8.5f, Cyan.WithAlpha(200), jp: true);
        T(c, "黒鉄峠", x0 + 150f, y0 + 122f, 8.5f, Cyan.WithAlpha(170), jp: true);

        // own unit (FELT-01)
        float ownX = x0 + w * 0.46f, ownY = y0 + h * 0.52f;
        float ping = _t % 2.2f / 2.2f;
        _stroke.Color = Bright.WithAlpha((byte)(110f * (1f - ping)));
        _stroke.StrokeWidth = 1f;
        c.DrawCircle(ownX, ownY, 4f + ping * 22f, _stroke);
        c.Save();
        c.Translate(ownX, ownY);
        c.RotateDegrees(s.HeadingDeg);
        _path.Reset();
        _path.MoveTo(0, -6f); _path.LineTo(4f, 5f); _path.LineTo(0, 2.5f); _path.LineTo(-4f, 5f); _path.Close();
        _fill.Color = Bright;
        c.DrawPath(_path, _fill);
        c.Restore();
        T(c, "FELT-01", ownX + 8f, ownY + 3f, 8f, Bright, bold: true);

        foreach (var m in s.Markers)
        {
            float px = x0 + Math.Clamp(m.MX(_t), 0.04f, 0.96f) * w;
            float py = y0 + Math.Clamp(m.MY(_t), 0.06f, 0.92f) * h;
            var col = m.Iff == Iff.Enemy ? Red : m.Iff == Iff.Friend ? Cyan : Yellow;
            if (m.Iff == Iff.Enemy)
            {
                _path.Reset();
                _path.MoveTo(px, py - 5f); _path.LineTo(px + 4.5f, py + 3.5f); _path.LineTo(px - 4.5f, py + 3.5f); _path.Close();
                _fill.Color = col;
                c.DrawPath(_path, _fill);
            }
            else
            {
                _path.Reset();
                _path.MoveTo(px - 5f, py - 4f); _path.LineTo(px, py + 4f); _path.LineTo(px + 5f, py - 4f);
                Stroke(c, _path, col, 1.3f, glow: false);
            }
            T(c, m.Code, px + 7f, py - 1f, 8f, col.WithAlpha(220));
            if (m.Sub.Length > 0) T(c, m.Sub, px + 7f, py + 8f, 8f, col.WithAlpha(180));
        }
        c.Restore();

        // speed readout box inside the map
        float sbx = x0 + w - 78f, sby = y0 + 8f;
        Chamfer(c, sbx, sby, 70f, 24f, 5f, Cyan.WithAlpha(170), new SKColor(0x03, 0x10, 0x14, 0xD0));
        T(c, $"{s.SpeedKmh:0000}km/h", sbx + 35f, sby + 11f, 9.5f, Bright, SKTextAlign.Center, bold: true);
        T(c, "SPEED", sbx + 35f, sby + 20f, 7f, Cyan.WithAlpha(170), SKTextAlign.Center);

        // header tab
        Chamfer(c, x0 + 2f, y0 + h - 20f, 100f, 16f, 4f, Cyan.WithAlpha(180), new SKColor(0x04, 0x16, 0x12, 0xE0));
        T(c, "MAP:", x0 + 8f, y0 + h - 8f, 9f, Cyan, bold: true);
        T(c, "黒鉄峠 周辺", x0 + 34f, y0 + h - 8f, 9f, Bright, jp: true);
    }

    private void BuildMapContour(float w, float h)
    {
        _mapContour?.Dispose();
        var p = new SKPath();
        const int NX = 40, NY = 34;
        var f = new float[NX + 1, NY + 1];
        for (int i = 0; i <= NX; i++)
            for (int j = 0; j <= NY; j++)
            {
                float u = i / (float)NX, v = j / (float)NY;
                f[i, j] = 0.5f * MathF.Sin(u * 6.1f + 1.3f) + 0.42f * MathF.Sin(v * 5.3f + 0.4f)
                          + 0.33f * MathF.Sin(u * 3.7f + v * 5.9f + 2f) + 0.2f * MathF.Sin(u * 8.2f + v * 2.3f);
            }
        float[] levels = { -0.5f, -0.18f, 0.16f, 0.5f };
        foreach (float lv in levels)
            for (int i = 0; i < NX; i++)
                for (int j = 0; j < NY; j++)
                {
                    float tl = f[i, j] - lv, tr = f[i + 1, j] - lv, br = f[i + 1, j + 1] - lv, bl = f[i, j + 1] - lv;
                    int idx = (tl > 0 ? 1 : 0) | (tr > 0 ? 2 : 0) | (br > 0 ? 4 : 0) | (bl > 0 ? 8 : 0);
                    if (idx == 0 || idx == 15) continue;
                    float x0 = i * w / NX, x1 = (i + 1) * w / NX, y0 = j * h / NY, y1 = (j + 1) * h / NY;
                    var T0 = new SKPoint(x0 + (x1 - x0) * (tl / (tl - tr)), y0);
                    var B0 = new SKPoint(x0 + (x1 - x0) * (bl / (bl - br)), y1);
                    var L0 = new SKPoint(x0, y0 + (y1 - y0) * (tl / (tl - bl)));
                    var R0 = new SKPoint(x1, y0 + (y1 - y0) * (tr / (tr - br)));
                    switch (idx)
                    {
                        case 1: case 14: p.MoveTo(L0); p.LineTo(T0); break;
                        case 2: case 13: p.MoveTo(T0); p.LineTo(R0); break;
                        case 3: case 12: p.MoveTo(L0); p.LineTo(R0); break;
                        case 4: case 11: p.MoveTo(R0); p.LineTo(B0); break;
                        case 6: case 9: p.MoveTo(T0); p.LineTo(B0); break;
                        case 7: case 8: p.MoveTo(L0); p.LineTo(B0); break;
                        case 5: p.MoveTo(L0); p.LineTo(T0); p.MoveTo(R0); p.LineTo(B0); break;
                        case 10: p.MoveTo(T0); p.LineTo(R0); p.MoveTo(L0); p.LineTo(B0); break;
                    }
                }
        _mapContour = p;
        _mapKey = (int)(w * 1000 + h);
    }

    // ----------------------------------------------------------------- comms

    private void DrawComm(SKCanvas c, ArmorSim s)
    {
        float y0 = 540f, h = 34f, xL = 12f, xR = 470f;

        // hexagonal ribbon (distinct from the reference's boxes)
        _path.Reset();
        _path.MoveTo(xL + 14f, y0);
        _path.LineTo(xR - 14f, y0);
        _path.LineTo(xR, y0 + h / 2f);
        _path.LineTo(xR - 14f, y0 + h);
        _path.LineTo(xL + 14f, y0 + h);
        _path.LineTo(xL, y0 + h / 2f);
        _path.Close();
        _fill.Color = Panel; _fill.Shader = null;
        c.DrawPath(_path, _fill);
        Stroke(c, _path, Cyan, 1.4f);

        // 通信 block
        T(c, "通信", xL + 42f, y0 + 14f, 13f, Bright, SKTextAlign.Center, bold: true, jp: true);
        T(c, "CONNECT", xL + 42f, y0 + 26f, 7f, Cyan.WithAlpha(190), SKTextAlign.Center);
        Line(c, xL + 78f, y0 + 5f, xL + 78f, y0 + h - 5f, Cyan.WithAlpha(110), 1f, glow: false);

        // channel
        T(c, "CH", xL + 90f, y0 + 13f, 8f, Cyan.WithAlpha(170));
        T(c, "OPEN", xL + 90f, y0 + 27f, 11f, s.CommTx ? Bright : Cyan, bold: true);
        Line(c, xL + 138f, y0 + 5f, xL + 138f, y0 + h - 5f, Cyan.WithAlpha(90), 1f, glow: false);

        // tx/rx + sender
        if (s.CommTx && Blink(4f)) { _fill.Color = Red; _fill.Shader = null; c.DrawCircle(xL + 150f, y0 + 11f, 3.4f, _fill); }
        else { _stroke.Color = Cyan.WithAlpha(140); _stroke.StrokeWidth = 1f; _stroke.PathEffect = null; c.DrawCircle(xL + 150f, y0 + 11f, 3.2f, _stroke); }
        T(c, s.CommTx ? "TX" : "RX", xL + 158f, y0 + 14f, 8.5f, s.CommTx ? Red : Cyan.WithAlpha(150), bold: true);
        if (s.CommTx) T(c, s.CommFrom, xL + 150f, y0 + 27f, 8f, Bright);

        // id capsules (rounded) — original ids
        void Cap(float x, string id)
        {
            var rr = SKRect.Create(x, y0 + 8f, 104f, 18f);
            _stroke.Color = Cyan.WithAlpha(150);
            _stroke.StrokeWidth = 1.1f;
            _stroke.PathEffect = null;
            c.DrawRoundRect(rr, 9f, 9f, _stroke);
            T(c, "ID", x + 8f, y0 + 21f, 8f, Cyan.WithAlpha(180));
            T(c, id, x + 98f, y0 + 21f, 10.5f, Bright, SKTextAlign.Right, bold: true);
        }
        Cap(xL + 196f, "41882");
        Cap(xL + 312f, "41875");
    }

    // ------------------------------------------------ airframe damage (blocks)

    private void DrawDamagePanel(SKCanvas c, ArmorSim s)
    {
        float px = 600f, py = 108f, pw = 192f, ph = 196f;

        T(c, "T.K.A Type-19", px + 12f, py + 14f, 12f, Cyan, bold: true);
        T(c, "UNIT  FELT-01", px + 12f, py + 27f, 8.5f, Cyan.WithAlpha(190));
        Line(c, px + 12f, py + 33f, px + pw - 8f, py + 33f, Cyan.WithAlpha(90), 1f, glow: false);

        float cx = px + 56f;
        SKRect[] r =
        {
            SKRect.Create(cx - 13f, py + 44f, 26f, 16f),   // HEAD
            SKRect.Create(cx - 23f, py + 64f, 46f, 46f),   // BODY
            SKRect.Create(cx - 45f, py + 66f, 17f, 42f),   // R.ARM (viewer left)
            SKRect.Create(cx + 28f, py + 66f, 17f, 42f),   // L.ARM
            SKRect.Create(cx - 21f, py + 112f, 17f, 48f),  // R.LEG
            SKRect.Create(cx + 4f,  py + 112f, 17f, 48f),  // L.LEG
            SKRect.Create(cx - 45f, py + 112f, 15f, 30f),  // R.JU (below R-arm, outboard of R-leg; shorter than legs)
            SKRect.Create(cx + 30f, py + 112f, 15f, 30f),  // L.JU (below L-arm, outboard of L-leg; shorter than legs)
        };

        for (int i = 0; i < r.Length; i++)
        {
            float hp = s.PartHp[i];
            var col = HpCol(hp);
            bool flash = s.ImpactTimer > 0f && s.ImpactPart == i && Blink(10f);
            bool crit = hp <= 35f;
            byte fa = flash ? (byte)220 : (crit && !Blink(2f)) ? (byte)20 : (byte)70;
            _fill.Color = (flash ? Bright : col).WithAlpha(fa);
            _fill.Shader = null;
            c.DrawRect(r[i], _fill);
            _stroke.Color = flash ? Bright : col;
            _stroke.StrokeWidth = 1.4f;
            _stroke.PathEffect = null;
            c.DrawRect(r[i], _stroke);
        }

        // per-part readout column
        float tx = px + 116f, ty = py + 50f;
        for (int i = 0; i < ArmorSim.PartTags.Length; i++)
        {
            float hp = s.PartHp[i];
            T(c, $"{ArmorSim.PartTags[i],-5} {hp,3:0}%", tx, ty + i * 14f, 8.5f, HpCol(hp), bold: hp <= 35f);
        }

        float integ = s.Integrity;
        var icol = integ > 60f ? Cyan : integ > 30f ? Yellow : Red;
        T(c, $"INTEGRITY {integ:0}%", px + 12f, py + ph - 4f, 9.5f, icol, bold: true);
    }

    private void DrawArmWeapons(SKCanvas c, ArmorSim s)
    {
        // Both arms share the same vertical position; each can select all 3 weapons.
        DrawArmSelector(c, 12f, 356f, 192f, 170f, "LEFT ARM", s.Left, s.ActiveArm == 0, s);
        DrawArmSelector(c, 600f, 356f, 192f, 170f, "RIGHT ARM", s.Right, s.ActiveArm == 1, s);
    }

    private void DrawArmSelector(SKCanvas c, float x, float y, float w, float h, string title,
                                 ArmWeapons arm, bool armActive, ArmorSim s)
    {
        _fill.Color = Panel; _fill.Shader = null;
        c.DrawRect(x, y, w, h, _fill);
        Rect(c, x, y, w, h, armActive ? Cyan : Cyan.WithAlpha(110), armActive ? 1.6f : 1.2f, glow: armActive);

        FillRect(c, x, y, w, 16f, (armActive ? Cyan : CyanDim).WithAlpha(36));
        T(c, title, x + 8f, y + 12f, 9.5f, armActive ? Bright : Cyan, bold: true);
        if (armActive)
            T(c, "● ACTIVE", x + w - 6f, y + 12f, 8f, s.SwapFlash > 0f && Blink(6f) ? Yellow : Cyan, SKTextAlign.Right, bold: true);

        // equipped weapon icon — remaining ammo drawn inside the image
        var eq = arm.Cur;
        DrawWeaponIcon(c, eq.Kind, eq.Ammo, eq.Max, x + w / 2f, y + 42f, armActive);

        // selectable weapon rows (original list style; non-equipped shown dimmed)
        float ry = y + 66f;
        for (int i = 0; i < arm.W.Length; i++)
        {
            bool equipped = i == arm.Sel;
            bool strike = equipped && armActive && arm.BladeSwing > 0f;
            DrawWeaponRow(c, x + 8f, ry + i * 32f, w - 16f, arm.W[i], equipped, armActive, equipped && arm.Reloading, strike, s);
        }
    }

    private void DrawWeaponRow(SKCanvas c, float x, float y, float w, Weapon wp,
                               bool equipped, bool armActive, bool reloading, bool strike, ArmorSim s)
    {
        if (equipped)
        {
            var hl = armActive ? Yellow : Cyan;
            FillRect(c, x, y, w, 24f, hl.WithAlpha(26));
            Rect(c, x, y, w, 24f, hl.WithAlpha(s.SwapFlash > 0f && armActive && Blink(6f) ? (byte)255 : (byte)170), 1.1f);
            _path.Reset();
            _path.MoveTo(x + 4f, y + 7f); _path.LineTo(x + 9f, y + 11f); _path.LineTo(x + 4f, y + 15f); _path.Close();
            _fill.Color = hl; _fill.Shader = null;
            c.DrawPath(_path, _fill);
        }

        var nameCol = equipped ? (armActive ? Bright : Cyan) : Cyan.WithAlpha(140);
        T(c, wp.Name, x + 13f, y + 11f, 9f, nameCol, bold: equipped, jp: true);

        if (equipped && reloading && Blink(4f))
            T(c, "RELOAD", x + w - 5f, y + 21f, 7.5f, Yellow, SKTextAlign.Right, bold: true);
        else
            T(c, wp.Model, x + w - 5f, y + 21f, 7f, Cyan.WithAlpha(equipped ? (byte)160 : (byte)100), SKTextAlign.Right);

        if (wp.Melee)
        {
            T(c, strike && Blink(8f) ? "STRIKE" : "C.Q.C", x + w - 5f, y + 11f, 10f,
              strike ? Yellow : equipped ? (armActive ? Yellow : Cyan) : Cyan.WithAlpha(150), SKTextAlign.Right, bold: equipped);
        }
        else
        {
            bool low = wp.Ammo <= wp.Max * 0.15f;
            var ac = !equipped ? Cyan.WithAlpha(140) : low ? Yellow : Bright;
            T(c, $"{wp.Ammo:0000}", x + w - 5f, y + 11f, 12f, ac, SKTextAlign.Right, bold: equipped);
            float gx = x + 13f, gw = w - 64f, gy = y + 15f;
            _stroke.Color = Cyan.WithAlpha(equipped ? (byte)120 : (byte)70); _stroke.StrokeWidth = 0.9f; _stroke.PathEffect = null;
            c.DrawRect(gx, gy, gw, 3.4f, _stroke);
            _fill.Color = (low ? Yellow : Cyan).WithAlpha(equipped ? (byte)200 : (byte)90); _fill.Shader = null;
            c.DrawRect(gx + 0.7f, gy + 0.7f, (gw - 1.4f) * Math.Clamp(wp.Ammo / (float)wp.Max, 0f, 1f), 2f, _fill);
        }
    }

    /// <summary>Static weapon icon with the remaining ammo shown inside the image.</summary>
    private void DrawWeaponIcon(SKCanvas c, WeaponKind kind, int ammo, int max, float cx, float cy, bool active)
    {
        var col = active ? Cyan.WithAlpha(235) : Cyan.WithAlpha(150);
        switch (kind)
        {
            case WeaponKind.Autocannon:
                _path.Reset();
                _path.AddRect(SKRect.Create(cx - 34f, cy - 3f, 26f, 6f));   // barrel
                _path.AddRect(SKRect.Create(cx - 8f, cy - 8f, 26f, 16f));   // body
                _path.AddRect(SKRect.Create(cx + 18f, cy - 5f, 12f, 7f));   // stock
                _path.AddRect(SKRect.Create(cx - 22f, cy + 4f, 6f, 9f));    // foregrip
                Stroke(c, _path, col, 1.3f);
                float mx = cx + 2f, my = cy + 8f, mw = 12f, mh = 16f;        // magazine = ammo gauge
                _stroke.Color = col; _stroke.StrokeWidth = 1.1f; _stroke.PathEffect = null;
                c.DrawRect(mx, my, mw, mh, _stroke);
                float fr = Math.Clamp(ammo / (float)max, 0f, 1f);
                _fill.Color = (fr < 0.2f ? Yellow : col).WithAlpha(180); _fill.Shader = null;
                c.DrawRect(mx + 1f, my + mh - mh * fr, mw - 2f, mh * fr, _fill);
                break;

            case WeaponKind.Missile:
                _stroke.Color = col; _stroke.StrokeWidth = 1.3f; _stroke.PathEffect = null;
                c.DrawRect(cx - 32f, cy - 9f, 64f, 18f, _stroke);
                for (int i = 0; i < max; i++)                                 // tubes = remaining (quad launcher)
                {
                    float tx = cx - 24f + i * 16f;
                    if (i < ammo) { _fill.Color = col.WithAlpha(200); _fill.Shader = null; c.DrawCircle(tx, cy, 4f, _fill); }
                    else { _stroke.Color = col.WithAlpha(70); _stroke.StrokeWidth = 1f; c.DrawCircle(tx, cy, 4f, _stroke); }
                }
                break;

            case WeaponKind.Blade:
                _path.Reset();
                _path.MoveTo(cx - 32f, cy); _path.LineTo(cx + 16f, cy - 5f); _path.LineTo(cx + 26f, cy); _path.LineTo(cx + 16f, cy + 5f); _path.Close();
                _path.AddRect(SKRect.Create(cx + 26f, cy - 4f, 12f, 8f));
                Stroke(c, _path, col, 1.4f);
                break;
        }
    }

    // -------------------------------------------------------- weapon diagram

    private void DrawFox(SKCanvas c, ArmorSim s)
    {
        // FOX call-out at the bottom-center when the active arm launches a missile
        var arm = s.ActiveArm == 0 ? s.Left : s.Right;
        if (arm.Cur.Kind == WeaponKind.Missile && s.FoxTimer > 0f && Blink(6f))
            T(c, "● FOX-2", CX, 466f, 18f, Yellow, SKTextAlign.Center, bold: true);
    }

    // ---------------------------------------------------------------- warnings

    private void DrawWarnings(SKCanvas c, ArmorSim s)
    {
        if (s.ImpactTimer > 0f)
        {
            _stroke.Color = Red.WithAlpha((byte)Math.Min(180f, s.ImpactTimer * 200f));
            _stroke.StrokeWidth = 4f;
            _stroke.PathEffect = null;
            c.DrawRect(5f, 5f, RW - 10f, RH - 10f, _stroke);
            if (Blink(8f) && s.ImpactPart >= 0)
                T(c, $"DAMAGE : {ArmorSim.PartTags[s.ImpactPart]}", CX, 168f, 14f, Red, SKTextAlign.Center, bold: true);
        }
        if (s.WarnTimer > 0f && Blink(4f))
        {
            T(c, ">> WARNING : HOSTILE LOCK <<", CX, 150f, 15f, Yellow, SKTextAlign.Center, bold: true);
            _stroke.Color = Yellow.WithAlpha(150);
            _stroke.StrokeWidth = 2f;
            _stroke.PathEffect = null;
            c.DrawRect(6f, 6f, RW - 12f, RH - 12f, _stroke);
        }
    }

    // ------------------------------------------------------------- crt overlay

    private void DrawCrt(SKCanvas c)
    {
        _fill.Color = new SKColor(0, 0, 0, 24);
        _fill.Shader = null;
        for (float y = 0; y < RH; y += 3f) c.DrawRect(0, y, RW, 1f, _fill);

        if (_vignette == null || _vigW != (int)RW || _vigH != (int)RH)
        {
            _vignette?.Dispose();
            _vignette = SKShader.CreateRadialGradient(
                new SKPoint(RW / 2f, RH / 2f), RW * 0.62f,
                new[] { SKColors.Transparent, new SKColor(0, 0, 0, 140) },
                new[] { 0.6f, 1f }, SKShaderTileMode.Clamp);
            _vigW = (int)RW;
            _vigH = (int)RH;
        }
        _fill.Shader = _vignette;
        _fill.Color = SKColors.White;
        c.DrawRect(0, 0, RW, RH, _fill);
        _fill.Shader = null;
    }

    public void Dispose()
    {
        _stroke.Dispose();
        _under.Dispose();
        _fill.Dispose();
        _text.Dispose();
        _mono.Dispose();
        _monoB.Dispose();
        _jp.Dispose();
        _jpB.Dispose();
        _dash.Dispose();
        _path.Dispose();
        _mapContour?.Dispose();
        _vignette?.Dispose();
    }
}

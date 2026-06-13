using SkiaSharp;

namespace MechHud;

/// <summary>
/// Draws the ground-mech HUD. Green phosphor palette; layout anchored to the
/// window edges / center and scaled by <c>_s</c>.
/// </summary>
public sealed class MechRenderer : IDisposable
{
    private static readonly SKColor Bg = new(0x04, 0x0A, 0x06);
    private static readonly SKColor Main = new(0x7D, 0xF3, 0x6B);
    private static readonly SKColor Bright = new(0xEA, 0xFF, 0xE6);
    private static readonly SKColor Amber = new(0xFF, 0xB4, 0x3E);
    private static readonly SKColor Red = new(0xFF, 0x50, 0x50);
    private static readonly SKColor Cyan = new(0x4F, 0xD8, 0xE8);
    private static readonly SKColor Panel = new(0x06, 0x12, 0x08, 0xB4);

    private readonly SKPaint _stroke = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
    private readonly SKPaint _under = new() { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round };
    private readonly SKPaint _fill = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKPaint _text = new() { IsAntialias = true, Style = SKPaintStyle.Fill };
    private readonly SKTypeface _mono = SKTypeface.FromFamilyName("Consolas");
    private readonly SKTypeface _monoBold = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold);
    private readonly SKPathEffect _dash = SKPathEffect.CreateDash(new float[] { 8f, 6f }, 0f);
    private readonly SKPath _path = new();
    private readonly Random _rng = new();

    private SKShader? _vignette;
    private int _vigW, _vigH;
    private SKPath? _sweepPath;
    private SKShader? _sweepShader;
    private float _sweepR;
    private SKPath? _terrain;
    private int _terrW;

    private float _t;
    private float _s;
    private float _bob;
    private float W, H, CX, CY;

    public void Render(SKCanvas c, int width, int height, MechSim s)
    {
        W = width;
        H = height;
        CX = W / 2f;
        CY = H * 0.42f;
        _s = Math.Clamp(MathF.Min(W / 1560f, H / 980f), 0.5f, 1.7f);
        _t = s.Time;

        float bobAmp = s.Mode switch { MoveMode.Walk => 3f, MoveMode.Dash => 5f, _ => 1.5f };
        _bob = MathF.Sin(_t * s.StepHz * MathF.Tau) * bobAmp * _s;

        c.Clear(Bg);
        DrawBackdrop(c);
        DrawFrame(c);
        DrawHorizon(c, s);
        DrawReticle(c, s);
        DrawTargets(c, s);
        DrawHeadingTape(c, s);
        DrawMotion(c, s);
        DrawWeaponPanelLeft(c, s);
        DrawWeaponPanelRight(c, s);
        DrawDamagePanel(c, s);
        DrawRadar(c, s);
        DrawTerrainMap(c, s);
        DrawCommPanel(c, s);
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

    private void PanelPath(float x, float y, float w, float h, float cut)
    {
        _path.Reset();
        _path.MoveTo(x + cut, y);
        _path.LineTo(x + w, y);
        _path.LineTo(x + w, y + h - cut);
        _path.LineTo(x + w - cut, y + h);
        _path.LineTo(x, y + h);
        _path.LineTo(x, y + cut);
        _path.Close();
    }

    private void DrawTechBox(SKCanvas c, float cx, float cy, float w, float h, string txt, float ts, SKColor txtCol)
    {
        PanelPath(cx - w / 2f, cy - h / 2f, w, h, 7f * _s);
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

    private void HexPath(float x, float y, float r)
    {
        _path.Reset();
        for (int i = 0; i < 6; i++)
        {
            float a = MathF.PI / 3f * i - MathF.PI / 6f;
            float px = x + r * MathF.Cos(a);
            float py = y + r * MathF.Sin(a);
            if (i == 0) _path.MoveTo(px, py);
            else _path.LineTo(px, py);
        }
        _path.Close();
    }

    // ------------------------------------------------------------- background

    private void DrawBackdrop(SKCanvas c)
    {
        float pitch = 46f * MathF.Max(_s, 0.8f);
        _stroke.Color = Main.WithAlpha(12);
        _stroke.StrokeWidth = 1f;
        _stroke.PathEffect = null;
        for (float x = 0f; x < W; x += pitch) c.DrawLine(x, 0f, x, H, _stroke);
        for (float y = 0f; y < H; y += pitch) c.DrawLine(0f, y, W, y, _stroke);

        float band = H * 0.16f;
        float by = _t * 0.05f % 1f * (H + band * 2f) - band;
        using var sh = SKShader.CreateLinearGradient(
            new SKPoint(0f, by), new SKPoint(0f, by + band),
            new[] { Main.WithAlpha(0), Main.WithAlpha(14), Main.WithAlpha(0) },
            null, SKShaderTileMode.Clamp);
        _fill.Shader = sh;
        _fill.Color = SKColors.White;
        c.DrawRect(0f, by, W, band, _fill);
        _fill.Shader = null;
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

        float R = 286f * _s;
        var rect = SKRect.Create(CX - R, CY - R, R * 2f, R * 2f);
        _stroke.Color = Main.WithAlpha(55);
        _stroke.StrokeWidth = 1.5f * _s;
        _stroke.PathEffect = null;
        c.DrawArc(rect, 150f, 60f, false, _stroke);
        c.DrawArc(rect, -30f, 60f, false, _stroke);

        float R2 = R - 10f * _s;
        var rect2 = SKRect.Create(CX - R2, CY - R2, R2 * 2f, R2 * 2f);
        _stroke.Color = Main.WithAlpha(90);
        float rot = _t * 11f;
        for (int i = 0; i < 3; i++)
            c.DrawArc(rect2, rot + i * 120f, 24f, false, _stroke);
    }

    // ----------------------------------------------------------- center vision

    private void DrawHorizon(SKCanvas c, MechSim s)
    {
        float pxDeg = 8f * _s;
        c.Save();
        c.ClipRect(SKRect.Create(CX - 190f * _s, CY - 120f * _s, 380f * _s, 240f * _s));
        c.Translate(CX, CY + _bob);
        c.RotateDegrees(-s.RollDeg);

        for (int p = -10; p <= 10; p += 10)
        {
            float yy = (s.PitchDeg - p) * pxDeg;
            bool horizon = p == 0;
            float half = (horizon ? 165f : 85f) * _s;
            float gap = (horizon ? 60f : 44f) * _s;
            var col = Main.WithAlpha(horizon ? (byte)220 : (byte)120);
            _stroke.Color = col;
            _stroke.StrokeWidth = (horizon ? 2f : 1.3f) * _s;
            _stroke.PathEffect = p < 0 ? _dash : null;
            c.DrawLine(-half, yy, -gap, yy, _stroke);
            c.DrawLine(gap, yy, half, yy, _stroke);
            _stroke.PathEffect = null;
        }
        c.Restore();
    }

    private void DrawReticle(SKCanvas c, MechSim s)
    {
        float x = CX;
        float y = CY + _bob * 0.6f;

        // tiny fixed cross
        Line(c, CX - 6f * _s, CY, CX + 6f * _s, CY, Main.WithAlpha(140), 1.3f);
        Line(c, CX, CY - 6f * _s, CX, CY + 6f * _s, Main.WithAlpha(140), 1.3f);

        if (s.ActiveSide == WeaponSide.Left)
        {
            // SMG: circle + rotating ticks + speed-dependent spread dots
            _under.Color = Main.WithAlpha(60);
            _under.StrokeWidth = 6f * _s;
            c.DrawCircle(x, y, 26f * _s, _under);
            _stroke.Color = Main.WithAlpha(210);
            _stroke.StrokeWidth = 1.8f * _s;
            _stroke.PathEffect = null;
            c.DrawCircle(x, y, 26f * _s, _stroke);

            c.Save();
            c.Translate(x, y);
            c.RotateDegrees(_t * 24f);
            for (int i = 0; i < 4; i++)
            {
                c.DrawLine(26f * _s, 0f, 34f * _s, 0f, _stroke);
                c.RotateDegrees(90f);
            }
            c.Restore();

            float sp = (8f + s.SpeedKmh * 0.10f + (s.SmgFiring ? 6f : 0f)) * _s;
            _fill.Color = Main;
            c.DrawCircle(x, y - 36f * _s - sp, 2.2f * _s, _fill);
            c.DrawCircle(x, y + 36f * _s + sp, 2.2f * _s, _fill);
            c.DrawCircle(x - 36f * _s - sp, y, 2.2f * _s, _fill);
            c.DrawCircle(x + 36f * _s + sp, y, 2.2f * _s, _fill);
            c.DrawCircle(x, y, 2f * _s, _fill);
        }
        else if (s.RightWeapon == RightArm.Cannon)
        {
            // long range cannon: fine cross + converging chevrons + charge arc
            Line(c, x - 42f * _s, y, x - 10f * _s, y, Main.WithAlpha(220), 1.5f);
            Line(c, x + 10f * _s, y, x + 42f * _s, y, Main.WithAlpha(220), 1.5f);
            Line(c, x, y - 42f * _s, x, y - 10f * _s, Main.WithAlpha(220), 1.5f);
            Line(c, x, y + 10f * _s, x, y + 42f * _s, Main.WithAlpha(220), 1.5f);

            float charge = s.CannonCharge;
            float conv = (12f + (1f - charge) * 22f) * _s;
            _path.Reset();
            _path.MoveTo(x - 42f * _s - conv, y - 7f * _s);
            _path.LineTo(x - 34f * _s - conv, y);
            _path.LineTo(x - 42f * _s - conv, y + 7f * _s);
            _path.MoveTo(x + 42f * _s + conv, y - 7f * _s);
            _path.LineTo(x + 34f * _s + conv, y);
            _path.LineTo(x + 42f * _s + conv, y + 7f * _s);
            StrokePath(c, _path, Main, 1.8f);

            var arcRect = SKRect.Create(x - 46f * _s, y - 46f * _s, 92f * _s, 92f * _s);
            _stroke.Color = Main.WithAlpha(70);
            _stroke.StrokeWidth = 3f * _s;
            c.DrawArc(arcRect, 30f, 120f, false, _stroke);
            if (charge > 0f)
            {
                _stroke.Color = charge >= 1f ? Amber : Main;
                c.DrawArc(arcRect, 90f - 60f * charge, 120f * charge, false, _stroke);
            }
            if (charge >= 1f && Blink(4f))
                Text(c, "RDY", x, y + 66f * _s, 12f, Amber, SKTextAlign.Center, bold: true);

            string rng = s.LockedTarget != null ? $"RNG {s.LockedTarget.RangeM:0000}M" : "RNG ----";
            Text(c, rng, x, y + 84f * _s, 12f, Bright, SKTextAlign.Center);
        }
        else
        {
            // missile seeker: pulsing bracket + rotating diamond
            float r = (26f + 2.5f * MathF.Sin(_t * 6f)) * _s;
            DrawBracket(c, x, y, r, Main, 2f);
            c.Save();
            c.Translate(x, y);
            c.RotateDegrees(_t * 90f);
            float d = 7f * _s;
            _path.Reset();
            _path.MoveTo(0f, -d); _path.LineTo(d, 0f); _path.LineTo(0f, d); _path.LineTo(-d, 0f);
            _path.Close();
            StrokePath(c, _path, Main, 1.5f);
            c.Restore();

            string tone = s.LockSteady ? "TONE" : "SEEK";
            if (!s.LockSteady || Blink(5f))
                Text(c, tone, x, y + 48f * _s, 12f, s.LockSteady ? Amber : Main.WithAlpha(180), SKTextAlign.Center, bold: s.LockSteady);
        }
    }

    // ---------------------------------------------------------------- targets

    private void DrawTargets(SKCanvas c, MechSim s)
    {
        float pxDeg = 11f * _s;
        foreach (var ct in s.Contacts)
        {
            float relB = MechSim.AngDiff(ct.BearingDeg, s.HeadingDeg);
            if (MathF.Abs(relB) > 26f)
            {
                if (ct == s.LockedTarget) DrawOffscreenCue(c, s, relB);
                continue;
            }

            float x = CX + relB * pxDeg;
            float y = CY + (s.PitchDeg * 0.6f - ct.ElevDeg) * pxDeg + _bob * 0.6f;
            y = Math.Clamp(y, CY - 150f * _s, CY + 150f * _s);

            if (ct == s.LockedTarget)
            {
                DrawLockHex(c, s, ct, x, y);
            }
            else
            {
                HexPath(x, y, 13f * _s);
                StrokePath(c, _path, Red.WithAlpha(190), 1.5f);
                Text(c, ct.Name, x, y - 20f * _s, 10f, Red.WithAlpha(200), SKTextAlign.Center);
                Text(c, $"{ct.RangeM:0000}", x, y + 28f * _s, 10f, Red.WithAlpha(160), SKTextAlign.Center);
            }
        }
    }

    private void DrawLockHex(SKCanvas c, MechSim s, GroundContact ct, float x, float y)
    {
        bool steady = s.LockSteady;
        float r = (steady ? 30f : 38f + 4f * MathF.Sin(_t * 9f)) * _s;

        HexPath(x, y, r);
        StrokePath(c, _path, Red, 2.2f);

        c.Save();
        c.Translate(x, y);
        c.RotateDegrees(_t * 70f);
        for (int i = 0; i < 3; i++)
        {
            _path.Reset();
            _path.MoveTo(0f, -(r + 6f * _s));
            _path.LineTo(-5f * _s, -(r + 15f * _s));
            _path.LineTo(5f * _s, -(r + 15f * _s));
            _path.Close();
            StrokePath(c, _path, Red, 1.5f, glow: false);
            c.RotateDegrees(120f);
        }
        c.Restore();

        float lx = x + r + 16f * _s, ly = y - r - 8f * _s;
        Line(c, x + r * 0.7f, y - r * 0.7f, lx, ly, Red.WithAlpha(180), 1.3f);
        Line(c, lx, ly, lx + 104f * _s, ly, Red.WithAlpha(180), 1.3f);
        Text(c, ct.Name, lx + 4f * _s, ly - 5f * _s, 12f, Red, bold: true);
        Text(c, ct.Type, lx + 4f * _s, ly + 13f * _s, 11f, Bright);
        Text(c, $"RNG {ct.RangeM,4:0}M", lx + 4f * _s, ly + 27f * _s, 11f, Bright);
        Text(c, $"CLS {ct.ClosureMs,3:+0;-0}M/S", lx + 4f * _s, ly + 41f * _s, 11f, Bright);

        if (steady)
        {
            bool shoot = ct.RangeM < 600f;
            if (Blink(shoot ? 6f : 2.5f))
                Text(c, shoot ? "SHOOT" : "LOCK ON", x, y + r + 24f * _s, 14f, shoot ? Red : Amber, SKTextAlign.Center, bold: true);
        }
        else
        {
            Text(c, "ACQ...", x, y + r + 24f * _s, 12f, Amber, SKTextAlign.Center);
        }

        if (s.SmgFiring)
        {
            for (int i = 0; i < 4; i++)
            {
                float gx = x + ((float)_rng.NextDouble() * 2f - 1f) * 24f * _s;
                float gy = y + ((float)_rng.NextDouble() * 2f - 1f) * 24f * _s;
                _fill.Color = Bright.WithAlpha((byte)_rng.Next(120, 255));
                c.DrawCircle(gx, gy, 1.6f * _s, _fill);
            }
        }
    }

    private void DrawOffscreenCue(SKCanvas c, MechSim s, float relB)
    {
        float side = MathF.Sign(relB);
        float x = CX + side * 282f * _s, y = CY;
        _path.Reset();
        _path.MoveTo(x, y - 10f * _s);
        _path.LineTo(x + side * 14f * _s, y);
        _path.LineTo(x, y + 10f * _s);
        StrokePath(c, _path, Red, 2f);
        if (s.LockedTarget != null)
            Text(c, $"TGT {MechSim.Wrap360(s.LockedTarget.BearingDeg):000}", x - side * 6f * _s, y + 28f * _s, 11f, Red.WithAlpha(200), SKTextAlign.Center);
    }

    // ------------------------------------------------------- heading / motion

    private void DrawHeadingTape(SKCanvas c, MechSim s)
    {
        float y = 64f * _s;
        float halfW = 250f * _s;
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

    private void DrawMotion(SKCanvas c, MechSim s)
    {
        // speed box (left of reticle)
        float sx = CX - 205f * _s;
        Text(c, "SPD", sx, CY - 28f * _s, 11f, Main.WithAlpha(170), SKTextAlign.Center);
        DrawTechBox(c, sx, CY, 92f * _s, 34f * _s, $"{s.SpeedKmh:000}", 18f, Bright);
        Text(c, "KM/H", sx, CY + 32f * _s, 10f, Main.WithAlpha(150), SKTextAlign.Center);

        // AGL box (right of reticle) + movement mode tag
        float ax = CX + 205f * _s;
        Text(c, "AGL", ax, CY - 28f * _s, 11f, Main.WithAlpha(170), SKTextAlign.Center);
        DrawTechBox(c, ax, CY, 92f * _s, 34f * _s, $"{s.AglM:0.0}M", 18f, s.Mode == MoveMode.Hover ? Amber : Bright);
        switch (s.Mode)
        {
            case MoveMode.Walk:
                Text(c, "WALK", ax, CY + 34f * _s, 11f, Main, SKTextAlign.Center, bold: true);
                break;
            case MoveMode.Dash:
                Text(c, "DASH", ax, CY + 34f * _s, 11f, Amber, SKTextAlign.Center, bold: true);
                break;
            default:
                if (Blink(3f))
                    Text(c, "NOE FLT", ax, CY + 34f * _s, 11f, Amber, SKTextAlign.Center, bold: true);
                break;
        }

        if (s.SwapFlash > 0f && Blink(5f))
        {
            string arm = s.ActiveSide == WeaponSide.Left ? "L-ARM" : "R-ARM";
            Text(c, $"WPN SWAP >> {arm}", CX, CY - 128f * _s, 14f, Amber, SKTextAlign.Center, bold: true);
        }
    }

    // ---------------------------------------------------------- weapon panels

    private void WeaponPanelFrame(SKCanvas c, float x, float y, float w, float h, bool active)
    {
        PanelPath(x, y, w, h, 14f * _s);
        _fill.Color = Panel;
        c.DrawPath(_path, _fill);
        StrokePath(c, _path, active ? Main : Main.WithAlpha(110), active ? 2.2f : 1.4f, glow: active);
        if (active)
        {
            _path.Reset();
            _path.MoveTo(x + 14f * _s, y);
            _path.LineTo(x + 34f * _s, y);
            _path.LineTo(x + 14f * _s, y + 20f * _s);
            _path.Close();
            _fill.Color = Main.WithAlpha(190);
            c.DrawPath(_path, _fill);
        }
    }

    private void StatusTag(SKCanvas c, float rx, float y, bool active, bool reloading)
    {
        if (reloading)
            Text(c, Blink(4f) ? "RELOAD" : "", rx, y, 11f, Amber, SKTextAlign.Right, bold: true);
        else if (active)
            Text(c, ">> ACTIVE", rx, y, 11f, Amber, SKTextAlign.Right, bold: true);
        else
            Text(c, "STBY", rx, y, 11f, Main.WithAlpha(120), SKTextAlign.Right);
    }

    private void ProgressBar(SKCanvas c, float x, float y, float w, float h, float frac, SKColor col)
    {
        _stroke.Color = Main.WithAlpha(140);
        _stroke.StrokeWidth = 1.2f * _s;
        _stroke.PathEffect = null;
        c.DrawRect(x, y, w, h, _stroke);
        _fill.Color = col;
        c.DrawRect(x + 1.5f * _s, y + 1.5f * _s, (w - 3f * _s) * Math.Clamp(frac, 0f, 1f), h - 3f * _s, _fill);
    }

    private void DrawWeaponPanelLeft(SKCanvas c, MechSim s)
    {
        float w = 236f * _s, h = 196f * _s;
        float x = 34f * _s, y = CY - h / 2f;
        bool active = s.ActiveSide == WeaponSide.Left;
        WeaponPanelFrame(c, x, y, w, h, active);

        Text(c, "L-ARM", x + 40f * _s, y + 22f * _s, 14f, active ? Bright : Main, bold: true);
        StatusTag(c, x + w - 14f * _s, y + 22f * _s, active, s.SmgReload > 0f);
        Line(c, x + 14f * _s, y + 32f * _s, x + w - 14f * _s, y + 32f * _s, Main.WithAlpha(120), 1f);
        Text(c, "SMG-88 9x40MM", x + 14f * _s, y + 52f * _s, 12f, Bright);

        // --- SMG silhouette; the magazine doubles as the ammo gauge
        float ox = x + 30f * _s, oy = y + 78f * _s;
        var col = active ? Main : Main.WithAlpha(130);

        _path.Reset();
        _path.MoveTo(ox - 16f * _s, oy + 2f * _s);            // stock
        _path.LineTo(ox, oy + 2f * _s);
        _path.MoveTo(ox - 16f * _s, oy + 2f * _s);
        _path.LineTo(ox - 16f * _s, oy + 14f * _s);
        _path.LineTo(ox, oy + 14f * _s);
        _path.AddRect(SKRect.Create(ox, oy, 78f * _s, 16f * _s));        // receiver
        _path.AddRect(SKRect.Create(ox + 26f * _s, oy + 16f * _s, 10f * _s, 16f * _s)); // grip
        StrokePath(c, _path, col, 1.5f, glow: false);

        Line(c, ox + 78f * _s, oy + 5f * _s, ox + 112f * _s, oy + 5f * _s, col, 3f);   // barrel
        Line(c, ox + 112f * _s, oy + 1f * _s, ox + 112f * _s, oy + 9f * _s, col, 2f);  // muzzle

        float magX = ox + 46f * _s, magY = oy + 16f * _s, magW = 22f * _s, magH = 30f * _s;
        float frac = s.SmgRounds / (float)MechSim.SmgMagSize;
        _stroke.Color = col;
        _stroke.StrokeWidth = 1.5f * _s;
        c.DrawRect(magX, magY, magW, magH, _stroke);
        _fill.Color = (frac < 0.25f ? Amber : Main).WithAlpha(170);
        float fh = magH * frac;
        c.DrawRect(magX, magY + magH - fh, magW, fh, _fill);

        if (s.SmgFiring && Blink(12f))
        {
            Line(c, ox + 114f * _s, oy + 5f * _s, ox + 126f * _s, oy + 5f * _s, Bright, 2.2f);
            Line(c, ox + 114f * _s, oy + 3f * _s, ox + 124f * _s, oy - 3f * _s, Bright, 1.6f);
            Line(c, ox + 114f * _s, oy + 7f * _s, ox + 124f * _s, oy + 13f * _s, Bright, 1.6f);
        }

        // --- numbers
        Text(c, $"{s.SmgRounds:000}", x + w - 16f * _s, y + 100f * _s, 26f, Bright, SKTextAlign.Right, bold: true);
        Text(c, $"/{MechSim.SmgMagSize}", x + w - 16f * _s, y + 117f * _s, 11f, Main.WithAlpha(150), SKTextAlign.Right);
        Text(c, $"MAG x{s.SmgMags}", x + w - 16f * _s, y + 133f * _s, 11f, Main, SKTextAlign.Right);

        if (s.SmgReload > 0f)
        {
            ProgressBar(c, x + 14f * _s, y + 152f * _s, w - 28f * _s, 8f * _s,
                        1f - s.SmgReload / MechSim.SmgReloadTime, Amber.WithAlpha(200));
            if (Blink(4f)) Text(c, "RELOADING", x + w / 2f, y + 148f * _s, 11f, Amber, SKTextAlign.Center, bold: true);
        }
        else if (s.SmgFiring)
        {
            Text(c, "FIRING", x + 14f * _s, y + 158f * _s, 11f, Red, bold: true);
        }

        Text(c, "PWR LINK OK   FEED NORM", x + 14f * _s, y + h - 12f * _s, 9f, Main.WithAlpha(110));
    }

    private void DrawWeaponPanelRight(SKCanvas c, MechSim s)
    {
        float w = 236f * _s, h = 196f * _s;
        float x = W - 34f * _s - w, y = CY - h / 2f;
        bool active = s.ActiveSide == WeaponSide.Right;
        bool cannon = s.RightWeapon == RightArm.Cannon;
        bool reloading = cannon ? s.CannonRackReload > 0f : s.MslReload > 0f;
        WeaponPanelFrame(c, x, y, w, h, active);

        Text(c, "R-ARM", x + 40f * _s, y + 22f * _s, 14f, active ? Bright : Main, bold: true);
        StatusTag(c, x + w - 14f * _s, y + 22f * _s, active, reloading);
        Line(c, x + 14f * _s, y + 32f * _s, x + w - 14f * _s, y + 32f * _s, Main.WithAlpha(120), 1f);
        Text(c, cannon ? "LRC-120 120MM CANNON" : "ATM-8 GUIDED MSL", x + 14f * _s, y + 52f * _s, 12f, Bright);

        var col = active ? Main : Main.WithAlpha(130);
        float ox = x + 22f * _s, oy = y + 78f * _s;

        if (cannon)
        {
            // --- cannon silhouette (barrel points left) with shell rack gauge
            _path.Reset();
            _path.AddRect(SKRect.Create(ox - 8f * _s, oy + 2f * _s, 8f * _s, 11f * _s));   // muzzle brake
            _path.AddRect(SKRect.Create(ox, oy + 4f * _s, 96f * _s, 7f * _s));             // barrel
            _path.AddRect(SKRect.Create(ox + 96f * _s, oy - 6f * _s, 46f * _s, 26f * _s)); // breech
            StrokePath(c, _path, col, 1.5f, glow: false);
            Line(c, ox + 20f * _s, oy - 1f * _s, ox + 90f * _s, oy - 1f * _s, col.WithAlpha(150), 1.2f);

            // shell rack: 2 rows x 6 cells = 12 shells
            float rx = ox + 96f * _s, ry = oy + 26f * _s;
            for (int i = 0; i < MechSim.CannonMax; i++)
            {
                float cxx = rx + i % 6 * 8f * _s;
                float cyy = ry + i / 6 * 11f * _s;
                if (i < s.CannonShells)
                {
                    _fill.Color = col.WithAlpha(190);
                    c.DrawRect(cxx, cyy, 6.5f * _s, 9f * _s, _fill);
                }
                else
                {
                    _stroke.Color = col.WithAlpha(60);
                    _stroke.StrokeWidth = 1f * _s;
                    c.DrawRect(cxx, cyy, 6.5f * _s, 9f * _s, _stroke);
                }
            }

            if (s.CannonFlash > 0f)
            {
                Line(c, ox - 10f * _s, oy + 7.5f * _s, ox - 26f * _s, oy + 7.5f * _s, Bright, 2.4f);
                Line(c, ox - 10f * _s, oy + 4f * _s, ox - 22f * _s, oy - 2f * _s, Bright, 1.6f);
                Line(c, ox - 10f * _s, oy + 11f * _s, ox - 22f * _s, oy + 17f * _s, Bright, 1.6f);
            }

            Text(c, $"{s.CannonShells:00}", x + w - 16f * _s, y + 100f * _s, 26f, Bright, SKTextAlign.Right, bold: true);
            Text(c, $"/{MechSim.CannonMax}", x + w - 16f * _s, y + 117f * _s, 11f, Main.WithAlpha(150), SKTextAlign.Right);

            if (s.CannonRackReload > 0f)
            {
                ProgressBar(c, x + 14f * _s, y + 152f * _s, w - 28f * _s, 8f * _s,
                            1f - s.CannonRackReload / MechSim.CannonRackTime, Amber.WithAlpha(200));
                if (Blink(4f)) Text(c, "RACK RELOAD", x + w / 2f, y + 148f * _s, 11f, Amber, SKTextAlign.Center, bold: true);
            }
            else
            {
                ProgressBar(c, x + 14f * _s, y + 152f * _s, w - 28f * _s, 8f * _s,
                            s.CannonCharge, (s.CannonCharge >= 1f ? Amber : Main).WithAlpha(190));
                Text(c, "CHG", x + 14f * _s, y + 148f * _s, 9f, Main.WithAlpha(140));
                if (s.CannonCharge >= 1f && Blink(4f))
                    Text(c, "RDY", x + w - 14f * _s, y + 148f * _s, 10f, Amber, SKTextAlign.Right, bold: true);
            }
        }
        else
        {
            // --- missile box silhouette: 2x4 tubes, filled = remaining
            var boxCol = s.MslFlash > 0f && Blink(6f) ? Amber : col;
            _stroke.Color = boxCol;
            _stroke.StrokeWidth = 1.5f * _s;
            _stroke.PathEffect = null;
            c.DrawRect(ox + 6f * _s, oy - 14f * _s, 118f * _s, 50f * _s, _stroke);
            for (int i = 0; i < MechSim.MslMax; i++)
            {
                float cxx = ox + 24f * _s + i % 4 * 27f * _s;
                float cyy = oy - 2f * _s + i / 4 * 26f * _s;
                if (i < s.MslCount)
                {
                    _fill.Color = col.WithAlpha(180);
                    c.DrawCircle(cxx, cyy, 8f * _s, _fill);
                }
                else
                {
                    _stroke.Color = col.WithAlpha(60);
                    _stroke.StrokeWidth = 1.2f * _s;
                    c.DrawCircle(cxx, cyy, 8f * _s, _stroke);
                }
            }

            Text(c, $"{s.MslCount:00}", x + w - 16f * _s, y + 100f * _s, 26f, Bright, SKTextAlign.Right, bold: true);
            Text(c, $"/{MechSim.MslMax}", x + w - 16f * _s, y + 117f * _s, 11f, Main.WithAlpha(150), SKTextAlign.Right);

            if (s.MslReload > 0f)
            {
                ProgressBar(c, x + 14f * _s, y + 152f * _s, w - 28f * _s, 8f * _s,
                            1f - s.MslReload / MechSim.MslReloadTime, Amber.WithAlpha(200));
                if (Blink(4f)) Text(c, "BOX RELOAD", x + w / 2f, y + 148f * _s, 11f, Amber, SKTextAlign.Center, bold: true);
            }
            else if (s.MslFlash > 0f && Blink(5f))
            {
                Text(c, "ATM AWAY!", x + w / 2f, y + 152f * _s, 12f, Amber, SKTextAlign.Center, bold: true);
            }
        }

        Text(c, "FCS LINK OK   SAFETY OFF", x + 14f * _s, y + h - 12f * _s, 9f, Main.WithAlpha(110));
    }

    // ------------------------------------------------- damage + propellant

    private void DrawDamagePanel(SKCanvas c, MechSim s)
    {
        float ph = 196f * _s;
        float y0 = H - 40f * _s - ph;
        Text(c, "DMG STATUS", CX, y0 + 2f * _s, 12f, Main, SKTextAlign.Center, bold: true);

        // part rectangles, front view (screen left = unit's left)
        var rects = new SKRect[MechSim.PartCount];
        rects[0] = SKRect.Create(CX - 10f * _s, y0 + 18f * _s, 20f * _s, 16f * _s);  // HEAD
        rects[1] = SKRect.Create(CX - 26f * _s, y0 + 38f * _s, 52f * _s, 52f * _s);  // CORE
        rects[2] = SKRect.Create(CX - 52f * _s, y0 + 40f * _s, 20f * _s, 46f * _s);  // L-ARM
        rects[3] = SKRect.Create(CX + 32f * _s, y0 + 40f * _s, 20f * _s, 46f * _s);  // R-ARM
        rects[4] = SKRect.Create(CX - 24f * _s, y0 + 96f * _s, 20f * _s, 56f * _s);  // L-LEG
        rects[5] = SKRect.Create(CX + 4f * _s, y0 + 96f * _s, 20f * _s, 56f * _s);   // R-LEG
        rects[6] = SKRect.Create(CX - 74f * _s, y0 + 34f * _s, 14f * _s, 40f * _s);  // L-JU
        rects[7] = SKRect.Create(CX + 60f * _s, y0 + 34f * _s, 14f * _s, 40f * _s);  // R-JU

        int worst = 0;
        for (int i = 0; i < MechSim.PartCount; i++)
        {
            float hp = s.PartHp[i];
            if (hp < s.PartHp[worst]) worst = i;

            SKColor col = hp > 70f ? Main : hp > 35f ? Amber : Red;
            bool flashHit = s.ImpactTimer > 0f && s.ImpactPart == i && Blink(10f);
            bool critBlink = hp <= 35f && !Blink(2f);

            _fill.Color = flashHit ? Bright.WithAlpha(220) : col.WithAlpha(critBlink ? (byte)25 : (byte)70);
            c.DrawRect(rects[i], _fill);
            _stroke.Color = flashHit ? Bright : col;
            _stroke.StrokeWidth = 1.5f * _s;
            _stroke.PathEffect = null;
            c.DrawRect(rects[i], _stroke);
        }

        Text(c, "L", CX - 67f * _s, y0 + 88f * _s, 9f, Main.WithAlpha(140), SKTextAlign.Center);
        Text(c, "R", CX + 67f * _s, y0 + 88f * _s, 9f, Main.WithAlpha(140), SKTextAlign.Center);

        float whp = s.PartHp[worst];
        SKColor wcol = whp > 70f ? Main : whp > 35f ? Amber : Red;
        Text(c, $"{MechSim.PartNames[worst]} {whp:0}%", CX, y0 + 172f * _s, 12f, wcol, SKTextAlign.Center, bold: whp <= 70f);

        // jump-unit propellant gauges flank the silhouette
        DrawPropBar(c, CX - 116f * _s, y0 + 24f * _s, s.PropL, "PL");
        DrawPropBar(c, CX + 100f * _s, y0 + 24f * _s, s.PropR, "PR");
        Text(c, "PRP", CX - 108f * _s, y0 + 14f * _s, 9f, Main.WithAlpha(150), SKTextAlign.Center);
        Text(c, "PRP", CX + 108f * _s, y0 + 14f * _s, 9f, Main.WithAlpha(150), SKTextAlign.Center);
    }

    private void DrawPropBar(SKCanvas c, float x, float y, float val, string label)
    {
        const int segs = 10;
        float w = 16f * _s, segH = 11f * _s, gap = 2f * _s;
        SKColor col = val < 10f ? (Blink(3f) ? Red : Red.WithAlpha(120)) : val < 25f ? Amber : Main;

        for (int i = 0; i < segs; i++)
        {
            float sy = y + (segs - 1 - i) * (segH + gap);
            bool on = val / 100f * segs > i + 0.5f;
            if (on)
            {
                _fill.Color = col.WithAlpha(200);
                c.DrawRect(x, sy, w, segH, _fill);
            }
            else
            {
                _stroke.Color = Main.WithAlpha(60);
                _stroke.StrokeWidth = 1f * _s;
                _stroke.PathEffect = null;
                c.DrawRect(x, sy, w, segH, _stroke);
            }
        }
        Text(c, $"{label} {val:0}", x + w / 2f, y + segs * (segH + gap) + 12f * _s, 10f, col, SKTextAlign.Center);
    }

    // ----------------------------------------------------------------- radar

    private void DrawRadar(SKCanvas c, MechSim s)
    {
        float R = 110f * _s;
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
                new[] { Main.WithAlpha(0), Main.WithAlpha(0), Main.WithAlpha(110) },
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

        // friendly squad blips derived from map positions (heading-up)
        foreach (var u in s.Squad)
        {
            float ux = u.PosX(s.Time, s.OwnX) - s.OwnX;
            float uy = u.PosY(s.Time, s.OwnY) - s.OwnY;
            float rng = MathF.Sqrt(ux * ux + uy * uy);
            if (rng > 3000f) continue;
            float brg = MathF.Atan2(ux, uy) * 180f / MathF.PI;
            float disp = MechSim.Wrap360(brg - s.HeadingDeg) * MathF.PI / 180f;
            float dist = rng / 3000f * (R - 8f * _s);
            _fill.Color = Cyan.WithAlpha(200);
            c.DrawCircle(cx + MathF.Sin(disp) * dist, cy - MathF.Cos(disp) * dist, (u.Platoon == "D1" ? 3.4f : 2.4f) * _s, _fill);
        }

        // hostile blips with sweep fade
        foreach (var ct in s.Contacts)
        {
            float disp = MechSim.Wrap360(ct.BearingDeg - s.HeadingDeg);
            float dist = MathF.Min(ct.RangeM / 3000f, 0.94f) * R;
            float ar = disp * MathF.PI / 180f;
            float bx = cx + MathF.Sin(ar) * dist;
            float by = cy - MathF.Cos(ar) * dist;

            float since = MechSim.Wrap360(s.RadarSweepDeg - disp);
            byte alpha = (byte)Math.Clamp(235f - since * 0.55f, 60f, 235f);
            _path.Reset();
            _path.MoveTo(bx, by - 5.5f * _s);
            _path.LineTo(bx + 5f * _s, by + 4f * _s);
            _path.LineTo(bx - 5f * _s, by + 4f * _s);
            _path.Close();
            _fill.Color = Red.WithAlpha(alpha);
            c.DrawPath(_path, _fill);

            if (ct == s.LockedTarget)
            {
                _stroke.Color = Red.WithAlpha(Blink(2.5f) ? (byte)230 : (byte)90);
                _stroke.StrokeWidth = 1.4f * _s;
                c.DrawCircle(bx, by, 9f * _s, _stroke);
            }
        }

        // own ship chevron
        _path.Reset();
        _path.MoveTo(cx, cy - 7f * _s);
        _path.LineTo(cx + 5.5f * _s, cy + 6f * _s);
        _path.LineTo(cx, cy + 2.5f * _s);
        _path.LineTo(cx - 5.5f * _s, cy + 6f * _s);
        _path.Close();
        _fill.Color = Bright;
        c.DrawPath(_path, _fill);

        float na = MechSim.Wrap360(-s.HeadingDeg) * MathF.PI / 180f;
        Text(c, "N", cx + MathF.Sin(na) * (R - 15f * _s), cy - MathF.Cos(na) * (R - 15f * _s) + 4f * _s, 11f, Amber, SKTextAlign.Center, bold: true);

        Text(c, "RDR // GND", cx - R, cy - R - 16f * _s, 12f, Main, bold: true);
        Text(c, "RNG 3.0KM", cx + R, cy - R - 16f * _s, 12f, Main.WithAlpha(170), SKTextAlign.Right);
        Text(c, $"CONTACTS {s.Contacts.Count}  IFF ON", cx - R, cy + R + 20f * _s, 11f, Main.WithAlpha(150));
    }

    // ----------------------------------------------------------- terrain map

    private static float Height(float u, float v) =>
        0.45f * MathF.Sin(u * 7.3f + 1.7f) + 0.40f * MathF.Sin(v * 5.9f + 0.6f)
        + 0.35f * MathF.Sin(u * 4.1f + v * 6.3f + 2.2f) + 0.20f * MathF.Sin(u * 7.7f + v * 2.1f + 4.0f);

    private void BuildTerrain(float mw, float mh)
    {
        _terrain?.Dispose();
        var p = new SKPath();
        const int NX = 56, NY = 40;
        var f = new float[NX + 1, NY + 1];
        for (int i = 0; i <= NX; i++)
            for (int j = 0; j <= NY; j++)
                f[i, j] = Height(i / (float)NX, j / (float)NY);

        // marching squares over a few iso levels (offset to dodge exact zeros)
        float[] levels = { -0.547f, -0.253f, 0.061f, 0.357f, 0.653f };
        foreach (float lv in levels)
        {
            for (int i = 0; i < NX; i++)
            {
                for (int j = 0; j < NY; j++)
                {
                    float tl = f[i, j] - lv, tr = f[i + 1, j] - lv;
                    float br = f[i + 1, j + 1] - lv, bl = f[i, j + 1] - lv;
                    int idx = (tl > 0f ? 1 : 0) | (tr > 0f ? 2 : 0) | (br > 0f ? 4 : 0) | (bl > 0f ? 8 : 0);
                    if (idx == 0 || idx == 15) continue;

                    float x0 = i * mw / NX, x1 = (i + 1) * mw / NX;
                    float y0 = j * mh / NY, y1 = (j + 1) * mh / NY;
                    var T = new SKPoint(x0 + (x1 - x0) * (tl / (tl - tr)), y0);
                    var B = new SKPoint(x0 + (x1 - x0) * (bl / (bl - br)), y1);
                    var L = new SKPoint(x0, y0 + (y1 - y0) * (tl / (tl - bl)));
                    var R = new SKPoint(x1, y0 + (y1 - y0) * (tr / (tr - br)));

                    switch (idx)
                    {
                        case 1: case 14: p.MoveTo(L); p.LineTo(T); break;
                        case 2: case 13: p.MoveTo(T); p.LineTo(R); break;
                        case 3: case 12: p.MoveTo(L); p.LineTo(R); break;
                        case 4: case 11: p.MoveTo(R); p.LineTo(B); break;
                        case 6: case 9: p.MoveTo(T); p.LineTo(B); break;
                        case 7: case 8: p.MoveTo(L); p.LineTo(B); break;
                        case 5: p.MoveTo(L); p.LineTo(T); p.MoveTo(R); p.LineTo(B); break;
                        case 10: p.MoveTo(T); p.LineTo(R); p.MoveTo(L); p.LineTo(B); break;
                    }
                }
            }
        }
        _terrain = p;
        _terrW = (int)mw;
    }

    private void DrawTerrainMap(SKCanvas c, MechSim s)
    {
        float mw = 300f * _s, mh = 205f * _s;
        float x0 = W - 36f * _s - mw;
        float y0 = H - 40f * _s - mh;

        // map space (meters, +Y north) -> screen px
        float MapX(float wx) => x0 + (wx + 1500f) / 3000f * mw;
        float MapY(float wy) => y0 + (1050f - wy) / 2100f * mh;

        PanelPath(x0, y0, mw, mh, 14f * _s);
        _fill.Color = Panel;
        c.DrawPath(_path, _fill);
        StrokePath(c, _path, Main);

        if (_terrain == null || _terrW != (int)mw) BuildTerrain(mw, mh);

        PanelPath(x0, y0, mw, mh, 14f * _s);
        c.Save();
        c.ClipPath(_path, SKClipOperation.Intersect, true);

        c.Save();
        c.Translate(x0, y0);
        _stroke.Color = Main.WithAlpha(60);
        _stroke.StrokeWidth = 1f * _s;
        _stroke.PathEffect = null;
        c.DrawPath(_terrain!, _stroke);
        _stroke.Color = Main.WithAlpha(36);
        for (int i = 1; i < 6; i++) c.DrawLine(i * mw / 6f, 0f, i * mw / 6f, mh, _stroke);
        for (int j = 1; j < 4; j++) c.DrawLine(0f, j * mh / 4f, mw, j * mh / 4f, _stroke);
        c.Restore();

        // grid labels
        for (int i = 0; i < 6; i++)
            Text(c, ((char)('A' + i)).ToString(), x0 + i * mw / 6f + mw / 12f, y0 + 11f * _s, 8f, Main.WithAlpha(110), SKTextAlign.Center);
        for (int j = 0; j < 4; j++)
            Text(c, (j + 1).ToString(), x0 + 6f * _s, y0 + j * mh / 4f + mh / 8f, 8f, Main.WithAlpha(110));

        // objective marker
        float objX = MapX(800f), objY = MapY(-300f);
        _path.Reset();
        _path.MoveTo(objX, objY - 6f * _s); _path.LineTo(objX + 6f * _s, objY);
        _path.LineTo(objX, objY + 6f * _s); _path.LineTo(objX - 6f * _s, objY);
        _path.Close();
        StrokePath(c, _path, Amber, 1.5f, glow: false);
        Text(c, "OBJ-A", objX, objY + 16f * _s, 9f, Amber.WithAlpha(200), SKTextAlign.Center);

        // hostile contacts placed off own position + bearing/range
        int ei = 0;
        foreach (var ct in s.Contacts)
        {
            ei++;
            float brgRad = ct.BearingDeg * MathF.PI / 180f;
            float wx = s.OwnX + MathF.Sin(brgRad) * ct.RangeM;
            float wy = s.OwnY + MathF.Cos(brgRad) * ct.RangeM;
            float px = Math.Clamp(MapX(wx), x0 + 10f * _s, x0 + mw - 10f * _s);
            float py = Math.Clamp(MapY(wy), y0 + 10f * _s, y0 + mh - 10f * _s);
            bool clipped = px != MapX(wx) || py != MapY(wy);

            _path.Reset();
            _path.MoveTo(px, py - 5f * _s);
            _path.LineTo(px + 4.5f * _s, py + 3.5f * _s);
            _path.LineTo(px - 4.5f * _s, py + 3.5f * _s);
            _path.Close();
            if (clipped)
            {
                _stroke.Color = Red.WithAlpha(120);
                _stroke.StrokeWidth = 1.2f * _s;
                c.DrawPath(_path, _stroke);
            }
            else
            {
                _fill.Color = Red.WithAlpha(220);
                c.DrawPath(_path, _fill);
                Text(c, $"E{ei}", px + 7f * _s, py + 3f * _s, 8f, Red.WithAlpha(200));
            }
        }

        // squad: own platoon labelled per unit, other platoons get one label
        float d2x = 0f, d2y = 0f, d3x = 0f, d3y = 0f;
        int d2n = 0, d3n = 0;
        foreach (var u in s.Squad)
        {
            float px = MapX(u.PosX(s.Time, s.OwnX));
            float py = MapY(u.PosY(s.Time, s.OwnY));
            if (u.Platoon == "D1")
            {
                c.Save();
                c.Translate(px, py);
                c.RotateDegrees(u.HeadingRad(s.Time) * 180f / MathF.PI);
                _path.Reset();
                _path.MoveTo(0f, -5.5f * _s); _path.LineTo(3.8f * _s, 4.5f * _s); _path.LineTo(-3.8f * _s, 4.5f * _s);
                _path.Close();
                _fill.Color = Cyan.WithAlpha(220);
                c.DrawPath(_path, _fill);
                c.Restore();
                Text(c, u.Code, px + 7f * _s, py + 3f * _s, 8f, Cyan.WithAlpha(210));
            }
            else
            {
                _fill.Color = Cyan.WithAlpha(160);
                c.DrawCircle(px, py, 2.4f * _s, _fill);
                if (u.Platoon == "D2") { d2x += px; d2y += py; d2n++; }
                else { d3x += px; d3y += py; d3n++; }
            }
        }
        if (d2n > 0) Text(c, "D2", d2x / d2n, d2y / d2n - 9f * _s, 9f, Cyan, SKTextAlign.Center, bold: true);
        if (d3n > 0) Text(c, "D3", d3x / d3n, d3y / d3n - 9f * _s, 9f, Cyan, SKTextAlign.Center, bold: true);

        // own unit: white arrow + expanding ping
        float ox = MapX(s.OwnX), oy = MapY(s.OwnY);
        float ping = _t % 2.2f / 2.2f;
        _stroke.Color = Bright.WithAlpha((byte)(110f * (1f - ping)));
        _stroke.StrokeWidth = 1.2f * _s;
        _stroke.PathEffect = null;
        c.DrawCircle(ox, oy, 6f * _s + ping * 26f * _s, _stroke);
        c.Save();
        c.Translate(ox, oy);
        c.RotateDegrees(s.HeadingDeg);
        _path.Reset();
        _path.MoveTo(0f, -7f * _s); _path.LineTo(4.5f * _s, 5.5f * _s); _path.LineTo(0f, 2.5f * _s); _path.LineTo(-4.5f * _s, 5.5f * _s);
        _path.Close();
        _fill.Color = Bright;
        c.DrawPath(_path, _fill);
        c.Restore();
        Text(c, "D1-1", ox + 8f * _s, oy + 3f * _s, 8f, Bright);

        c.Restore(); // clip

        Text(c, "TAC MAP // GRID 27-K", x0, y0 - 8f * _s, 12f, Main, bold: true);
        Text(c, "N^", x0 + mw, y0 - 8f * _s, 11f, Amber, SKTextAlign.Right, bold: true);
        Text(c, "CO DAGGER x12   PLT D1/D2/D3   SCALE 3.0KM", x0 + mw, y0 + mh + 16f * _s, 9f, Main.WithAlpha(140), SKTextAlign.Right);
    }

    // ------------------------------------------------------------------ comms

    private void DrawCommPanel(SKCanvas c, MechSim s)
    {
        float w = 300f * _s, h = 124f * _s;
        float x = W - 36f * _s - w, y = 30f * _s;

        PanelPath(x, y, w, h, 14f * _s);
        _fill.Color = Panel;
        c.DrawPath(_path, _fill);
        StrokePath(c, _path, Main, 1.4f, glow: false);

        float pad = 12f * _s;
        Text(c, "COMM // DLNK", x + pad, y + 18f * _s, 12f, Main, bold: true);
        Text(c, DateTime.Now.ToString("HH:mm:ss"), x + w - pad, y + 18f * _s, 11f, Main.WithAlpha(180), SKTextAlign.Right);
        Line(c, x + pad, y + 26f * _s, x + w - pad, y + 26f * _s, Main.WithAlpha(120), 1f);

        string[] rows =
        {
            "CH1 OPEN  118.50  NET-4C77",
            $"CH2 SCT   {s.SecureKey}",
            "CH3 CMD   SQ-NET  ID-91B0",
        };
        for (int i = 0; i < 3; i++)
        {
            float ry = y + 44f * _s + i * 17f * _s;
            bool act = s.ActiveChannel == i;
            if (act)
            {
                _fill.Color = Main.WithAlpha(28);
                c.DrawRect(x + pad - 4f * _s, ry - 12f * _s, w - pad * 2f + 8f * _s, 16f * _s, _fill);
                Text(c, ">", x + pad, ry, 11f, Amber, bold: true);
            }
            Text(c, rows[i], x + pad + 14f * _s, ry, 11f, act ? Bright : Main.WithAlpha(160), bold: act);
            if (i == 1)
                Text(c, "ENC", x + w - pad, ry, 9f, Amber.WithAlpha(act ? (byte)255 : (byte)150), SKTextAlign.Right, bold: true);
        }

        // waveform + TX/RX status line
        float wy = y + h - 14f * _s;
        bool tx = s.Transmitting;
        float amp = tx ? 1f : 0.16f;
        for (int i = 0; i < 20; i++)
        {
            float bh = (0.2f + 0.8f * MathF.Abs(MathF.Sin(_t * 9.7f + i * 1.31f) * MathF.Sin(_t * 3.3f + i * 0.7f))) * 14f * _s * amp;
            _fill.Color = (tx ? Main : Main.WithAlpha(90));
            c.DrawRect(x + pad + i * 7f * _s, wy - bh, 4.5f * _s, bh, _fill);
        }
        if (tx)
        {
            if (Blink(4f)) Text(c, "TX", x + pad + 146f * _s, wy, 11f, Red, bold: true);
            Text(c, $"{s.TxFrom} >> D1-1", x + w - pad, wy, 11f, Bright, SKTextAlign.Right);
        }
        else
        {
            Text(c, "RX", x + pad + 146f * _s, wy, 11f, Main.WithAlpha(120));
            Text(c, "NET IDLE", x + w - pad, wy, 11f, Main.WithAlpha(120), SKTextAlign.Right);
        }
    }

    // ----------------------------------------------------------------- status

    private void DrawStatus(SKCanvas c, MechSim s)
    {
        float m = 36f * _s;
        float lh = 17f * _s;
        float y = m + 14f * _s;

        Text(c, "UNIT D1-1 // CO DAGGER", m, y, 12f, Amber, bold: true);
        Text(c, "MODE GND COMBAT", m, y + lh, 12f, Main);
        Text(c, $"T+{TimeSpan.FromSeconds(s.Time):mm\\:ss}", m, y + lh * 2f, 12f, Main);

        int worst = 0;
        for (int i = 1; i < MechSim.PartCount; i++)
            if (s.PartHp[i] < s.PartHp[worst]) worst = i;
        float whp = s.PartHp[worst];
        if (whp <= 35f)
            Text(c, $"DMG CRIT {MechSim.PartNames[worst]}", m, y + lh * 3f, 12f, Red, bold: Blink(3f));
        else if (whp <= 70f)
            Text(c, $"DMG {MechSim.PartNames[worst]} {whp:0}%", m, y + lh * 3f, 12f, Amber);
        else
            Text(c, "SYS NOMINAL", m, y + lh * 3f, 12f, Main);

        Text(c, "GND-NAV OK   IFF ON   FCS LINKED   AP OFF", CX, H - 14f * _s, 10f, Main.WithAlpha(110), SKTextAlign.Center);
    }

    private void DrawWarnings(SKCanvas c, MechSim s)
    {
        if (s.IncomingTimer > 0f && Blink(4f))
        {
            Text(c, ">> INCOMING ARTILLERY <<", CX, 150f * _s, 20f, Amber, SKTextAlign.Center, bold: true);
            _stroke.Color = Amber.WithAlpha(160);
            _stroke.StrokeWidth = 3f * _s;
            _stroke.PathEffect = null;
            c.DrawRect(8f * _s, 8f * _s, W - 16f * _s, H - 16f * _s, _stroke);
        }

        if (s.ImpactTimer > 0f)
        {
            if (Blink(8f))
                Text(c, $"IMPACT - {MechSim.PartNames[Math.Max(0, s.ImpactPart)]}", CX, 180f * _s, 16f, Red, SKTextAlign.Center, bold: true);
            _stroke.Color = Red.WithAlpha((byte)Math.Min(200f, s.ImpactTimer * 220f));
            _stroke.StrokeWidth = 5f * _s;
            _stroke.PathEffect = null;
            c.DrawRect(4f * _s, 4f * _s, W - 8f * _s, H - 8f * _s, _stroke);
        }

        if ((s.PropL < 22f || s.PropR < 22f) && Blink(2f))
            Text(c, "PROPELLANT LOW", CX, 208f * _s, 14f, Amber, SKTextAlign.Center, bold: true);
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
        _vignette?.Dispose();
        _sweepPath?.Dispose();
        _sweepShader?.Dispose();
        _terrain?.Dispose();
    }
}

using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace WorkHmiCodex53;

public partial class MainWindow : Window
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering += OnRendering;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        HmiSurface.InvalidateVisual();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        float w = info.Width;
        float h = info.Height;
        float t = (float)_clock.Elapsed.TotalSeconds;

        canvas.Clear(SKColors.Black);
        DrawBackdrop(canvas, w, h, t);

        DrawEnergyNetwork(canvas, w, h, t);
        DrawTanks(canvas, w, h, t);
        DrawFans(canvas, w, h, t);
        DrawHud(canvas, w, h, t);
    }

    private static void DrawBackdrop(SKCanvas canvas, float w, float h, float t)
    {
        using var bgPaint = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(0, h),
                new[]
                {
                    new SKColor(3, 8, 18),
                    new SKColor(8, 16, 30),
                    new SKColor(2, 6, 14)
                },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(new SKRect(0, 0, w, h), bgPaint);

        using var gridPaint = new SKPaint
        {
            Color = new SKColor(28, 91, 138, 40),
            IsAntialias = true,
            StrokeWidth = 1,
            Style = SKPaintStyle.Stroke
        };

        float spacing = MathF.Max(36, w * 0.035f);
        float pan = (t * 18f) % spacing;
        for (float x = -spacing; x < w + spacing; x += spacing)
        {
            canvas.DrawLine(x + pan, 0, x + pan - w * 0.08f, h, gridPaint);
        }

        for (float y = 0; y < h; y += spacing * 0.7f)
        {
            canvas.DrawLine(0, y, w, y, gridPaint);
        }

        using var vignette = new SKPaint
        {
            IsAntialias = true,
            Shader = SKShader.CreateRadialGradient(
                new SKPoint(w * 0.5f, h * 0.5f),
                MathF.Max(w, h) * 0.7f,
                new[]
                {
                    new SKColor(0, 0, 0, 0),
                    new SKColor(0, 0, 0, 180)
                },
                new float[] { 0.55f, 1f },
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRect(new SKRect(0, 0, w, h), vignette);
    }

    private static void DrawEnergyNetwork(SKCanvas canvas, float w, float h, float t)
    {
        SKPoint[] mainLine =
        [
            new(w * 0.08f, h * 0.22f),
            new(w * 0.34f, h * 0.22f),
            new(w * 0.34f, h * 0.46f),
            new(w * 0.55f, h * 0.46f),
            new(w * 0.55f, h * 0.32f),
            new(w * 0.82f, h * 0.32f)
        ];

        SKPoint[] branchA =
        [
            new(w * 0.34f, h * 0.46f),
            new(w * 0.34f, h * 0.72f),
            new(w * 0.68f, h * 0.72f),
            new(w * 0.68f, h * 0.56f)
        ];

        SKPoint[] branchB =
        [
            new(w * 0.55f, h * 0.46f),
            new(w * 0.74f, h * 0.46f),
            new(w * 0.74f, h * 0.18f),
            new(w * 0.90f, h * 0.18f)
        ];

        SKPoint[] magentaLine =
        [
            new(w * 0.09f, h * 0.83f),
            new(w * 0.30f, h * 0.83f),
            new(w * 0.30f, h * 0.58f),
            new(w * 0.50f, h * 0.58f),
            new(w * 0.50f, h * 0.82f),
            new(w * 0.72f, h * 0.82f)
        ];

        SKPoint[] magentaBranch =
        [
            new(w * 0.50f, h * 0.58f),
            new(w * 0.64f, h * 0.58f),
            new(w * 0.64f, h * 0.42f),
            new(w * 0.79f, h * 0.42f)
        ];

        SKPoint[] tankFeedA =
        [
            new(w * 0.68f, h * 0.72f),
            new(w * 0.79f, h * 0.72f)
        ];

        SKPoint[] tankFeedB =
        [
            new(w * 0.72f, h * 0.82f),
            new(w * 0.84f, h * 0.82f),
            new(w * 0.84f, h * 0.70f)
        ];

        SKPoint[] tankFeedC =
        [
            new(w * 0.34f, h * 0.72f),
            new(w * 0.24f, h * 0.72f),
            new(w * 0.24f, h * 0.58f)
        ];

        DrawFlowPath(canvas, mainLine, t, new SKColor(20, 212, 255), new SKColor(170, 255, 255));
        DrawFlowPath(canvas, branchA, t + 0.8f, new SKColor(20, 212, 255), new SKColor(170, 255, 255));
        DrawFlowPath(canvas, branchB, t + 1.3f, new SKColor(20, 212, 255), new SKColor(170, 255, 255));
        DrawFlowPath(canvas, magentaLine, t + 0.35f, new SKColor(255, 84, 212), new SKColor(255, 180, 240));
        DrawFlowPath(canvas, magentaBranch, t + 1.1f, new SKColor(255, 84, 212), new SKColor(255, 180, 240));
        DrawFlowPath(canvas, tankFeedA, t + 1.7f, new SKColor(20, 212, 255), new SKColor(170, 255, 255), 12f);
        DrawFlowPath(canvas, tankFeedB, t + 0.9f, new SKColor(255, 84, 212), new SKColor(255, 180, 240), 12f);
        DrawFlowPath(canvas, tankFeedC, t + 1.35f, new SKColor(20, 212, 255), new SKColor(170, 255, 255), 12f);

        SKPoint[] joints =
        [
            mainLine[1], mainLine[2], mainLine[3], mainLine[4],
            branchA[1], branchA[2], branchA[3],
            branchB[1], branchB[2],
            magentaLine[2], magentaLine[3], magentaLine[4],
            magentaBranch[1], magentaBranch[2],
            tankFeedB[1],
            tankFeedC[1]
        ];

        float pulse = 0.45f + 0.55f * (MathF.Sin(t * 2.4f) * 0.5f + 0.5f);
        byte alpha = (byte)(150 + pulse * 100);
        foreach (SKPoint p in joints)
        {
            using var glow = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = new SKColor(40, 220, 255, alpha),
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 8)
            };
            canvas.DrawCircle(p, MathF.Max(5, h * 0.008f), glow);

            using var core = new SKPaint
            {
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                Color = new SKColor(170, 255, 255, 240)
            };
            canvas.DrawCircle(p, MathF.Max(2, h * 0.0035f), core);
        }

        DrawBranchCoverJoint(canvas, mainLine[2], h, t);
        DrawBranchCoverJoint(canvas, mainLine[3], h, t + 0.6f);
        DrawBranchCoverJoint(canvas, magentaLine[3], h, t + 1.1f);
        DrawBranchCoverJoint(canvas, tankFeedC[1], h, t + 1.4f);
    }

    private static void DrawFlowPath(
        SKCanvas canvas,
        SKPoint[] points,
        float t,
        SKColor glowColor,
        SKColor coreColor,
        float conduitWidth = 18f)
    {
        using var path = BuildPath(points);
        float glowWidth = conduitWidth * 0.78f;
        float coreWidth = conduitWidth * 0.39f;

        using var conduit = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = conduitWidth,
            Color = new SKColor(10, 44, 62, 190)
        };
        canvas.DrawPath(path, conduit);

        float phase = -((t * 130f) % 42f);
        float[] pattern = [20f, 11f];

        using var flowGlow = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = glowWidth,
            Color = glowColor.WithAlpha(120),
            PathEffect = SKPathEffect.CreateDash(pattern, phase),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6)
        };
        canvas.DrawPath(path, flowGlow);

        using var flowCore = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = coreWidth,
            Color = coreColor.WithAlpha(245),
            PathEffect = SKPathEffect.CreateDash(pattern, phase)
        };
        canvas.DrawPath(path, flowCore);
    }

    private static void DrawBranchCoverJoint(SKCanvas canvas, SKPoint center, float h, float t)
    {
        float radius = MathF.Max(12f, h * 0.018f);
        float innerRadius = radius * 0.58f;
        float sweep = 65f + (MathF.Sin(t * 2.2f) * 0.5f + 0.5f) * 35f;

        using var shellGlow = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(30, 214, 255, 70),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 6)
        };
        canvas.DrawCircle(center, radius + 4f, shellGlow);

        using var shell = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(13, 31, 56, 245)
        };
        canvas.DrawCircle(center, radius, shell);

        using var ring = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2.5f,
            Color = new SKColor(140, 240, 255, 210)
        };
        canvas.DrawCircle(center, radius, ring);

        using var slot = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = new SKColor(90, 220, 250, 190),
            StrokeCap = SKStrokeCap.Round
        };
        var arcRect = SKRect.Create(center.X - innerRadius, center.Y - innerRadius, innerRadius * 2f, innerRadius * 2f);
        canvas.DrawArc(arcRect, -120f, sweep, false, slot);
        canvas.DrawArc(arcRect, 55f, sweep * 0.72f, false, slot);
    }

    private static SKPath BuildPath(SKPoint[] points)
    {
        var path = new SKPath();
        if (points.Length == 0)
        {
            return path;
        }

        path.MoveTo(points[0]);
        for (int i = 1; i < points.Length; i++)
        {
            path.LineTo(points[i]);
        }

        return path;
    }

    private static void DrawTanks(SKCanvas canvas, float w, float h, float t)
    {
        DrawTank(canvas, w * 0.74f, h * 0.53f, w * 0.13f, h * 0.35f, t, new SKColor(80, 248, 255), new SKColor(8, 102, 185));
        DrawTank(canvas, w * 0.15f, h * 0.47f, w * 0.11f, h * 0.24f, t + 1.2f, new SKColor(98, 228, 255), new SKColor(16, 124, 198));
        DrawTank(canvas, w * 0.89f, h * 0.59f, w * 0.08f, h * 0.17f, t + 0.6f, new SKColor(255, 134, 226), new SKColor(178, 52, 140));
    }

    private static void DrawTank(
        SKCanvas canvas,
        float x,
        float y,
        float tankW,
        float tankH,
        float t,
        SKColor fluidTopColor,
        SKColor fluidBottomColor)
    {
        float labelSize = MathF.Max(12, tankH * 0.18f);
        var rect = new SKRect(x, y, x + tankW, y + tankH);

        using var shell = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 4,
            Color = new SKColor(145, 238, 255, 220)
        };
        canvas.DrawRoundRect(rect, 16, 16, shell);

        using var shellGlow = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 10,
            Color = new SKColor(25, 175, 220, 80),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 8)
        };
        canvas.DrawRoundRect(rect, 16, 16, shellGlow);

        float level = 0.45f + (MathF.Sin(t * 0.9f) * 0.5f + 0.5f) * 0.4f;
        float fluidTop = rect.Bottom - rect.Height * level;
        var fluidRect = new SKRect(rect.Left + 6, fluidTop, rect.Right - 6, rect.Bottom - 6);

        using var fluid = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(fluidRect.MidX, fluidRect.Top),
                new SKPoint(fluidRect.MidX, fluidRect.Bottom),
                new[]
                {
                    fluidTopColor.WithAlpha(220),
                    MixColor(fluidTopColor, fluidBottomColor, 0.45f).WithAlpha(200),
                    fluidBottomColor.WithAlpha(180)
                },
                null,
                SKShaderTileMode.Clamp)
        };
        canvas.DrawRoundRect(fluidRect, 12, 12, fluid);

        float wave = MathF.Sin(t * 3.2f) * 3f;
        using var surface = new SKPaint
        {
            IsAntialias = true,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(210, 255, 255, 220)
        };
        canvas.DrawLine(fluidRect.Left + 8, fluidRect.Top + wave, fluidRect.Right - 8, fluidRect.Top - wave, surface);

        using var gaugeText = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(194, 244, 255, 230),
            TextSize = labelSize,
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold)
        };
        canvas.DrawText($"{(int)(level * 100)}%", rect.MidX, rect.Top - 10, gaugeText);
    }

    private static SKColor MixColor(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        byte r = (byte)(a.Red + (b.Red - a.Red) * t);
        byte g = (byte)(a.Green + (b.Green - a.Green) * t);
        byte bl = (byte)(a.Blue + (b.Blue - a.Blue) * t);
        byte alpha = (byte)(a.Alpha + (b.Alpha - a.Alpha) * t);
        return new SKColor(r, g, bl, alpha);
    }

    private static void DrawFans(SKCanvas canvas, float w, float h, float t)
    {
        DrawFan(canvas, new SKPoint(w * 0.13f, h * 0.84f), MathF.Max(48, h * 0.095f), t * 220f, new SKColor(115, 230, 255, 190));
        DrawFan(canvas, new SKPoint(w * 0.63f, h * 0.24f), MathF.Max(28, h * 0.055f), -t * 180f, new SKColor(255, 156, 236, 185));
        DrawFan(canvas, new SKPoint(w * 0.91f, h * 0.31f), MathF.Max(24, h * 0.048f), t * 260f, new SKColor(152, 246, 255, 170));
    }

    private static void DrawFan(SKCanvas canvas, SKPoint center, float radius, float rotation, SKColor bladeColor)
    {
        float hub = radius * 0.22f;

        using var ringGlow = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 12,
            Color = new SKColor(18, 170, 220, 80),
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 8)
        };
        canvas.DrawCircle(center, radius, ringGlow);

        using var ring = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3,
            Color = new SKColor(130, 230, 250, 220)
        };
        canvas.DrawCircle(center, radius, ring);

        using var bladePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = bladeColor
        };

        for (int i = 0; i < 5; i++)
        {
            float angle = rotation + i * 72f;
            canvas.Save();
            canvas.Translate(center.X, center.Y);
            canvas.RotateDegrees(angle);

            var blade = new SKPath();
            blade.MoveTo(hub * 0.1f, -hub * 0.45f);
            blade.QuadTo(radius * 0.82f, -radius * 0.25f, radius * 0.92f, 0);
            blade.QuadTo(radius * 0.82f, radius * 0.25f, hub * 0.1f, hub * 0.45f);
            blade.Close();

            canvas.DrawPath(blade, bladePaint);
            blade.Dispose();
            canvas.Restore();
        }

        using var hubPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(180, 250, 255, 235)
        };
        canvas.DrawCircle(center, hub, hubPaint);
    }

    private static void DrawHud(SKCanvas canvas, float w, float h, float t)
    {
        using var title = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(173, 245, 255, 240),
            TextSize = MathF.Max(22, h * 0.032f),
            Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold)
        };
        canvas.DrawText("FUTURE FACTORY : ENERGY GRID", w * 0.05f, h * 0.1f, title);

        using var sub = new SKPaint
        {
            IsAntialias = true,
            Color = new SKColor(120, 218, 240, 210),
            TextSize = MathF.Max(14, h * 0.02f),
            Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal)
        };
        canvas.DrawText($"Flow Sync  {((MathF.Sin(t * 0.8f) * 0.5f + 0.5f) * 100f):00.0}%", w * 0.05f, h * 0.14f, sub);
        canvas.DrawText($"Core Temp  {(68f + MathF.Sin(t * 0.4f) * 6f):00.0} C", w * 0.05f, h * 0.17f, sub);

        using var panel = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            Color = new SKColor(80, 196, 226, 160)
        };
        canvas.DrawRoundRect(new SKRect(w * 0.04f, h * 0.05f, w * 0.39f, h * 0.2f), 8, 8, panel);
    }
}


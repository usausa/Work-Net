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
        DrawTank(canvas, w, h, t);
        DrawFan(canvas, w, h, t);
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

        DrawFlowPath(canvas, mainLine, t);
        DrawFlowPath(canvas, branchA, t + 0.8f);
        DrawFlowPath(canvas, branchB, t + 1.3f);

        SKPoint[] joints =
        [
            mainLine[1], mainLine[2], mainLine[3], mainLine[4],
            branchA[1], branchA[2], branchA[3],
            branchB[1], branchB[2]
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
    }

    private static void DrawFlowPath(SKCanvas canvas, SKPoint[] points, float t)
    {
        using var path = BuildPath(points);

        using var conduit = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            StrokeWidth = 18,
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
            StrokeWidth = 14,
            Color = new SKColor(20, 212, 255, 120),
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
            StrokeWidth = 7,
            Color = new SKColor(170, 255, 255, 245),
            PathEffect = SKPathEffect.CreateDash(pattern, phase)
        };
        canvas.DrawPath(path, flowCore);
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

    private static void DrawTank(SKCanvas canvas, float w, float h, float t)
    {
        float tankW = w * 0.16f;
        float tankH = h * 0.35f;
        var rect = new SKRect(w * 0.78f, h * 0.53f, w * 0.78f + tankW, h * 0.53f + tankH);

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
                    new SKColor(80, 248, 255, 220),
                    new SKColor(15, 145, 210, 200),
                    new SKColor(8, 102, 185, 180)
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
            TextSize = MathF.Max(16, h * 0.024f),
            TextAlign = SKTextAlign.Center,
            Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold)
        };
        canvas.DrawText($"{(int)(level * 100)}%", rect.MidX, rect.Top - 10, gaugeText);
    }

    private static void DrawFan(SKCanvas canvas, float w, float h, float t)
    {
        SKPoint center = new(w * 0.17f, h * 0.72f);
        float radius = MathF.Max(48, h * 0.095f);
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

        float rotation = t * 220f;
        using var bladePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = new SKColor(115, 230, 255, 190)
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

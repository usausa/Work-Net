using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp.Views.Desktop;

namespace MechHud;

public partial class MainWindow : Window
{
    private readonly MechSim _sim = new();
    private readonly MechRenderer _renderer = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private double _last;

    public MainWindow()
    {
        InitializeComponent();
        CompositionTarget.Rendering += (_, _) => HudCanvas.InvalidateVisual();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        double now = _clock.Elapsed.TotalSeconds;
        float dt = (float)Math.Min(now - _last, 0.1);
        _last = now;

        _sim.Update(dt);
        _renderer.Render(e.Surface.Canvas, e.Info.Width, e.Info.Height, _sim);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            Close();
        }
        else if (e.Key == Key.F11)
        {
            bool isFull = WindowStyle == WindowStyle.None;
            WindowStyle = isFull ? WindowStyle.SingleBorderWindow : WindowStyle.None;
            WindowState = isFull ? WindowState.Normal : WindowState.Maximized;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _renderer.Dispose();
    }
}

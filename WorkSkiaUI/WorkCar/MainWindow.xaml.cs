using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SkiaSharp.Views.Desktop;

namespace WorkCar;

public partial class MainWindow : Window
{
    private readonly VehicleSimulator _sim = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastFrame;
    private bool _paused;

    public MainWindow()
    {
        InitializeComponent();
        CompositionTarget.Rendering += OnFrame;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        Closed += (_, _) => CompositionTarget.Rendering -= OnFrame;
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        var dt = (float)(now - _lastFrame).TotalSeconds;
        _lastFrame = now;
        if (dt <= 0f || dt > 0.25f) dt = 1f / 60f;

        if (!_paused) _sim.Update(dt);
        HudCanvas.InvalidateVisual();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space: _sim.BoostRequested = true; break;
            case Key.P: if (!e.IsRepeat) _paused = !_paused; break;
            case Key.Escape: Close(); break;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space) _sim.BoostRequested = false;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        HudRenderer.Render(e.Surface.Canvas, e.Info.Width, e.Info.Height,
            _sim, (float)_clock.Elapsed.TotalSeconds, _paused);
    }
}

using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using EnergyLineHmi.Rendering;
using EnergyLineHmi.Simulation;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace EnergyLineHmi;

public partial class MainWindow : Window
{
    readonly PlantSimulation _sim = new();
    readonly HmiRenderer _renderer = new();
    readonly Stopwatch _clock = Stopwatch.StartNew();
    double _lastT;
    string? _hoverId;

    public MainWindow()
    {
        InitializeComponent();
        // 毎フレーム再描画（描画時にシミュレーションも進める）
        CompositionTarget.Rendering += (_, _) => Canvas.InvalidateVisual();
    }

    void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        double t = _clock.Elapsed.TotalSeconds;
        _sim.Update(Math.Clamp(t - _lastT, 0, 0.1));
        _lastT = t;
        _renderer.Render(e.Surface.Canvas, e.Info, _sim, t, _hoverId);
    }

    /// <summary>マウス位置（DIP）→ ピクセル座標。SKElement のキャンバスは物理ピクセル基準。</summary>
    SKPoint DevicePoint(MouseEventArgs e)
    {
        var p = e.GetPosition(Canvas);
        var m = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice;
        return new SKPoint(
            (float)(p.X * (m?.M11 ?? 1.0)),
            (float)(p.Y * (m?.M22 ?? 1.0)));
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
        _hoverId = _renderer.HitTest(DevicePoint(e), _sim);
        Cursor = _hoverId != null ? Cursors.Hand : Cursors.Arrow;
    }

    void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var id = _renderer.HitTest(DevicePoint(e), _sim);
        if (id != null) _sim.Toggle(id);
    }

    void OnMouseLeave(object sender, MouseEventArgs e) => _hoverId = null;
}

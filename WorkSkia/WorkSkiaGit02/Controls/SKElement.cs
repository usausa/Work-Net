namespace WorkSkiaGit02.Controls;

using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using SkiaSharp;
using SkiaSharp.Views.Desktop;

public class SKElement : FrameworkElement
{
    private const double BitmapDpi = 96.0;

    private readonly bool designMode;

    private WriteableBitmap? bitmap;

    public SKElement()
    {
        designMode = DesignerProperties.GetIsInDesignMode(this);
    }

    public SKSize CanvasSize { get; private set; }

    [Category("Appearance")]
    public event EventHandler<SKPaintSurfaceEventArgs>? PaintSurface;

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (designMode)
        {
            return;
        }

        if ((Visibility != Visibility.Visible) || (PresentationSource.FromVisual(this) is null))
        {
            return;
        }

        var size = CreateSize(out _, out var scaleX, out var scaleY);
        CanvasSize = size;

        if ((size.Width <= 0) || (size.Height <= 0))
        {
            return;
        }

        var info = new SKImageInfo(size.Width, size.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

        if ((bitmap is null) || (info.Width != bitmap.PixelWidth) || (info.Height != bitmap.PixelHeight))
        {
            bitmap = new WriteableBitmap(info.Width, size.Height, BitmapDpi * scaleX, BitmapDpi * scaleY, PixelFormats.Pbgra32, null);
        }

        bitmap.Lock();
        using (var surface = SKSurface.Create(info, bitmap.BackBuffer, bitmap.BackBufferStride))
        {
            OnPaintSurface(new SKPaintSurfaceEventArgs(surface, info, info));
        }

        bitmap.AddDirtyRect(new Int32Rect(0, 0, info.Width, size.Height));
        bitmap.Unlock();
        drawingContext.DrawImage(bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
    }

    protected virtual void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        PaintSurface?.Invoke(this, e);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        InvalidateVisual();
    }

    private SKSizeI CreateSize(out SKSizeI unscaledSize, out float scaleX, out float scaleY)
    {
        unscaledSize = SKSizeI.Empty;
        scaleX = 1.0f;
        scaleY = 1.0f;

        var w = ActualWidth;
        var h = ActualHeight;

        if (!IsPositive(w) || !IsPositive(h))
        {
            return SKSizeI.Empty;
        }

        unscaledSize = new SKSizeI((int)w, (int)h);

        var source = PresentationSource.FromVisual(this);
        if (source is not null)
        {
            var m = source.CompositionTarget.TransformToDevice;
            scaleX = (float)m.M11;
            scaleY = (float)m.M22;
        }

        return new SKSizeI((int)(w * scaleX), (int)(h * scaleY));

        static bool IsPositive(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && (value > 0);
    }
}

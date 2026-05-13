namespace WorkSkiaGitMaui.Controls;

using Microsoft.Maui.Controls;

using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

using WorkSkiaGitMaui.Graph;

public sealed class GraphRowSurface : SKCanvasView
{
    public static readonly BindableProperty RowDataProperty = BindableProperty.Create(
        nameof(RowData),
        typeof(GraphRow),
        typeof(GraphRowSurface),
        defaultValue: null,
        propertyChanged: OnRowDataChanged);

    public GraphRowSurface()
    {
        HeightRequest = GraphRowRenderer.RowHeight;
        IgnorePixelScaling = false;
    }

    public GraphRow? RowData
    {
        get => (GraphRow?)GetValue(RowDataProperty);
        set => SetValue(RowDataProperty, value);
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (RowData is null)
        {
            return;
        }

        canvas.Save();
        var scaleX = (float)(e.Info.Width / Width);
        var scaleY = (float)(e.Info.Height / Height);
        if (float.IsFinite(scaleX) && float.IsFinite(scaleY) && scaleX > 0 && scaleY > 0)
        {
            canvas.Scale(scaleX, scaleY);
        }
        GraphRowRenderer.Render(canvas, RowData);
        canvas.Restore();
    }

    private static void OnRowDataChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var element = (GraphRowSurface)bindable;
        if (newValue is GraphRow row)
        {
            element.WidthRequest = GraphRowRenderer.GetCellWidth(row.LaneCount);
        }
        element.InvalidateSurface();
    }
}

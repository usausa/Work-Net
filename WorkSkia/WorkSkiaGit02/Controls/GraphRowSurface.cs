namespace WorkSkiaGit02.Controls;

using System.Windows;

using SkiaSharp.Views.Desktop;

using WorkSkiaGit02.Graph;

public sealed class GraphRowSurface : SKElement
{
    public static readonly DependencyProperty RowDataProperty = DependencyProperty.Register(
        nameof(RowData),
        typeof(GraphRow),
        typeof(GraphRowSurface),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
            OnRowDataChanged));

    public GraphRowSurface()
    {
        Height = GraphRowRenderer.RowHeight;
        PaintSurface += OnPaint;
    }

    public GraphRow? RowData
    {
        get => (GraphRow?)GetValue(RowDataProperty);
        set => SetValue(RowDataProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var laneCount = RowData?.LaneCount ?? 0;
        return new Size(GraphRowRenderer.GetCellWidth(laneCount), GraphRowRenderer.RowHeight);
    }

    private static void OnRowDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var element = (GraphRowSurface)d;
        if (e.NewValue is GraphRow row)
        {
            element.Width = GraphRowRenderer.GetCellWidth(row.LaneCount);
        }
        element.InvalidateVisual();
    }

    private void OnPaint(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        if (RowData is null)
        {
            canvas.Clear();
            return;
        }

        GraphRowRenderer.Render(canvas, RowData);
    }
}

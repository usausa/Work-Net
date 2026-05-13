namespace WorkSkiaGitMaui.Graph;

using SkiaSharp;

public static class GraphRowRenderer
{
    public const float RowHeight = 26f;
    public const float LaneWidth = 18f;
    public const float NodeRadius = 5f;
    public const float MarginLeft = 8f;
    public const float MarginRight = 8f;
    public const float StrokeWidth = 2f;

    private static readonly SKColor[] LaneColors =
    [
        new SKColor(0x1F, 0x77, 0xB4),
        new SKColor(0xFF, 0x7F, 0x0E),
        new SKColor(0x2C, 0xA0, 0x2C),
        new SKColor(0xD6, 0x27, 0x28),
        new SKColor(0x94, 0x67, 0xBD),
        new SKColor(0x8C, 0x56, 0x4B),
        new SKColor(0xE3, 0x77, 0xC2),
        new SKColor(0x7F, 0x7F, 0x7F),
        new SKColor(0xBC, 0xBD, 0x22),
        new SKColor(0x17, 0xBE, 0xCF),
    ];

    public static float GetCellWidth(int laneCount) =>
        MarginLeft + (LaneWidth * System.Math.Max(laneCount, 1)) + MarginRight;

    public static void Render(SKCanvas canvas, GraphRow row)
    {
        canvas.Clear(SKColors.Transparent);

        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = StrokeWidth,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        foreach (var seg in row.Segments)
        {
            strokePaint.Color = LaneColors[seg.ColorIndex % LaneColors.Length];
            DrawSegment(canvas, seg, strokePaint);
        }

        var nodeCenterX = LaneCenterX(row.Lane);
        var nodeCenterY = RowHeight / 2f;
        var nodeColor = LaneColors[row.Lane % LaneColors.Length];

        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = nodeColor,
            IsAntialias = true,
        };
        using var borderPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            Color = SKColors.White,
            IsAntialias = true,
        };

        canvas.DrawCircle(nodeCenterX, nodeCenterY, NodeRadius, fillPaint);
        canvas.DrawCircle(nodeCenterX, nodeCenterY, NodeRadius, borderPaint);
    }

    private static void DrawSegment(SKCanvas canvas, GraphSegment seg, SKPaint paint)
    {
        var laneX = LaneCenterX(seg.Lane);
        var radius = StrokeWidth / 2f;
        switch (seg.Kind)
        {
            case GraphSegmentKind.Vertical:
                canvas.DrawLine(laneX, 0f, laneX, RowHeight, paint);
                break;
            case GraphSegmentKind.HalfVerticalTop:
                canvas.DrawLine(laneX, 0f, laneX, RowHeight / 2f, paint);
                break;
            case GraphSegmentKind.HalfVerticalBottom:
                canvas.DrawLine(laneX, RowHeight / 2f, laneX, RowHeight, paint);
                break;
            case GraphSegmentKind.Diagonal:
                canvas.DrawLine(
                    laneX,
                    RowHeight / 2f,
                    LaneCenterX(seg.ToLane),
                    RowHeight - radius,
                    paint);
                break;
            case GraphSegmentKind.DiagonalBranch:
                canvas.DrawLine(
                    laneX,
                    radius,
                    LaneCenterX(seg.ToLane),
                    RowHeight / 2f,
                    paint);
                break;
            default:
                break;
        }
    }

    private static float LaneCenterX(int lane) => MarginLeft + (lane * LaneWidth) + (LaneWidth / 2f);
}

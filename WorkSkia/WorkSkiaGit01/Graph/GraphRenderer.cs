namespace WorkSkiaGit01.Graph;

using System;
using System.Globalization;

using SkiaSharp;

public sealed class GraphRenderer
{
    private const float RowHeight = 26f;
    private const float LaneWidth = 18f;
    private const float NodeRadius = 5f;
    private const float MarginLeft = 16f;
    private const float MarginTop = 12f;
    private const float MarginRight = 16f;
    private const float MarginBottom = 12f;
    private const float TextGap = 14f;
    private const float BadgeGap = 4f;
    private const float BadgePaddingX = 6f;
    private const float BadgePaddingY = 2f;
    private const float TextSize = 12f;
    private const float MonoTextSize = 12f;

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

    private static readonly SKTypeface MonoTypeface =
        SKTypeface.FromFamilyName("Consolas", SKFontStyle.Normal) ?? SKTypeface.Default;

    private static readonly SKTypeface SansTypeface =
        SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Normal) ?? SKTypeface.Default;

    private static readonly SKTypeface SansBoldTypeface =
        SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold) ?? SKTypeface.Default;

    private readonly GraphLayout layout;

    public GraphRenderer(GraphLayout layout)
    {
        this.layout = layout;

        var laneAreaWidth = MarginLeft + (LaneWidth * Math.Max(layout.LaneCount, 1));
        ContentWidth = laneAreaWidth + TextGap + 720f + MarginRight;
        ContentHeight = MarginTop + (RowHeight * layout.RowCount) + MarginBottom;
    }

    public float ContentWidth { get; }

    public float ContentHeight { get; }

    public void Render(SKCanvas canvas)
    {
        canvas.Clear(SKColors.White);

        using var edgePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
        };

        using var nodeFill = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        using var nodeStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            IsAntialias = true,
            Color = SKColors.White,
        };

        using var textPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = SKColors.Black,
        };

        using var monoFont = new SKFont(MonoTypeface, MonoTextSize);
        using var textFont = new SKFont(SansTypeface, TextSize);
        using var boldFont = new SKFont(SansBoldTypeface, TextSize);

        DrawEdges(canvas, edgePaint);
        DrawNodesAndInfo(canvas, nodeFill, nodeStroke, textPaint, monoFont, textFont, boldFont);
    }

    private SKColor GetLaneColor(int lane) => LaneColors[lane % LaneColors.Length];

    private SKPoint GetCenter(int row, int lane)
    {
        var x = MarginLeft + (lane * LaneWidth) + (LaneWidth / 2f);
        var y = MarginTop + (row * RowHeight) + (RowHeight / 2f);
        return new SKPoint(x, y);
    }

    private void DrawEdges(SKCanvas canvas, SKPaint paint)
    {
        foreach (var edge in layout.Edges)
        {
            var from = GetCenter(edge.FromRow, edge.FromLane);
            var to = GetCenter(edge.ToRow, edge.ToLane);

            paint.Color = GetLaneColor(edge.ToLane);

            if (edge.FromLane == edge.ToLane)
            {
                canvas.DrawLine(from, to, paint);
                continue;
            }

            using var path = new SKPath();
            path.MoveTo(from);

            var c1 = new SKPoint(from.X, from.Y + (RowHeight * 0.2f));
            var c2 = new SKPoint(to.X, from.Y + (RowHeight * 0.8f));
            path.CubicTo(c1, c2, to);

            canvas.DrawPath(path, paint);
        }
    }

    private void DrawNodesAndInfo(
        SKCanvas canvas,
        SKPaint nodeFill,
        SKPaint nodeStroke,
        SKPaint textPaint,
        SKFont monoFont,
        SKFont textFont,
        SKFont boldFont)
    {
        var infoBaseX = MarginLeft + (LaneWidth * Math.Max(layout.LaneCount, 1)) + TextGap;

        foreach (var node in layout.Nodes)
        {
            var center = GetCenter(node.Row, node.Lane);

            nodeFill.Color = GetLaneColor(node.Lane);
            canvas.DrawCircle(center, NodeRadius, nodeFill);
            canvas.DrawCircle(center, NodeRadius, nodeStroke);

            var x = infoBaseX;
            var baseline = center.Y + (TextSize / 2f) - 2f;

            x = DrawRefs(canvas, node, x, center.Y, boldFont);

            textPaint.Color = new SKColor(0x66, 0x66, 0x66);
            canvas.DrawText(node.ShortSha, x, baseline, monoFont, textPaint);
            x += MeasureWidth(monoFont, node.ShortSha) + 8f;

            textPaint.Color = SKColors.Black;
            var summary = TrimToFit(node.Summary, textFont, 460f);
            canvas.DrawText(summary, x, baseline, textFont, textPaint);
            x += MeasureWidth(textFont, summary) + 12f;

            textPaint.Color = new SKColor(0x88, 0x88, 0x88);
            var meta = $"{node.Author}  {node.When.LocalDateTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}";
            canvas.DrawText(meta, x, baseline, textFont, textPaint);
        }
    }

    private static float DrawRefs(SKCanvas canvas, GraphNode node, float x, float centerY, SKFont font)
    {
        if (node.Refs.Count == 0)
        {
            return x;
        }

        using var badgeFill = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        using var badgeStroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true,
            Color = new SKColor(0x33, 0x33, 0x33),
        };
        using var badgeText = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
            Color = SKColors.Black,
        };

        foreach (var graphRef in node.Refs)
        {
            var (fill, textColor) = GetBadgeColors(graphRef.Kind);

            var text = graphRef.Name;
            var width = MeasureWidth(font, text) + (BadgePaddingX * 2);
            var height = TextSize + (BadgePaddingY * 2);
            var rect = new SKRect(x, centerY - (height / 2f), x + width, centerY + (height / 2f));

            badgeFill.Color = fill;
            canvas.DrawRoundRect(rect, 4f, 4f, badgeFill);
            canvas.DrawRoundRect(rect, 4f, 4f, badgeStroke);

            badgeText.Color = textColor;
            var baseline = centerY + (TextSize / 2f) - 2f;
            canvas.DrawText(text, x + BadgePaddingX, baseline, font, badgeText);

            x += width + BadgeGap;
        }

        return x + 4f;
    }

    private static (SKColor Fill, SKColor Text) GetBadgeColors(GraphRefKind kind) => kind switch
    {
        GraphRefKind.Head => (new SKColor(0x21, 0x96, 0xF3), SKColors.White),
        GraphRefKind.LocalBranch => (new SKColor(0xC8, 0xE6, 0xC9), new SKColor(0x1B, 0x5E, 0x20)),
        GraphRefKind.RemoteBranch => (new SKColor(0xFF, 0xE0, 0xB2), new SKColor(0xE6, 0x5C, 0x00)),
        GraphRefKind.Tag => (new SKColor(0xFF, 0xF5, 0x9D), new SKColor(0x82, 0x6F, 0x00)),
        _ => (SKColors.LightGray, SKColors.Black),
    };

    private static float MeasureWidth(SKFont font, string text) => font.MeasureText(text);

    private static string TrimToFit(string text, SKFont font, float maxWidth)
    {
        if (MeasureWidth(font, text) <= maxWidth)
        {
            return text;
        }

        const string Ellipsis = "...";
        var ellipsisWidth = MeasureWidth(font, Ellipsis);
        var lo = 0;
        var hi = text.Length;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            var candidate = text[..mid];
            if (MeasureWidth(font, candidate) + ellipsisWidth <= maxWidth)
            {
                lo = mid;
            }
            else
            {
                hi = mid - 1;
            }
        }
        return text[..lo] + Ellipsis;
    }
}

using SkiaSharp;

namespace WorkImage;

public sealed class DrawWrapper : IDisposable
{
    private readonly SKSurface surface;

    private DrawWrapper(SKSurface surface)
    {
        this.surface = surface;
    }

    public void Dispose()
    {
        surface.Dispose();
    }

    public static async ValueTask<DrawWrapper> CreateAsync(string filename)
    {
        await using var inputStream = File.OpenRead(filename);
        using var original = SKBitmap.Decode(inputStream);

        var image = new SKImageInfo(original.Width, original.Height);
        var surface = SKSurface.Create(image);
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(original, 0, 0);

        return new DrawWrapper(surface);
    }

    public void DrawRectangle(float x, float y, float w, float h, string text = "")
    {
        var paint = new SKPaint
        {
            Color = SKColors.Red,
            StrokeWidth = 1,
            IsStroke = true,
            Style = SKPaintStyle.Stroke
        };

        surface.Canvas.DrawRect(x, y, w, h, paint);

        if (!String.IsNullOrEmpty(text))
        {
            paint.Color = SKColors.White;
            surface.Canvas.DrawText(text, x + 1, y + 1, new SKFont(SKTypeface.Default, 14f), paint);
            paint.Color = SKColors.Black;
            surface.Canvas.DrawText(text, x, y, new SKFont(SKTypeface.Default, 14f), paint);
        }
    }

    public async ValueTask OutputAsync(string filename)
    {
        using var outputImage = surface.Snapshot();
        using var data = outputImage.Encode(SKEncodedImageFormat.Jpeg, 90);
        await using var outputStream = File.OpenWrite(filename);
        data.SaveTo(outputStream);
    }
}

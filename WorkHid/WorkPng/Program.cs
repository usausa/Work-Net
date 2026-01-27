namespace WorkPng;

using SkiaSharp;

public static class Program
{
    public static void Main()
    {
        File.WriteAllBytes("black.png", PngGenerator.CreateFilledImage(480, 1920, 255, 0, 0, 0));
        File.WriteAllBytes("red.png", PngGenerator.CreateFilledImage(480, 1920, 255, 255, 0, 0));
        File.WriteAllBytes("green.png", PngGenerator.CreateFilledImage(480, 1920, 255, 0, 255, 0));
        File.WriteAllBytes("blue.png", PngGenerator.CreateFilledImage(480, 1920, 255, 0, 0, 255));
    }
}


public static class PngGenerator
{
    public static byte[] CreateFilledImage(int width, int height, byte a, byte r, byte g, byte b)
    {
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        var color = new SKColor(r, g, b, a);
        canvas.Clear(color);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}

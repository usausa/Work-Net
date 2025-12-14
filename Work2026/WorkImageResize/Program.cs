namespace WorkImageResize;

using SkiaSharp;
using System.Numerics;

internal static class Program
{
    static void Main()
    {
        // D:\学習データ\face.jpg
        // D:\学習データ\people.jpg
        var useSimd = Vector.IsHardwareAccelerated && Vector<float>.Count >= 4;
        Console.WriteLine($"SIMD: {useSimd}");
        ResizeImage(@"D:\学習データ\people.jpg", @"D:\new_people.jpg", 320, 320);
    }

    private static void ResizeImage(string inputFilename, string outputPath, int newWidth, int newHeight)
    {
        using var inputStream = File.OpenRead(inputFilename);
        using var originalBitmap = SKBitmap.Decode(inputStream);

        var width = originalBitmap.Width;
        var height = originalBitmap.Height;

        // Read
        var rgbData = new byte[width * height * 3];
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = originalBitmap.GetPixel(x, y);
                rgbData[index++] = pixel.Red;
                rgbData[index++] = pixel.Green;
                rgbData[index++] = pixel.Blue;
            }
        }

        // Resize
        var newData = new byte[width * height * 3];
        ResizeBilinear(rgbData, width, height, newData, newWidth, newHeight);

        // Write
        using var newBitmap = new SKBitmap(newHeight, newHeight, SKColorType.Rgba8888, SKAlphaType.Opaque);

        index = 0;
        for (var y = 0; y < newHeight; y++)
        {
            for (var x = 0; x < newWidth; x++)
            {
                var r = newData[index++];
                var g = newData[index++];
                var b = newData[index++];
                newBitmap.SetPixel(x, y, new SKColor(r, g, b));
            }
        }

        using var image = SKImage.FromBitmap(newBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var outputStream = File.OpenWrite(outputPath);
        data.SaveTo(outputStream);
    }

    private static void ResizeBilinear(ReadOnlySpan<byte> source, int srcWidth, int srcHeight, Span<byte> destination, int dstWidth, int dstHeight)
    {
        var xRatio = (float)(srcWidth - 1) / dstWidth;
        var yRatio = (float)(srcHeight - 1) / dstHeight;
        for (var y = 0; y < dstHeight; y++)
        {
            var srcY = y * yRatio;
            var srcYInt = (int)srcY;
            var yDiff = srcY - srcYInt;
            var yDiffInv = 1.0f - yDiff;
            var srcY1 = Math.Min(srcYInt + 1, srcHeight - 1);

            var srcRow0 = srcYInt * srcWidth * 3;
            var srcRow1 = srcY1 * srcWidth * 3;
            var dstRow = y * dstWidth * 3;

            for (var x = 0; x < dstWidth; x++)
            {
                var srcX = x * xRatio;
                var srcXInt = (int)srcX;
                var xDiff = srcX - srcXInt;
                var xDiffInv = 1.0f - xDiff;
                var srcX1 = Math.Min(srcXInt + 1, srcWidth - 1);

                var srcCol0 = srcXInt * 3;
                var srcCol1 = srcX1 * 3;
                var dstIdx = dstRow + x * 3;

                // 4つの近傍ピクセル
                var idx00 = srcRow0 + srcCol0;
                var idx10 = srcRow0 + srcCol1;
                var idx01 = srcRow1 + srcCol0;
                var idx11 = srcRow1 + srcCol1;

                // 重み計算
                var w00 = xDiffInv * yDiffInv;
                var w10 = xDiff * yDiffInv;
                var w01 = xDiffInv * yDiff;
                var w11 = xDiff * yDiff;

                // RGB各チャンネル
                for (var c = 0; c < 3; c++)
                {
                    var val =source[idx00 + c] * w00 +source[idx10 + c] * w10 +source[idx01 + c] * w01 +source[idx11 + c] * w11;
                    destination[dstIdx + c] = (byte)Math.Clamp(val, 0, 255);
                }
            }
        }
    }
}

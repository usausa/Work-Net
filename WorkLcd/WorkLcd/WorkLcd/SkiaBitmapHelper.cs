using SkiaSharp;

namespace WorkLcd;

public static class SkiaBitmapHelper
{
    public static byte[] ToJpegBytes(SKBitmap bitmap, int quality = 95)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var source = ResizeIfNeeded(bitmap);
        using var image = SKImage.FromBitmap(source);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        return data.ToArray();
    }

    public static unsafe byte[] ToRgb565Bytes(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var source = ResizeIfNeeded(bitmap);
        var pixmap = source.PeekPixels() ?? throw new InvalidOperationException("Failed to access bitmap pixels.");

        var width = pixmap.Width;
        var height = pixmap.Height;
        var output = new byte[width * height * 2];

        fixed (byte* outputPtr = output)
        {
            var srcBase = (byte*)pixmap.GetPixels();

            for (var y = 0; y < height; y++)
            {
                var srcRow = srcBase + (y * pixmap.RowBytes);
                var dstRow = outputPtr + (y * width * 2);

                switch (pixmap.ColorType)
                {
                    case SKColorType.Rgb565:
                    {
                        Buffer.MemoryCopy(srcRow, dstRow, width * 2, width * 2);
                        break;
                    }
                    case SKColorType.Rgba8888:
                    {
                        var rowPtr = srcRow;
                        for (var x = 0; x < width; x++)
                        {
                            var r = rowPtr[0];
                            var g = rowPtr[1];
                            var b = rowPtr[2];
                            var rgb565 = (ushort)(((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3));
                            dstRow[0] = (byte)(rgb565 & 0xFF);
                            dstRow[1] = (byte)(rgb565 >> 8);
                            rowPtr += 4;
                            dstRow += 2;
                        }
                        break;
                    }
                    case SKColorType.Bgra8888:
                    {
                        var rowPtr = srcRow;
                        for (var x = 0; x < width; x++)
                        {
                            var b = rowPtr[0];
                            var g = rowPtr[1];
                            var r = rowPtr[2];
                            var rgb565 = (ushort)(((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3));
                            dstRow[0] = (byte)(rgb565 & 0xFF);
                            dstRow[1] = (byte)(rgb565 >> 8);
                            rowPtr += 4;
                            dstRow += 2;
                        }
                        break;
                    }
                    default:
                    {
                        for (var x = 0; x < width; x++)
                        {
                            var c = source.GetPixel(x, y);
                            var rgb565 = (ushort)(((c.Red & 0xF8) << 8) | ((c.Green & 0xFC) << 3) | (c.Blue >> 3));
                            dstRow[0] = (byte)(rgb565 & 0xFF);
                            dstRow[1] = (byte)(rgb565 >> 8);
                            dstRow += 2;
                        }
                        break;
                    }
                }
            }
        }

        return output;
    }

    private static SKBitmap ResizeIfNeeded(SKBitmap bitmap)
    {
        if (bitmap.Width == UsbLcdDevice.Width && bitmap.Height == UsbLcdDevice.Height)
        {
            return bitmap.Copy();
        }

        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
        return bitmap.Resize(new SKImageInfo(UsbLcdDevice.Width, UsbLcdDevice.Height), sampling)
            ?? throw new InvalidOperationException("Failed to resize bitmap.");
    }
}
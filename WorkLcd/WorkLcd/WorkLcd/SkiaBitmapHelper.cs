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

    public static byte[] ToRgb565Bytes(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        using var source = ResizeIfNeeded(bitmap);
        var buffer = new byte[source.Width * source.Height * 2];
        var offset = 0;

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                var rgb565 = (ushort)(
                    ((pixel.Red & 0xF8) << 8) |
                    ((pixel.Green & 0xFC) << 3) |
                    (pixel.Blue >> 3));

                buffer[offset++] = (byte)(rgb565 & 0xFF);
                buffer[offset++] = (byte)(rgb565 >> 8);
            }
        }

        return buffer;
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
namespace WorkImageOnyx;

using System.Runtime.CompilerServices;

public static class ImageResizer
{
    //--------------------------------------------------------------------------------
    // ヘルパー
    //--------------------------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Min(int a, int b) => a < b ? a : b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Clamp(float value)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value > 255)
        {
            return 255;
        }
        return (byte)value;
    }

    //--------------------------------------------------------------------------------
    // スカラー処理
    //--------------------------------------------------------------------------------

    // スカラー処理
    public static void ResizeBilinearScalar(ReadOnlySpan<byte> source, Span<byte> destination, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        var xRatio = (float)(srcWidth - 1) / dstWidth;
        var yRatio = (float)(srcHeight - 1) / dstHeight;

        for (var y = 0; y < dstHeight; y++)
        {
            var srcY = y * yRatio;
            var srcYInt = (int)srcY;
            var yDiff = srcY - srcYInt;
            var yDiffInv = 1.0f - yDiff;
            var srcY1 = Min(srcYInt + 1, srcHeight - 1);

            var srcRow0 = srcYInt * srcWidth * 3;
            var srcRow1 = srcY1 * srcWidth * 3;
            var dstRow = y * dstWidth * 3;

            for (var x = 0; x < dstWidth; x++)
            {
                ProcessPixelScalar(source, destination, x, srcRow0, srcRow1, dstRow, xRatio, yDiff, yDiffInv, srcWidth);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessPixelScalar(ReadOnlySpan<byte> source, Span<byte> dst, int x, int srcRow0, int srcRow1, int dstRow, float xRatio, float yDiff, float yDiffInv, int srcWidth)
    {
        var srcX = x * xRatio;
        var srcXInt = (int)srcX;
        var xDiff = srcX - srcXInt;
        var xDiffInv = 1.0f - xDiff;
        var srcX1 = Min(srcXInt + 1, srcWidth - 1);

        var srcCol0 = srcXInt * 3;
        var srcCol1 = srcX1 * 3;
        var dstIdx = dstRow + x * 3;

        var idx00 = srcRow0 + srcCol0;
        var idx10 = srcRow0 + srcCol1;
        var idx01 = srcRow1 + srcCol0;
        var idx11 = srcRow1 + srcCol1;

        var w00 = xDiffInv * yDiffInv;
        var w10 = xDiff * yDiffInv;
        var w01 = xDiffInv * yDiff;
        var w11 = xDiff * yDiff;

        // R channel
        var valR =
            source[idx00] * w00 +
            source[idx10] * w10 +
            source[idx01] * w01 +
            source[idx11] * w11;
        dst[dstIdx] = Clamp(valR);

        // G channel
        var valG =
            source[idx00 + 1] * w00 +
            source[idx10 + 1] * w10 +
            source[idx01 + 1] * w01 +
            source[idx11 + 1] * w11;
        dst[dstIdx + 1] = Clamp(valG);

        // B channel
        var valB =
            source[idx00 + 2] * w00 +
            source[idx10 + 2] * w10 +
            source[idx01 + 2] * w01 +
            source[idx11 + 2] * w11;
        dst[dstIdx + 2] = Clamp(valB);
    }
}

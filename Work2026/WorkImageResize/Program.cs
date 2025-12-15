namespace WorkImageResize;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using SkiaSharp;

using System.Numerics;
using System.Runtime.CompilerServices;

internal static class Program
{
    static void Main()
    {
        //var useSimd = Vector.IsHardwareAccelerated && Vector<float>.Count >= 4;
        //Console.WriteLine($"SIMD: {useSimd}");

        //var sourceImage = new byte[640 * 480 * 3];
        //var resultBuffer = new byte[320 * 240 * 3];
        //ImageResizer.ResizeBilinearSIMD(sourceImage, resultBuffer, 640, 480, 320, 240);

        //BenchmarkRunner.Run<ResizeBenchmark>();

        // D:\学習データ\face.jpg
        // D:\学習データ\people.jpg
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
        ImageResizer.ResizeBilinearScalar(rgbData, newData, width, height, newWidth, newHeight);

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
}

[MemoryDiagnoser]
public class ResizeBenchmark
{
    private byte[] sourceImage = default!;
    private byte[] resultBuffer = default!;
    private const int SrcWidth = 640;
    private const int SrcHeight = 480;
    private const int DstWidth = 320;
    private const int DstHeight = 240;

    [GlobalSetup]
    public void Setup()
    {
        sourceImage = new byte[SrcWidth * SrcHeight * 3];
        resultBuffer = new byte[DstWidth * DstHeight * 3];
        new Random(42).NextBytes(sourceImage);
    }

    [Benchmark(Baseline = true)]
    public void Resize()
    {
        ImageResizer.ResizeBilinearScalar(sourceImage, resultBuffer, SrcWidth, SrcHeight, DstWidth, DstHeight);
    }

    //[Benchmark]
    //public void ResizeSIMD()
    //{
    //    ImageResizer.ResizeBilinearSIMD(sourceImage, resultBuffer, SrcWidth, SrcHeight, DstWidth, DstHeight);
    //}

    //[Benchmark]
    //public void ResizeSimple()
    //{
    //    ImageResizer.ResizeBilinear(sourceImage, SrcWidth, SrcHeight, resultBuffer, DstWidth, DstHeight);
    //}
}


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
    // リサイズ
    //--------------------------------------------------------------------------------

    //public static void ResizeBilinear(ReadOnlySpan<byte> source, int srcWidth, int srcHeight, Span<byte> destination, int dstWidth, int dstHeight)
    //{
    //    var xRatio = (float)(srcWidth - 1) / dstWidth;
    //    var yRatio = (float)(srcHeight - 1) / dstHeight;
    //    for (var y = 0; y < dstHeight; y++)
    //    {
    //        var srcY = y * yRatio;
    //        var srcYInt = (int)srcY;
    //        var yDiff = srcY - srcYInt;
    //        var yDiffInv = 1.0f - yDiff;
    //        var srcY1 = Min(srcYInt + 1, srcHeight - 1);

    //        var srcRow0 = srcYInt * srcWidth * 3;
    //        var srcRow1 = srcY1 * srcWidth * 3;
    //        var dstRow = y * dstWidth * 3;

    //        for (var x = 0; x < dstWidth; x++)
    //        {
    //            var srcX = x * xRatio;
    //            var srcXInt = (int)srcX;
    //            var xDiff = srcX - srcXInt;
    //            var xDiffInv = 1.0f - xDiff;
    //            var srcX1 = Min(srcXInt + 1, srcWidth - 1);

    //            var srcCol0 = srcXInt * 3;
    //            var srcCol1 = srcX1 * 3;
    //            var dstIdx = dstRow + x * 3;

    //            // 4つの近傍ピクセル
    //            var idx00 = srcRow0 + srcCol0;
    //            var idx10 = srcRow0 + srcCol1;
    //            var idx01 = srcRow1 + srcCol0;
    //            var idx11 = srcRow1 + srcCol1;

    //            // 重み計算
    //            var w00 = xDiffInv * yDiffInv;
    //            var w10 = xDiff * yDiffInv;
    //            var w01 = xDiffInv * yDiff;
    //            var w11 = xDiff * yDiff;

    //            // RGB各チャンネル
    //            for (var c = 0; c < 3; c++)
    //            {
    //                var val = source[idx00 + c] * w00 + source[idx10 + c] * w10 + source[idx01 + c] * w01 + source[idx11 + c] * w11;
    //                destination[dstIdx + c] = Clamp(val);
    //            }
    //        }
    //    }
    //}

    // 自動判定
    //public static void ResizeBilinear(ReadOnlySpan<byte> source, Span<byte> destination, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    //{
    //    if (Vector.IsHardwareAccelerated && Vector<float>.Count >= 4)
    //    {
    //        ResizeBilinearSIMD(source, destination, srcWidth, srcHeight, dstWidth, dstHeight);
    //    }
    //    else
    //    {
    //        ResizeBilinearScalar(source, destination, srcWidth, srcHeight, dstWidth, dstHeight);
    //    }
    //}

    //--------------------------------------------------------------------------------
    // SIMD
    //--------------------------------------------------------------------------------

    public static void ResizeBilinearSIMD(ReadOnlySpan<byte> source, Span<byte> destination, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        var xRatio = (float)(srcWidth - 1) / dstWidth;
        var yRatio = (float)(srcHeight - 1) / dstHeight;
        var vectorSize = Vector<float>.Count;

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

            var simdWidth = (dstWidth / vectorSize) * vectorSize;
            var x = 0;

            // SIMD処理
            for (; x < simdWidth; x += vectorSize)
            {
                ProcessPixelsVector(source, destination, x, vectorSize, srcRow0, srcRow1, dstRow, xRatio, yDiff, yDiffInv, srcWidth);
            }

            // 残りをスカラー処理
            for (; x < dstWidth; x++)
            {
                ProcessPixelScalar(source, destination, x, srcRow0, srcRow1, dstRow, xRatio, yDiff, yDiffInv, srcWidth);
            }
        }
    }

    // SIMD処理
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessPixelsVector(
        ReadOnlySpan<byte> source,
        Span<byte> dst,
        int x,
        int vectorSize,
        int srcRow0, int srcRow1, int dstRow,
        float xRatio, float yDiff, float yDiffInv,
        int srcWidth)
    {
        // X座標の計算用バッファ
        Span<float> xIndices = stackalloc float[vectorSize];
        Span<float> srcXArray = stackalloc float[vectorSize];

        // X座標を準備
        for (var i = 0; i < vectorSize; i++)
        {
            xIndices[i] = x + i;
        }

        // Vector<float>を使用してX座標を計算
        var xIndicesVec = new Vector<float>(xIndices);
        var xRatioVec = new Vector<float>(xRatio);
        var srcXVec = xIndicesVec * xRatioVec;

        srcXVec.CopyTo(srcXArray);

        // 各ピクセルを処理
        for (var i = 0; i < vectorSize; i++)
        {
            ProcessSinglePixelOptimized(source, dst, x + i, srcRow0, srcRow1, dstRow, srcXArray[i], yDiff, yDiffInv, srcWidth);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessSinglePixelOptimized(
        ReadOnlySpan<byte> source, Span<byte> dst, int x,
        int srcRow0, int srcRow1, int dstRow,
        float srcXFloat, float yDiff, float yDiffInv,
        int srcWidth)
    {
        var srcXInt = (int)srcXFloat;
        var xDiff = srcXFloat - srcXInt;
        var xDiffInv = 1.0f - xDiff;
        var srcX1 = Min(srcXInt + 1, srcWidth - 1);

        var srcCol0 = srcXInt * 3;
        var srcCol1 = srcX1 * 3;
        var dstIdx = dstRow + x * 3;

        var idx00 = srcRow0 + srcCol0;
        var idx10 = srcRow0 + srcCol1;
        var idx01 = srcRow1 + srcCol0;
        var idx11 = srcRow1 + srcCol1;

        // 重みを事前計算
        var w00 = xDiffInv * yDiffInv;
        var w10 = xDiff * yDiffInv;
        var w01 = xDiffInv * yDiff;
        var w11 = xDiff * yDiff;

        // RGBを処理（手動でアンロール）
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

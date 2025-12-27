using System.Buffers;

namespace WorkImageAlloc;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Runtime.CompilerServices;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<Benchmark>();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class Benchmark
{
    private readonly byte[] sourceImage = new byte[640 * 480  * 4];

    private readonly DenseTensor<float> tensor = new DenseTensor<float>([1, 3, 240, 320]);

    private readonly int[] Dimensions = [1, 3, 240, 320];

    private readonly float[] buffer = new float[3 * 240 * 320];

    [Benchmark]
    public void TensorNew()
    {
        var length = 3 * 240 * 320;
        for (var i = 0; i < 30; i++)
        {
            var buffer = ArrayPool<float>.Shared.Rent(length);
            try
            {
                _ = new DenseTensor<float>(buffer.AsMemory(0, length), Dimensions);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(buffer);
            }
        }
    }


    [Benchmark]
    public void Resize()
    {
        for (var i = 0; i < 30; i++)
        {
            ResizeBilinearDirectToTensor(sourceImage, tensor, 640, 480, 320, 240);
        }
    }

    //[Benchmark]
    //public void Set1()
    //{
    //    for (var i = 0; i < 30; i++)
    //    {
    //        tensor[0, 0, 0, 0] = 0;
    //        tensor[0, 1, 0, 0] = 0;
    //        tensor[0, 2, 0, 0] = 0;
    //    }
    //}

    //[Benchmark]
    //public void SetSize()
    //{
    //    for (var i = 0; i < 30; i++)
    //    {
    //        for (var y = 0; y < 240; y++)
    //        {
    //            for (var x = 0; x < 320; x++)
    //            {
    //                tensor[0, 0, y, x] = 0;
    //                tensor[0, 1, y, x] = 0;
    //                tensor[0, 2, y, x] = 0;
    //            }
    //        }
    //    }
    //}

    [Benchmark]
    public void SetSizeHalf()
    {
        for (var i = 0; i < 30; i++)
        {
            for (var y = 0; y < 120; y++)
            {
                for (var x = 0; x < 160; x++)
                {
                    tensor[0, 0, y, x] = 0;
                    tensor[0, 1, y, x] = 0;
                    tensor[0, 2, y, x] = 0;
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Min(int a, int b) => a < b ? a : b;

    private static void ResizeBilinearDirectToTensor(ReadOnlySpan<byte> source, DenseTensor<float> tensor, int srcWidth, int srcHeight, int dstWidth, int dstHeight)
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

            var srcRow0 = srcYInt * srcWidth * 4;
            var srcRow1 = srcY1 * srcWidth * 4;

            for (var x = 0; x < dstWidth; x++)
            {
                var srcX = x * xRatio;
                var srcXInt = (int)srcX;
                var xDiff = srcX - srcXInt;
                var xDiffInv = 1.0f - xDiff;
                var srcX1 = Min(srcXInt + 1, srcWidth - 1);

                var srcCol0 = srcXInt * 4;
                var srcCol1 = srcX1 * 4;

                var idx00 = srcRow0 + srcCol0;
                var idx10 = srcRow0 + srcCol1;
                var idx01 = srcRow1 + srcCol0;
                var idx11 = srcRow1 + srcCol1;

                var w00 = xDiffInv * yDiffInv;
                var w10 = xDiff * yDiffInv;
                var w01 = xDiffInv * yDiff;
                var w11 = xDiff * yDiff;

                // R channel
                var r = (source[idx00] * w00) + (source[idx10] * w10) + (source[idx01] * w01) + (source[idx11] * w11);
                tensor[0, 0, y, x] = (r - 127f) / 128f;
                // G channel
                var g = (source[idx00 + 1] * w00) + (source[idx10 + 1] * w10) + (source[idx01 + 1] * w01) + (source[idx11 + 1] * w11);
                tensor[0, 1, y, x] = (g - 127f) / 128f;
                // B channel
                var b = (source[idx00 + 2] * w00) + (source[idx10 + 2] * w10) + (source[idx01 + 2] * w01) + (source[idx11 + 2] * w11);
                tensor[0, 2, y, x] = (b - 127f) / 128f;
            }
        }
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace WorkMemo;

internal readonly record struct ColorCount(byte R, byte G, byte B, long Count);

internal readonly record struct Rgb24(byte R, byte G, byte B);

internal static class Program
{
    private static void Main(string[] args)
    {
        var imagePath = args.Length > 0 ? args[0] : "image.jpg";
        var maxColors = ParsePositiveInt(args, 1, 16);
        var samplingSize = ParsePositiveInt(args, 2, 256);

        using var bitmap = SKBitmap.Decode(imagePath) ?? throw new InvalidOperationException($"画像を読み込めませんでした: {imagePath}");
        using var extractor = new DeterministicAccentColorExtractor(samplingSize);

        var watch = Stopwatch.StartNew();
        var colors = extractor.Extract(bitmap, maxColors);
        watch.Stop();

        var totalCount = colors.Sum(static x => x.Count);

        Console.WriteLine($"Image       : {imagePath}");
        Console.WriteLine($"Size        : {bitmap.Width} x {bitmap.Height}");
        Console.WriteLine($"Sampling    : {extractor.SampleWidth} x {extractor.SampleHeight}");
        Console.WriteLine($"SampleCount : {extractor.SampleCount}");
        Console.WriteLine($"Elapsed     : {watch.ElapsedMilliseconds} ms");
        Console.WriteLine();
        Console.WriteLine("Share(%)\tR\tG\tB");

        foreach (var color in colors.OrderByDescending(static x => x.Count))
        {
            var percentage = totalCount == 0 ? 0d : (double)color.Count / totalCount * 100d;
            Console.WriteLine($"{percentage:F2}\t{color.R}\t{color.G}\t{color.B}");
        }
    }

    private static int ParsePositiveInt(string[] args, int index, int defaultValue)
    {
        if ((args.Length <= index) || !int.TryParse(args[index], out var value) || (value <= 0))
        {
            return defaultValue;
        }

        return value;
    }
}

internal unsafe sealed class DeterministicAccentColorExtractor : IDisposable
{
    private readonly uint* samplingBuffer;
    private readonly int[] samplingWeights;
    private readonly int samplingSize;
    private readonly int samplingCapacity;
    private readonly uint[] sortBuffer;
    private readonly int[] sortWeights;
    private bool disposed;

    public DeterministicAccentColorExtractor(int samplingSize = 256)
    {
        if (samplingSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(samplingSize));
        }

        this.samplingSize = samplingSize;
        samplingCapacity = samplingSize * samplingSize;
        samplingBuffer = (uint*)NativeMemory.Alloc((nuint)samplingCapacity, (nuint)sizeof(uint));
        samplingWeights = new int[samplingCapacity];
        sortBuffer = new uint[samplingCapacity];
        sortWeights = new int[samplingCapacity];
    }

    public int SampleWidth { get; private set; }

    public int SampleHeight { get; private set; }

    public int SampleCount => SampleWidth * SampleHeight;

    public List<ColorCount> Extract(SKBitmap source, int colorCount = 16)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(source);

        if (colorCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(colorCount));
        }

        SamplePixelsWithImportance(source);
        return QuantizeSampledBuffer(colorCount);
    }

    private void SamplePixelsWithImportance(SKBitmap source)
    {
        if ((source.ColorType != SKColorType.Bgra8888) && (source.ColorType != SKColorType.Rgba8888))
        {
            throw new NotSupportedException($"未対応のSKColorTypeです: {source.ColorType}");
        }

        var srcWidth = source.Width;
        var srcHeight = source.Height;
        SampleWidth = Math.Min(samplingSize, srcWidth);
        SampleHeight = Math.Min(samplingSize, srcHeight);

        if ((SampleWidth == 0) || (SampleHeight == 0))
        {
            return;
        }

        var rowBytes = source.RowBytes;
        var basePointer = (byte*)source.GetPixels();
        if (basePointer is null)
        {
            throw new InvalidOperationException("画像ピクセルの取得に失敗しました。");
        }

        if (source.ColorType == SKColorType.Bgra8888)
        {
            SampleBgraPixels(basePointer, rowBytes, srcWidth, srcHeight);
            return;
        }

        SampleRgbaPixels(basePointer, rowBytes, srcWidth, srcHeight);
    }

    private void SampleBgraPixels(byte* basePointer, int rowBytes, int srcWidth, int srcHeight)
    {
        for (var ySample = 0; ySample < SampleHeight; ySample++)
        {
            var srcYStart = ySample * srcHeight / SampleHeight;
            var srcYEnd = Math.Max(srcYStart + 1, ((ySample + 1) * srcHeight) / SampleHeight);
            var yStep = Math.Max(1, (srcYEnd - srcYStart) / 2);

            for (var xSample = 0; xSample < SampleWidth; xSample++)
            {
                var srcXStart = xSample * srcWidth / SampleWidth;
                var srcXEnd = Math.Max(srcXStart + 1, ((xSample + 1) * srcWidth) / SampleWidth);
                var xStep = Math.Max(1, (srcXEnd - srcXStart) / 2);
                var sampleIndex = (ySample * SampleWidth) + xSample;
                samplingWeights[sampleIndex] = (srcXEnd - srcXStart) * (srcYEnd - srcYStart);

                uint bestPixel = 0;
                var maxSaturation = -1;

                for (var y = srcYStart; y < srcYEnd; y += yStep)
                {
                    var row = basePointer + (y * rowBytes);
                    for (var x = srcXStart; x < srcXEnd; x += xStep)
                    {
                        var pixel = row + (x * 4);
                        var b = pixel[0];
                        var g = pixel[1];
                        var r = pixel[2];
                        var saturation = GetSaturation(r, g, b);
                        if (saturation <= maxSaturation)
                        {
                            continue;
                        }

                        maxSaturation = saturation;
                        bestPixel = PackRgb(r, g, b);
                        if (saturation == 255)
                        {
                            goto WriteBgraPixel;
                        }
                    }
                }

            WriteBgraPixel:
                samplingBuffer[sampleIndex] = bestPixel;
            }
        }
    }

    private void SampleRgbaPixels(byte* basePointer, int rowBytes, int srcWidth, int srcHeight)
    {
        for (var ySample = 0; ySample < SampleHeight; ySample++)
        {
            var srcYStart = ySample * srcHeight / SampleHeight;
            var srcYEnd = Math.Max(srcYStart + 1, ((ySample + 1) * srcHeight) / SampleHeight);
            var yStep = Math.Max(1, (srcYEnd - srcYStart) / 2);

            for (var xSample = 0; xSample < SampleWidth; xSample++)
            {
                var srcXStart = xSample * srcWidth / SampleWidth;
                var srcXEnd = Math.Max(srcXStart + 1, ((xSample + 1) * srcWidth) / SampleWidth);
                var xStep = Math.Max(1, (srcXEnd - srcXStart) / 2);
                var sampleIndex = (ySample * SampleWidth) + xSample;
                samplingWeights[sampleIndex] = (srcXEnd - srcXStart) * (srcYEnd - srcYStart);

                uint bestPixel = 0;
                var maxSaturation = -1;

                for (var y = srcYStart; y < srcYEnd; y += yStep)
                {
                    var row = basePointer + (y * rowBytes);
                    for (var x = srcXStart; x < srcXEnd; x += xStep)
                    {
                        var pixel = row + (x * 4);
                        var r = pixel[0];
                        var g = pixel[1];
                        var b = pixel[2];
                        var saturation = GetSaturation(r, g, b);
                        if (saturation <= maxSaturation)
                        {
                            continue;
                        }

                        maxSaturation = saturation;
                        bestPixel = PackRgb(r, g, b);
                        if (saturation == 255)
                        {
                            goto WriteRgbaPixel;
                        }
                    }
                }

            WriteRgbaPixel:
                samplingBuffer[sampleIndex] = bestPixel;
            }
        }
    }

    private List<ColorCount> QuantizeSampledBuffer(int maxColors)
    {
        var sampleCount = SampleCount;
        if (sampleCount == 0)
        {
            return [];
        }

        for (var i = 0; i < sampleCount; i++)
        {
            sortBuffer[i] = samplingBuffer[i];
            sortWeights[i] = samplingWeights[i];
        }

        var initialBox = new ColorBox(0, sampleCount);
        UpdateBoxStats(ref initialBox);

        var boxes = new List<ColorBox>(Math.Min(maxColors, sampleCount))
        {
            initialBox
        };

        while (boxes.Count < maxColors)
        {
            var targetIndex = -1;
            long maxPriority = -1;

            for (var i = 0; i < boxes.Count; i++)
            {
                if ((boxes[i].Length > 1) && (boxes[i].Priority > maxPriority))
                {
                    maxPriority = boxes[i].Priority;
                    targetIndex = i;
                }
            }

            if (targetIndex < 0)
            {
                break;
            }

            var target = boxes[targetIndex];
            boxes.RemoveAt(targetIndex);

            SortRange(target.Start, target.Length, target.SortChannel);

            var leftLength = target.Length / 2;
            var rightLength = target.Length - leftLength;
            if ((leftLength == 0) || (rightLength == 0))
            {
                boxes.Add(target);
                break;
            }

            var left = new ColorBox(target.Start, leftLength);
            var right = new ColorBox(target.Start + leftLength, rightLength);
            UpdateBoxStats(ref left);
            UpdateBoxStats(ref right);
            boxes.Add(left);
            boxes.Add(right);
        }

        var merged = new Dictionary<uint, long>();
        foreach (var box in boxes)
        {
            var packed = PackRgb(box.Average.R, box.Average.G, box.Average.B);
            ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(merged, packed, out _);
            count += box.Weight;
        }

        return merged
            .Select(static pair => new ColorCount(
                (byte)(pair.Key >> 16),
                (byte)(pair.Key >> 8),
                (byte)pair.Key,
                pair.Value))
            .OrderByDescending(static x => x.Count)
            .ThenBy(static x => x.R)
            .ThenBy(static x => x.G)
            .ThenBy(static x => x.B)
            .ToList();
    }

    private void UpdateBoxStats(ref ColorBox box)
    {
        byte minR = byte.MaxValue;
        byte minG = byte.MaxValue;
        byte minB = byte.MaxValue;
        byte maxR = byte.MinValue;
        byte maxG = byte.MinValue;
        byte maxB = byte.MinValue;
        long sumR = 0;
        long sumG = 0;
        long sumB = 0;
        long totalWeight = 0;

        for (var i = box.Start; i < box.Start + box.Length; i++)
        {
            var color = sortBuffer[i];
            var weight = sortWeights[i];
            var r = (byte)(color >> 16);
            var g = (byte)(color >> 8);
            var b = (byte)color;

            if (r < minR)
            {
                minR = r;
            }

            if (g < minG)
            {
                minG = g;
            }

            if (b < minB)
            {
                minB = b;
            }

            if (r > maxR)
            {
                maxR = r;
            }

            if (g > maxG)
            {
                maxG = g;
            }

            if (b > maxB)
            {
                maxB = b;
            }

            sumR += (long)r * weight;
            sumG += (long)g * weight;
            sumB += (long)b * weight;
            totalWeight += weight;
        }

        box.MinR = minR;
        box.MinG = minG;
        box.MinB = minB;
        box.MaxR = maxR;
        box.MaxG = maxG;
        box.MaxB = maxB;

        var rangeR = maxR - minR;
        var rangeG = maxG - minG;
        var rangeB = maxB - minB;
        box.SortChannel = SelectSortChannel(rangeR, rangeG, rangeB);
        box.Weight = totalWeight;
        box.Priority = (((long)rangeR + 1) * ((long)rangeG + 1) * ((long)rangeB + 1) << 16) + totalWeight;
        box.Average = new Rgb24(
            (byte)(sumR / totalWeight),
            (byte)(sumG / totalWeight),
            (byte)(sumB / totalWeight));
    }

    private void SortRange(int start, int length, byte channel)
    {
        Array.Sort(sortBuffer, sortWeights, start, length, channel switch
        {
            0 => PackedColorComparers.ByRed,
            1 => PackedColorComparers.ByGreen,
            _ => PackedColorComparers.ByBlue
        });
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        NativeMemory.Free(samplingBuffer);
        disposed = true;
    }

    private static byte SelectSortChannel(int rangeR, int rangeG, int rangeB)
    {
        if ((rangeR >= rangeG) && (rangeR >= rangeB))
        {
            return 0;
        }

        if (rangeG >= rangeB)
        {
            return 1;
        }

        return 2;
    }

    private static int GetSaturation(byte r, byte g, byte b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        return max - min;
    }

    private static uint PackRgb(byte r, byte g, byte b) => ((uint)r << 16) | ((uint)g << 8) | b;
}

internal struct ColorBox(int start, int length)
{
    public int Start { get; set; } = start;

    public int Length { get; set; } = length;

    public byte MinR { get; set; }

    public byte MinG { get; set; }

    public byte MinB { get; set; }

    public byte MaxR { get; set; }

    public byte MaxG { get; set; }

    public byte MaxB { get; set; }

    public byte SortChannel { get; set; }

    public long Priority { get; set; }

    public long Weight { get; set; }

    public Rgb24 Average { get; set; }
}

internal static class PackedColorComparers
{
    public static IComparer<uint> ByRed { get; } = Comparer<uint>.Create(static (x, y) =>
    {
        var xr = (byte)(x >> 16);
        var yr = (byte)(y >> 16);
        var result = xr.CompareTo(yr);
        if (result != 0)
        {
            return result;
        }

        var xg = (byte)(x >> 8);
        var yg = (byte)(y >> 8);
        result = xg.CompareTo(yg);
        if (result != 0)
        {
            return result;
        }

        var xb = (byte)x;
        var yb = (byte)y;
        result = xb.CompareTo(yb);
        return result != 0 ? result : x.CompareTo(y);
    });

    public static IComparer<uint> ByGreen { get; } = Comparer<uint>.Create(static (x, y) =>
    {
        var xg = (byte)(x >> 8);
        var yg = (byte)(y >> 8);
        var result = xg.CompareTo(yg);
        if (result != 0)
        {
            return result;
        }

        var xr = (byte)(x >> 16);
        var yr = (byte)(y >> 16);
        result = xr.CompareTo(yr);
        if (result != 0)
        {
            return result;
        }

        var xb = (byte)x;
        var yb = (byte)y;
        result = xb.CompareTo(yb);
        return result != 0 ? result : x.CompareTo(y);
    });

    public static IComparer<uint> ByBlue { get; } = Comparer<uint>.Create(static (x, y) =>
    {
        var xb = (byte)x;
        var yb = (byte)y;
        var result = xb.CompareTo(yb);
        if (result != 0)
        {
            return result;
        }

        var xr = (byte)(x >> 16);
        var yr = (byte)(y >> 16);
        result = xr.CompareTo(yr);
        if (result != 0)
        {
            return result;
        }

        var xg = (byte)(x >> 8);
        var yg = (byte)(y >> 8);
        result = xg.CompareTo(yg);
        return result != 0 ? result : x.CompareTo(y);
    });
}

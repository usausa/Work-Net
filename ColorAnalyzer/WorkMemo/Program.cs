using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace WorkMemo;

internal readonly record struct ColorCount(byte R, byte G, byte B, long Count);

internal readonly record struct Rgb24(byte R, byte G, byte B);

internal readonly record struct ExtractionOptions(
    string ImagePath,
    int MaxColors,
    int SamplingSize,
    double MinSharePercentage,
    double MergeDistance);

internal static class Program
{
    private const int NoCluster = -1;

    private static void Main(string[] args)
    {
        var options = ParseArguments(args);

        using var bitmap = SKBitmap.Decode(options.ImagePath) ?? throw new InvalidOperationException($"画像を読み込めませんでした: {options.ImagePath}");
        using var extractor = new DeterministicAccentColorExtractor(options.SamplingSize);

        var watch = Stopwatch.StartNew();
        var colors = extractor.Extract(bitmap, options.MaxColors);
        colors = MergeSimilarColors(colors, options.MergeDistance);
        watch.Stop();

        var totalCount = SumCounts(colors);
        var filteredColors = FilterByMinShare(colors, totalCount, options.MinSharePercentage);
        var retainedCount = SumCounts(filteredColors);

        Console.WriteLine($"Image       : {options.ImagePath}");
        Console.WriteLine($"Size        : {bitmap.Width} x {bitmap.Height}");
        Console.WriteLine($"Sampling    : {extractor.SampleWidth} x {extractor.SampleHeight}");
        Console.WriteLine($"SampleCount : {extractor.SampleCount}");
        Console.WriteLine($"MinShare    : {options.MinSharePercentage.ToString("F2", CultureInfo.InvariantCulture)} %");
        Console.WriteLine($"MergeDist   : {options.MergeDistance.ToString("F2", CultureInfo.InvariantCulture)}");
        Console.WriteLine($"Retained    : {(totalCount == 0 ? 0d : (double)retainedCount / totalCount * 100d):F2} %");
        Console.WriteLine($"Elapsed     : {watch.ElapsedMilliseconds} ms");
        Console.WriteLine();
        Console.WriteLine("Share(%)\tR\tG\tB");

        for (var i = 0; i < filteredColors.Count; i++)
        {
            var color = filteredColors[i];
            var percentage = totalCount == 0 ? 0d : (double)color.Count / totalCount * 100d;
            Console.WriteLine($"{percentage:F2}\t{color.R}\t{color.G}\t{color.B}");
        }
    }

    private static ExtractionOptions ParseArguments(string[] args)
    {
        var imagePath = "image.jpg";
        var maxColors = 16;
        var samplingSize = 256;
        var minSharePercentage = 0d;
        // `rgb.png` のようなベタ塗り画像は 8 ～ 16 程度から調整。
        // 自然画像は 16 ～ 32 程度から調整。
        var mergeDistance = 0d;
        var positionalIndex = 0;

        for (var i = 0; i < args.Length; i++)
        {
            if (TryReadOption(args, ref i, "--min-share", out var minShareValue))
            {
                minSharePercentage = ParseNonNegativeDouble(minShareValue, "--min-share");
                continue;
            }

            if (TryReadOption(args, ref i, "--merge-distance", out var mergeDistanceValue))
            {
                mergeDistance = ParseNonNegativeDouble(mergeDistanceValue, "--merge-distance");
                continue;
            }

            switch (positionalIndex)
            {
                case 0:
                    imagePath = args[i];
                    break;
                case 1:
                    maxColors = ParsePositiveInt(args[i], maxColors);
                    break;
                case 2:
                    samplingSize = ParsePositiveInt(args[i], samplingSize);
                    break;
            }

            positionalIndex++;
        }

        return new ExtractionOptions(imagePath, maxColors, samplingSize, minSharePercentage, mergeDistance);
    }

    private static List<ColorCount> MergeSimilarColors(List<ColorCount> colors, double maxDistance)
    {
        if ((colors.Count <= 1) || (maxDistance <= 0d))
        {
            return colors;
        }

        colors.Sort(ColorCountComparers.ByCountDescendingRgbAscending);

        var colorSpan = CollectionsMarshal.AsSpan(colors);
        var clusters = new MergeCluster[colorSpan.Length];
        var bucketHeads = new Dictionary<int, int>(colorSpan.Length);
        var cellSize = Math.Max(1, (int)Math.Ceiling(maxDistance));
        var maxCellIndex = 255 / cellSize;
        var maxDistanceSquared = maxDistance * maxDistance;
        var clusterCount = 0;

        for (var i = 0; i < colorSpan.Length; i++)
        {
            var color = colorSpan[i];
            var rCell = GetCellIndex(color.R, cellSize);
            var gCell = GetCellIndex(color.G, cellSize);
            var bCell = GetCellIndex(color.B, cellSize);
            var bestClusterIndex = NoCluster;

            for (var r = Math.Max(0, rCell - 1); r <= Math.Min(maxCellIndex, rCell + 1); r++)
            {
                for (var g = Math.Max(0, gCell - 1); g <= Math.Min(maxCellIndex, gCell + 1); g++)
                {
                    for (var b = Math.Max(0, bCell - 1); b <= Math.Min(maxCellIndex, bCell + 1); b++)
                    {
                        if (!bucketHeads.TryGetValue(CreateBucketKey(r, g, b), out var clusterIndex))
                        {
                            continue;
                        }

                        while (clusterIndex != NoCluster)
                        {
                            ref var cluster = ref clusters[clusterIndex];
                            if ((GetDistanceSquared(cluster.SeedR, cluster.SeedG, cluster.SeedB, color.R, color.G, color.B) <= maxDistanceSquared)
                                && ((bestClusterIndex == NoCluster) || (clusterIndex < bestClusterIndex)))
                            {
                                bestClusterIndex = clusterIndex;
                            }

                            clusterIndex = cluster.NextBucketIndex;
                        }
                    }
                }
            }

            if (bestClusterIndex != NoCluster)
            {
                ref var cluster = ref clusters[bestClusterIndex];
                cluster.SumR += color.R * color.Count;
                cluster.SumG += color.G * color.Count;
                cluster.SumB += color.B * color.Count;
                cluster.Count += color.Count;
                continue;
            }

            var bucketKey = CreateBucketKey(rCell, gCell, bCell);
            var headIndex = bucketHeads.TryGetValue(bucketKey, out var existingHeadIndex) ? existingHeadIndex : NoCluster;
            clusters[clusterCount] = new MergeCluster(
                color.R,
                color.G,
                color.B,
                color.R * color.Count,
                color.G * color.Count,
                color.B * color.Count,
                color.Count,
                headIndex);
            bucketHeads[bucketKey] = clusterCount;
            clusterCount++;
        }

        var merged = new List<ColorCount>(clusterCount);
        for (var i = 0; i < clusterCount; i++)
        {
            var cluster = clusters[i];
            merged.Add(new ColorCount(
                (byte)Math.Round((double)cluster.SumR / cluster.Count),
                (byte)Math.Round((double)cluster.SumG / cluster.Count),
                (byte)Math.Round((double)cluster.SumB / cluster.Count),
                cluster.Count));
        }

        merged.Sort(ColorCountComparers.ByCountDescendingRgbAscending);
        return merged;
    }

    private static List<ColorCount> FilterByMinShare(List<ColorCount> colors, long totalCount, double minSharePercentage)
    {
        if ((colors.Count == 0) || (totalCount == 0) || (minSharePercentage <= 0d))
        {
            return colors;
        }

        var filtered = new List<ColorCount>(colors.Count);
        for (var i = 0; i < colors.Count; i++)
        {
            var color = colors[i];
            if (((double)color.Count / totalCount * 100d) >= minSharePercentage)
            {
                filtered.Add(color);
            }
        }

        return filtered;
    }

    private static long SumCounts(List<ColorCount> colors)
    {
        long total = 0;
        for (var i = 0; i < colors.Count; i++)
        {
            total += colors[i].Count;
        }

        return total;
    }

    private static bool TryReadOption(string[] args, ref int index, string optionName, out string value)
    {
        ArgumentNullException.ThrowIfNull(args);

        var arg = args[index];
        if (arg.Equals(optionName, StringComparison.OrdinalIgnoreCase))
        {
            if ((index + 1) >= args.Length)
            {
                throw new ArgumentException($"オプション値が指定されていません: {optionName}", nameof(args));
            }

            index++;
            value = args[index];
            return true;
        }

        var prefix = optionName + "=";
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = arg[prefix.Length..];
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static int ParsePositiveInt(string value, int defaultValue)
    {
        if (!int.TryParse(value, out var parsedValue) || (parsedValue <= 0))
        {
            return defaultValue;
        }

        return parsedValue;
    }

    private static double ParseNonNegativeDouble(string value, string optionName)
    {
        if (!double.TryParse(value, CultureInfo.InvariantCulture, out var parsedValue) || (parsedValue < 0d))
        {
            throw new ArgumentException($"オプション値が不正です: {optionName}", optionName);
        }

        return parsedValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetCellIndex(byte value, int cellSize) => value / cellSize;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CreateBucketKey(int rCell, int gCell, int bCell) => (rCell << 16) | (gCell << 8) | bCell;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double GetDistanceSquared(byte xR, byte xG, byte xB, byte yR, byte yG, byte yB)
    {
        var dr = xR - yR;
        var dg = xG - yG;
        var db = xB - yB;
        return (dr * dr) + (dg * dg) + (db * db);
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

        var result = new List<ColorCount>(merged.Count);
        foreach (var pair in merged)
        {
            result.Add(new ColorCount(
                (byte)(pair.Key >> 16),
                (byte)(pair.Key >> 8),
                (byte)pair.Key,
                pair.Value));
        }

        result.Sort(ColorCountComparers.ByCountDescendingRgbAscending);
        return result;
    }

    private void UpdateBoxStats(ref ColorBox box)
    {
        var minR = byte.MaxValue;
        var minG = byte.MaxValue;
        var minB = byte.MaxValue;
        var maxR = byte.MinValue;
        var maxG = byte.MinValue;
        var maxB = byte.MinValue;
        var sumR = 0L;
        var sumG = 0L;
        var sumB = 0L;
        var totalWeight = 0L;
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

        var rangeR = maxR - minR;
        var rangeG = maxG - minG;
        var rangeB = maxB - minB;
        box.SortChannel = SelectSortChannel(rangeR, rangeG, rangeB);
        box.Weight = totalWeight;
        Debug.Assert(totalWeight > 0, "Sample weight must be positive.");
        var priorityWidth = (long)rangeR + 1;
        var priorityHeight = (long)rangeG + 1;
        var priorityDepth = (long)rangeB + 1;
        box.Priority = (((priorityWidth * priorityHeight) * priorityDepth) << 16) + totalWeight;
        box.Average = new Rgb24((byte)(sumR / totalWeight), (byte)(sumG / totalWeight), (byte)(sumB / totalWeight));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSaturation(byte r, byte g, byte b)
    {
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        return max - min;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint PackRgb(byte r, byte g, byte b) => ((uint)r << 16) | ((uint)g << 8) | b;
}

internal struct ColorBox(int start, int length)
{
    public int Start { get; } = start;

    public int Length { get; } = length;

    public byte SortChannel { get; set; }

    public long Priority { get; set; }

    public long Weight { get; set; }

    public Rgb24 Average { get; set; }
}

internal struct MergeCluster(
    byte seedR,
    byte seedG,
    byte seedB,
    long sumR,
    long sumG,
    long sumB,
    long count,
    int nextBucketIndex)
{
    public byte SeedR { get; } = seedR;

    public byte SeedG { get; } = seedG;

    public byte SeedB { get; } = seedB;

    public long SumR { get; set; } = sumR;

    public long SumG { get; set; } = sumG;

    public long SumB { get; set; } = sumB;

    public long Count { get; set; } = count;

    public int NextBucketIndex { get; } = nextBucketIndex;
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

internal static class ColorCountComparers
{
    public static IComparer<ColorCount> ByCountDescendingRgbAscending { get; } = Comparer<ColorCount>.Create(static (x, y) =>
    {
        var result = y.Count.CompareTo(x.Count);
        if (result != 0)
        {
            return result;
        }

        result = x.R.CompareTo(y.R);
        if (result != 0)
        {
            return result;
        }

        result = x.G.CompareTo(y.G);
        if (result != 0)
        {
            return result;
        }

        return x.B.CompareTo(y.B);
    });
}

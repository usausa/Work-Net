using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SkiaSharp;

var bitmap = SKBitmap.Decode(File.ReadAllBytes("image.jpg"));

Debug.WriteLine($"Size: ({bitmap.Width}, {bitmap.Height})");

var raw = ColorAnalyzer.From(new SkiaImageSource(bitmap));
Debug.WriteLine($"Raw count: {raw.ColorCount}");

//var threshold = 0.1;
var threshold = 1.0;

var reducedFromRaw = ColorAnalyzer.From(raw, 5, 6, 5);
Debug.WriteLine($"Reduced1 count: {reducedFromRaw.ColorCount}");
var ret = reducedFromRaw.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

var reducedFromSource = ColorAnalyzer.From(new SkiaImageSource(bitmap), 5, 6, 5);
Debug.WriteLine($"Reduced2 count: {reducedFromSource.ColorCount}");
ret = reducedFromSource.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

var reducedFromRaw2 = ColorAnalyzer.From(reducedFromRaw, 4, 5, 4);
Debug.WriteLine($"Reduced12 count: {reducedFromRaw2.ColorCount}");
ret = reducedFromRaw2.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

var reducedFromSource2 = ColorAnalyzer.From(reducedFromSource, 4, 5, 4);
Debug.WriteLine($"Reduced22 count: {reducedFromSource2.ColorCount}");
ret = reducedFromSource2.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

var reducedFromRaw3 = ColorAnalyzer.From(reducedFromRaw, 3, 4, 3);
Debug.WriteLine($"Reduced13 count: {reducedFromRaw3.ColorCount}");
ret = reducedFromRaw3.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

var reducedFromSource3 = ColorAnalyzer.From(reducedFromSource, 3, 4, 3);
Debug.WriteLine($"Reduced23 count: {reducedFromSource3.ColorCount}");
ret = reducedFromSource3.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

var reducedFromRaw4 = ColorAnalyzer.From(reducedFromRaw, 2, 4, 2);
Debug.WriteLine($"Reduced14 count: {reducedFromRaw4.ColorCount}");
ret = reducedFromRaw4.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

var reducedFromSource4 = ColorAnalyzer.From(reducedFromSource, 2, 4, 2);
Debug.WriteLine($"Reduced24 count: {reducedFromSource4.ColorCount}");
ret = reducedFromSource4.ResolveRank(100, threshold);
Debug.WriteLine($"Ret: {ret.Result} {ret.Rank.Count} {ret.TotalPercentage:F2}");

// TODO 近似色探し、テーブルもインプット/コンストラクタ？、距離

public static class ColorAnalyzerExtensions
{
    public static (bool Result, List<ColorCount> Rank, double TotalPercentage) ResolveRank(this ColorAnalyzer analyzer, int maxCount, double threshold)
    {
        var sumCount = 0;
        var totalCount = analyzer.TotalCount;
        var list = new List<ColorCount>();
        var result = true;
        var p = 0d;
        var c = 0;
        var a = analyzer.GetColorCounts().Count();
        foreach (var color in analyzer.GetColorCounts())
        {
            var percentage = (double)color.Count / totalCount * 100;
            p += percentage;
            c += color.Count;
            //Debug.WriteLine($"{color.Count:D5} {c:D5} {percentage:F2} {p:F2}");

            if (percentage >= threshold)
            {
                sumCount += color.Count;
                if (list.Count >= maxCount)
                {
                    result = false;
                    break;
                }
                list.Add(color);
            }
        }

        //if (result)
        {
            list.Sort(static (x, y) => y.Count - x.Count);
            return (true, list, (double)sumCount / totalCount * 100);
        }

        //return (false, [], 0.0);
    }

}

// TODO 有効色数のカウント、有効を考慮しての件数？ 外？、拡張メソッド？

public interface IImageSource
{
    int Width { get; }
    int Height { get; }

    Color GetColor(int x, int y);
}

public sealed class SkiaImageSource : IImageSource
{
    private readonly SKBitmap bitmap;

    public SkiaImageSource(SKBitmap bitmap)
    {
        this.bitmap = bitmap;
    }
    public int Width => bitmap.Width;

    public int Height => bitmap.Height;

    public Color GetColor(int x, int y)
    {
        var pixel = bitmap.GetPixel(x, y);
        return new Color { R = pixel.Red, G = pixel.Green, B = pixel.Blue };
    }
}


public class ColorAnalyzer
{
    private enum DataType
    {
        Raw,
        Reduced
    }

    private readonly DataType dataType;

    private int[] rawTable = [];
    private ColorCount[] colorTable = [];

    private int RedResolution { get; }
    private int GreenResolution { get; }
    private int BlueResolution { get; }

    public int ColorCount { get; private set; }

    public int TotalCount { get; private set; }

    private ColorAnalyzer(DataType dataType, int redResolution, int greenResolution, int blueResolution)
    {
        this.dataType = dataType;
        RedResolution = redResolution;
        GreenResolution = greenResolution;
        BlueResolution = blueResolution;
    }

    public IEnumerable<ColorCount> GetColorCounts()
    {
        if (dataType == DataType.Raw)
        {
            for (var i = 0; i < rawTable.Length; i++)
            {
                if (rawTable[i] > 0)
                {
                    var r = (i >> 16) & 0xFF;
                    var g = (i >> 8) & 0xFF;
                    var b = i & 0xFF;
                    yield return new ColorCount { Color = new Color { R = (byte)r, G = (byte)g, B = (byte)b }, Count = rawTable[i] };
                }
            }
        }
        else
        {
            for (var i = 0; i < colorTable.Length; i++)
            {
                if (colorTable[i].Count > 0)
                {
                    yield return colorTable[i];
                }
            }
        }
    }

    public static ColorAnalyzer From(IImageSource source, int redResolution = 8, int greenResolution = 8, int blueResolution = 8)
    {
        if ((redResolution <= 0) || (redResolution > 8) || (greenResolution <= 0) || (greenResolution > 8) || (blueResolution <= 0) || (blueResolution > 8))
        {
            throw new ArgumentOutOfRangeException();
        }

        return ((redResolution == 8) && (greenResolution == 8) && (blueResolution == 8))
            ? BuildRawFromSource(source)
            : BuildReducedFromSource(source, redResolution, greenResolution, blueResolution);
    }

    private static ColorAnalyzer BuildRawFromSource(IImageSource source)
    {
        var result = new ColorAnalyzer(DataType.Raw, 8, 8, 8);

        var table = new int[256 * 256 * 256];

        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var color = source.GetColor(x, y);
                var index = (color.R << 16) | (color.G << 8) | color.B;
                table[index]++;
            }
        }

        result.rawTable = table;
        var count = CountRawTable(table);
        result.ColorCount = count;
        result.TotalCount = count;

        return result;
    }

    private static ColorAnalyzer BuildReducedFromSource(IImageSource source, int redResolution = 8, int greenResolution = 8, int blueResolution = 8)
    {
        var result = new ColorAnalyzer(DataType.Reduced, redResolution, greenResolution, blueResolution);

        var table = CreateColorCounts(redResolution, greenResolution, blueResolution);

        var redReduce = 8 - redResolution;
        var greenReduce = 8 - greenResolution;
        var blueReduce = 8 - blueResolution;
        var redShift = greenResolution + blueResolution;
        var greenShift = blueResolution;
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var color = source.GetColor(x, y);
                var index = (color.R >> redReduce << redShift) | (color.G >> greenReduce << greenShift) | (color.B >> blueReduce);
                ref var value = ref table[index];
                value.Count++;
            }
        }

        result.colorTable = table;
        var (colorCount, totalCount) = CountColorTable(table);
        result.ColorCount = colorCount;
        result.TotalCount = totalCount;

        return result;
    }

    public static ColorAnalyzer From(ColorAnalyzer source, int redResolution, int greenResolution, int blueResolution)
    {
        if ((redResolution <= 0) || (redResolution > source.RedResolution) || (greenResolution <= 0) || (greenResolution > source.GreenResolution) || (blueResolution <= 0) || (blueResolution > source.BlueResolution))
        {
            throw new ArgumentOutOfRangeException();
        }

        return source.dataType == DataType.Raw ? BuildReducedFromRaw(source, redResolution, greenResolution, blueResolution) : BuildReducedFromReduced(source, redResolution, greenResolution, blueResolution);
    }

    private static ColorAnalyzer BuildReducedFromRaw(ColorAnalyzer source, int redResolution, int greenResolution, int blueResolution)
    {
        var result = new ColorAnalyzer(DataType.Reduced, redResolution, greenResolution, blueResolution);

        var table = CreateColorCounts(redResolution, greenResolution, blueResolution);

        var redReduce = 8 - redResolution;
        var greenReduce = 8 - greenResolution;
        var blueReduce = 8 - blueResolution;
        var redShift = greenResolution + blueResolution;
        var greenShift = blueResolution;
        for (var i = 0; i < source.rawTable.Length; i++)
        {
            if (source.rawTable[i] > 0)
            {
                var r = (i >> 16) & 0xFF;
                var g = (i >> 8) & 0xFF;
                var b = i & 0xFF;
                var index = (r >> redReduce << redShift) | (g >> greenReduce << greenShift) | (b >> blueReduce);
                ref var value = ref table[index];

                value.Count++;
            }
        }

        result.colorTable = table;
        var (colorCount, totalCount) = CountColorTable(table);
        result.ColorCount = colorCount;
        result.TotalCount = totalCount;

        return result;
    }

    private static ColorAnalyzer BuildReducedFromReduced(ColorAnalyzer source, int redResolution, int greenResolution, int blueResolution)
    {
        var result = new ColorAnalyzer(DataType.Reduced, redResolution, greenResolution, blueResolution);

        var table = CreateColorCounts(redResolution, greenResolution, blueResolution);

        var redReduce = 8 - redResolution;
        var greenReduce = 8 - greenResolution;
        var blueReduce = 8 - blueResolution;
        var redShift = greenResolution + blueResolution;
        var greenShift = blueResolution;

        for (var i = 0; i < source.colorTable.Length; i++)
        {
            ref var sourceValue = ref source.colorTable[i];
            var index = (sourceValue.Color.R >> redReduce << redShift) | (sourceValue.Color.G >> greenReduce << greenShift) | (sourceValue.Color.B >> blueReduce);
            ref var value = ref table[index];

            if (sourceValue.Count > 0)
            {
                if (sourceValue.Count > value.Count)
                {
                    value.Color.R = sourceValue.Color.R;
                    value.Color.G = sourceValue.Color.G;
                    value.Color.B = sourceValue.Color.B;
                }

                value.Count += sourceValue.Count;
            }
        }

        result.colorTable = table;
        var (colorCount, totalCount) = CountColorTable(table);
        result.ColorCount = colorCount;
        result.TotalCount = totalCount;

        return result;
    }


    private static ColorCount[] CreateColorCounts(int redResolution, int greenResolution, int blueResolution)
    {
        var redCount = (1 << redResolution);
        var greenCount = (1 << greenResolution);
        var blueCount = (1 << blueResolution);

        var table = new ColorCount[redCount * greenCount * blueCount];

        var redReduce = 8 - redResolution;
        var greenReduce = 8 - greenResolution;
        var blueReduce = 8 - blueResolution;

        var i = 0;
        for (var b = 0; b < redCount; b++)
        {
            for (var g = 0; g < greenCount; g++)
            {
                for (var r = 0; r < redCount; r++)
                {
                    ref var value = ref table[i];
                    value.Color.R = (byte)(r << redReduce);
                    value.Color.G = (byte)(g << greenReduce);
                    value.Color.B = (byte)(b << blueReduce);
                    i++;
                }
            }
        }

        return table;
    }

    private static int CountRawTable(int[] table)
    {
        var count = 0;
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < table.Length; i++)
        {
            if (table[i] > 0)
            {
                count++;
            }
        }

        return count;
    }

    private static (int ColorCount, int TotalCount) CountColorTable(ColorCount[] table)
    {
        var colorCount = 0;
        var totalCount = 0;
        for (var i = 0; i < table.Length; i++)
        {
            var count = table[i].Count;
            totalCount += count;
            if (count > 0)
            {
                colorCount++;
            }
        }

        return (colorCount, totalCount);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Color
{
    public byte R;
    public byte G;
    public byte B;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ColorCount
{
    public Color Color;
    public int Count;
}

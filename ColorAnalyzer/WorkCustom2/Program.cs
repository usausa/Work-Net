using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using SkiaSharp;

class Program
{
    static void Main(string[] args)
    {
        var imagePath = "image.jpg";
        int topN = 10;

        if (!File.Exists(imagePath))
        {
            Debug.WriteLine($"Error: File not found: {imagePath}");
            return;
        }

        using var input = File.OpenRead(imagePath);
        using var codec = SKCodec.Create(input);
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height);
        using var bitmap = SKBitmap.Decode(codec);

        // Count pixels
        var freq = new Dictionary<uint, long>();
        int width = bitmap.Width, height = bitmap.Height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                SKColor c = bitmap.GetPixel(x, y);
                uint key = ((uint)c.Alpha << 24) | ((uint)c.Red << 16) | ((uint)c.Green << 8) | c.Blue;
                if (freq.ContainsKey(key))
                    freq[key]++;
                else
                    freq[key] = 1;
            }
        }

        long total = width * (long)height;

        // Build known colors list via reflection on SKColors
        var knownColors = typeof(SKColors)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Select(p => new
            {
                Name = p.Name,
                Color = (SKColor)p.GetValue(null)!
            })
            .ToList();

        // Helper: color distance squared
        long DistanceSq(SKColor a, SKColor b)
        {
            long dr = a.Red - b.Red;
            long dg = a.Green - b.Green;
            long db = a.Blue - b.Blue;
            return dr * dr + dg * dg + db * db;
        }

        // Find nearest known
        string FindNearestKnown(SKColor c)
        {
            var best = knownColors
                .OrderBy(k => DistanceSq(c, k.Color))
                .First();
            return best.Name;
        }

        // Top N colors
        var topColors = freq
            .OrderByDescending(kv => kv.Value)
            .Take(topN)
            .ToList();

        Debug.WriteLine($"Analyzed image: {Path.GetFileName(imagePath)}");
        Debug.WriteLine($"Resolution: {width}×{height}, Total pixels: {total}");
        Debug.WriteLine($"Top {topColors.Count} colors:");
        Debug.WriteLine("--------------------------------------------------------------");
        Debug.WriteLine("  # |   HEX    |    RGB           | Count    |  %   | Known");
        Debug.WriteLine("--------------------------------------------------------------");

        int rank = 1;
        foreach (var kv in topColors)
        {
            uint key = kv.Key;
            var c = new SKColor(
                (byte)((key >> 16) & 0xFF),
                (byte)((key >> 8) & 0xFF),
                (byte)(key & 0xFF),
                (byte)((key >> 24) & 0xFF)
            );
            long count = kv.Value;
            double pct = count * 100.0 / total;
            string hex = $"{c.Red:X2}{c.Green:X2}{c.Blue:X2}";
            string known = FindNearestKnown(c);
            Debug.WriteLine($"{rank,2} | #{hex} | ({c.Red,3},{c.Green,3},{c.Blue,3}) | {count,8} | {pct,5:F2}% | {known}");
            rank++;
        }

        Debug.WriteLine("--------------------------------------------------------------");
    }
}

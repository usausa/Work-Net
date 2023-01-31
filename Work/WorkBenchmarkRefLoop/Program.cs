namespace WorkBenchmarkRefLoop;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class Program
{
    public static void Main()
    {
        _ = BenchmarkRunner.Run<Benchmark>();
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
        AddDiagnoser(MemoryDiagnoser.Default, new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
        AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    private const int N = 1000;

    private static readonly byte[] Data = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();

    private static readonly string Hex = HexEncoder.Encode(Enumerable.Range(0, 256).Select(x => (byte)x).ToArray());

    [Benchmark]
    public int CountByte()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            Counter.Count(Data);
        }
        return count;
    }

    [Benchmark]
    public int CountByte2()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            Counter.Count2(Data);
        }
        return count;
    }

    [Benchmark]
    public int CountChar()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            Counter.Count(Hex);
        }
        return count;
    }

    [Benchmark]
    public int CountChar2()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            Counter.Count2(Hex);
        }
        return count;
    }

    // Decode

    [Benchmark]
    public byte[]? Decode()
    {
        var ret = default(byte[]);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.Decode(Hex);
        }
        return ret;
    }

    [Benchmark]
    public byte[]? Decode2()
    {
        var ret = default(byte[]);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.Decode2(Hex);
        }
        return ret;
    }

    [Benchmark]
    public byte[]? Decode2B()
    {
        var ret = default(byte[]);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.Decode2(Hex);
        }
        return ret;
    }

    [Benchmark]
    public unsafe void DecodeTo()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode(Hex, destination);
        }
    }

    [Benchmark]
    public unsafe void DecodeTo2()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode2(Hex, destination);
        }
    }

    [Benchmark]
    public unsafe void DecodeTo2B()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode2B(Hex, destination);
        }
    }

    [Benchmark]
    public string? Encode()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.Encode(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? Encode2()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.Encode2(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? Encode2B()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.Encode2B(Data);
        }
        return ret;
    }

    [Benchmark]
    public unsafe void EncodeTo()
    {
        Span<char> destination = stackalloc char[Data.Length * 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Encode(Data, destination);
        }
    }

    [Benchmark]
    public unsafe void EncodeTo2()
    {
        Span<char> destination = stackalloc char[Data.Length * 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Encode2(Data, destination);
        }
    }

    [Benchmark]
    public unsafe void EncodeTo2B()
    {
        Span<char> destination = stackalloc char[Data.Length * 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Encode2(Data, destination);
        }
    }
}

public static class Counter
{
    public static int Count(ReadOnlySpan<byte> source)
    {
        var count = 0;
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] != 0)
            {
                count++;
            }
        }

        return count;
    }

    public static int Count(ReadOnlySpan<char> source)
    {
        var count = 0;
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] != 0)
            {
                count++;
            }
        }

        return count;
    }

    public static int Count2(ReadOnlySpan<byte> source)
    {
        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);

        var count = 0;
        while (Unsafe.IsAddressLessThan(ref sr, ref end))
        {
            if (sr != 0)
            {
                count++;
            }
            sr = ref Unsafe.Add(ref sr, 1);
        }

        return count;
    }

    public static int Count2(ReadOnlySpan<char> source)
    {
        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);

        var count = 0;
        while (Unsafe.IsAddressLessThan(ref sr, ref end))
        {
            if (sr != 0)
            {
                count++;
            }
            sr = ref Unsafe.Add(ref sr, 1);
        }

        return count;
    }
}

public static class HexEncoder
{
    private static ReadOnlySpan<byte> HexTable => "0123456789ABCDEF"u8;

    [SkipLocalsInit]
    public static unsafe string Encode(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
        {
            return string.Empty;
        }

        var length = source.Length << 1;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);

        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        for (var i = 0; i < source.Length; i++)
        {
            var b = source[i];
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return new string(span);
    }

    public static int Encode(ReadOnlySpan<byte> source, Span<char> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var length = source.Length << 1;
        if (length > destination.Length)
        {
            return -1;
        }

        ref var buffer = ref MemoryMarshal.GetReference(destination);

        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        for (var i = 0; i < source.Length; i++)
        {
            var b = source[i];
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return length;
    }

    [SkipLocalsInit]
    public static unsafe string Encode2(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
        {
            return string.Empty;
        }

        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);

        var length = source.Length << 1;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);

        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        do
        {
            var b = sr;
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
            sr = ref Unsafe.Add(ref sr, 1);
        }
        while (Unsafe.IsAddressLessThan(ref sr, ref end)) ;

        return new string(span);
    }

    [SkipLocalsInit]
    public static unsafe string Encode2B(ReadOnlySpan<byte> source)
    {
        if (source.IsEmpty)
        {
            return string.Empty;
        }

        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);

        var length = source.Length << 1;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);

        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        do
        {
            var b = sr;
            sr = ref Unsafe.Add(ref sr, 1);
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }
        while (Unsafe.IsAddressLessThan(ref sr, ref end));

        return new string(span);
    }

    public static int Encode2(ReadOnlySpan<byte> source, Span<char> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var length = source.Length << 1;
        if (length > destination.Length)
        {
            return -1;
        }

        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);

        ref var buffer = ref MemoryMarshal.GetReference(destination);

        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        do
        {
            var b = sr;
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
            sr = ref Unsafe.Add(ref sr, 1);
        }
        while (Unsafe.IsAddressLessThan(ref sr, ref end));

        return length;
    }

    public static int Encode2B(ReadOnlySpan<byte> source, Span<char> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var length = source.Length << 1;
        if (length > destination.Length)
        {
            return -1;
        }

        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);

        ref var buffer = ref MemoryMarshal.GetReference(destination);

        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        do
        {
            var b = sr;
            sr = ref Unsafe.Add(ref sr, 1);
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }
        while (Unsafe.IsAddressLessThan(ref sr, ref end));

        return length;
    }

    [SkipLocalsInit]
    public static byte[] Decode(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[source.Length >> 1];
        ref var sr = ref MemoryMarshal.GetReference(source);

        for (var i = 0; i < buffer.Length; i++)
        {
            var b = CharToNumber(sr) << 4;
            sr = ref Unsafe.Add(ref sr, 1);
            buffer[i] = (byte)(b + CharToNumber(sr));
            sr = ref Unsafe.Add(ref sr, 1);
        }

        return buffer;
    }

    public static int Decode(ReadOnlySpan<char> source, Span<byte> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var length = source.Length >> 1;
        if (length > destination.Length)
        {
            return -1;
        }

        ref var sr = ref MemoryMarshal.GetReference(source);

        for (var i = 0; i < length; i++)
        {
            var b = CharToNumber(sr) << 4;
            sr = ref Unsafe.Add(ref sr, 1);
            destination[i] = (byte)(b + CharToNumber(sr));
            sr = ref Unsafe.Add(ref sr, 1);
        }

        return length;
    }

    [SkipLocalsInit]
    public static byte[] Decode2(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[source.Length >> 1];
        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);
        ref var dr = ref MemoryMarshal.GetReference(buffer.AsSpan());

        do
        {
            var b = CharToNumber(sr) << 4;
            sr = ref Unsafe.Add(ref sr, 1);
            dr = (byte)(b + CharToNumber(sr));
            dr = ref Unsafe.Add(ref dr, 1);
            sr = ref Unsafe.Add(ref sr, 1);
        } while (Unsafe.IsAddressLessThan(ref sr, ref end));

        return buffer;
    }

    [SkipLocalsInit]
    public static byte[] Decode2B(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[source.Length >> 1];
        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);
        ref var dr = ref MemoryMarshal.GetReference(buffer.AsSpan());

        do
        {
            var b = CharToNumber(sr) << 4;
            sr = ref Unsafe.Add(ref sr, 1);
            dr = (byte)(b + CharToNumber(sr));
            sr = ref Unsafe.Add(ref sr, 1);
            dr = ref Unsafe.Add(ref dr, 1);
        } while (Unsafe.IsAddressLessThan(ref sr, ref end));

        return buffer;
    }

    public static int Decode2(ReadOnlySpan<char> source, Span<byte> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var length = source.Length >> 1;
        if (length > destination.Length)
        {
            return -1;
        }

        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);
        ref var dr = ref MemoryMarshal.GetReference(destination);

        do
        {
            var b = CharToNumber(sr) << 4;
            sr = ref Unsafe.Add(ref sr, 1);
            dr = (byte)(b + CharToNumber(sr));
            dr = ref Unsafe.Add(ref dr, 1);
            sr = ref Unsafe.Add(ref sr, 1);
        } while (Unsafe.IsAddressLessThan(ref sr, ref end));

        return length;
    }

    public static int Decode2B(ReadOnlySpan<char> source, Span<byte> destination)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var length = source.Length >> 1;
        if (length > destination.Length)
        {
            return -1;
        }

        ref var sr = ref MemoryMarshal.GetReference(source);
        ref var end = ref Unsafe.Add(ref sr, source.Length);
        ref var dr = ref MemoryMarshal.GetReference(destination);

        do
        {
            var b = CharToNumber(sr) << 4;
            sr = ref Unsafe.Add(ref sr, 1);
            dr = (byte)(b + CharToNumber(sr));
            sr = ref Unsafe.Add(ref sr, 1);
            dr = ref Unsafe.Add(ref dr, 1);
        } while (Unsafe.IsAddressLessThan(ref sr, ref end));

        return length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CharToNumber(char c)
    {
        if ((c <= '9') && (c >= '0'))
        {
            return c - '0';
        }

        if ((c <= 'F') && (c >= 'A'))
        {
            return c - 'A' + 10;
        }

        if ((c <= 'f') && (c >= 'a'))
        {
            return c - 'a' + 10;
        }

        return 0;
    }
}

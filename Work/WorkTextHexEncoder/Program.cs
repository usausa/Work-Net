#pragma warning disable IDE0046
namespace WorkTextHexEncoder;

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
        _ = AddExporter(MarkdownExporter.GitHub);
        _ = AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        _ = AddDiagnoser(MemoryDiagnoser.Default);
        _ = AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    private const int N = 1000;

    private static readonly byte[] Data = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();

    private static readonly string Hex = HexEncoder.EncodeRef(Enumerable.Range(0, 256).Select(x => (byte)x).ToArray());

    [Benchmark]
    public unsafe void Decode()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode(Hex, destination);
        }
    }

    [Benchmark]
    public unsafe void Decode2()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode2(Hex, destination);
        }
    }

    [Benchmark]
    public unsafe void Decode3()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode3(Hex, destination);
        }
    }

    [Benchmark]
    public unsafe void Decode4()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode4(Hex, destination);
        }
    }

    [Benchmark]
    public unsafe void Decode5()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode5(Hex, destination);
        }
    }

    [Benchmark]
    public unsafe void Decode6()
    {
        Span<byte> destination = stackalloc byte[Hex.Length / 2];
        for (var i = 0; i < N; i++)
        {
            HexEncoder.Decode6(Hex, destination);
        }
    }

    //[Benchmark]
    //public byte[]? Decode()
    //{
    //    var ret = default(byte[]);
    //    for (var i = 0; i < N; i++)
    //    {
    //        ret = HexEncoder.Decode(Hex);
    //    }
    //    return ret;
    //}

    //[Benchmark]
    //public byte[]? Decode2()
    //{
    //    var ret = default(byte[]);
    //    for (var i = 0; i < N; i++)
    //    {
    //        ret = HexEncoder.Decode2(Hex);
    //    }
    //    return ret;
    //}

    //[Benchmark]
    //public byte[]? Decode3()
    //{
    //    var ret = default(byte[]);
    //    for (var i = 0; i < N; i++)
    //    {
    //        ret = HexEncoder.Decode3(Hex);
    //    }
    //    return ret;
    //}

    //[Benchmark]
    //public byte[]? Decode4()
    //{
    //    var ret = default(byte[]);
    //    for (var i = 0; i < N; i++)
    //    {
    //        ret = HexEncoder.Decode4(Hex);
    //    }
    //    return ret;
    //}

    //[Benchmark]
    //public byte[]? Decode5()
    //{
    //    var ret = default(byte[]);
    //    for (var i = 0; i < N; i++)
    //    {
    //        ret = HexEncoder.Decode5(Hex);
    //    }
    //    return ret;
    //}

    [Benchmark]
    public string? EncodeSimple()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeSimple(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? EncodeSimple2()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeSimple2(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? EncodeUnsafe()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeUnsafe(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? EncodeUnsafe2()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeUnsafe2(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? EncodeRef()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeRef(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? EncodeRef2()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeRef2(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? EncodeRef3()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeRef3(Data);
        }
        return ret;
    }

    [Benchmark]
    public string? EncodeRef4()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.EncodeRef4(Data);
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
    public string? Encode3()
    {
        var ret = default(string);
        for (var i = 0; i < N; i++)
        {
            ret = HexEncoder.Encode3(Data);
        }
        return ret;
    }
}

public static class HexEncoder
{
    private static ReadOnlySpan<byte> HexTable => "0123456789ABCDEF"u8;

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeSimple(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        var length = bytes.Length << 1;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var offset = 0;
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            buffer[offset] = (char)Unsafe.Add(ref hex, b >> 4);
            offset++;
            buffer[offset] = (char)Unsafe.Add(ref hex, b & 0xF);
            offset++;
        }

        return new string(buffer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeSimple2(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        var length = bytes.Length << 1;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var offset = 0;
        foreach (var b in bytes)
        {
            buffer[offset] = (char)Unsafe.Add(ref hex, b >> 4);
            offset++;
            buffer[offset] = (char)Unsafe.Add(ref hex, b & 0xF);
            offset++;
        }

        return new string(buffer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeUnsafe(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length << 1;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        fixed (char* pBuffer = buffer)
        {
            var p = pBuffer;
            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                *p = (char)Unsafe.Add(ref hex, b >> 4);
                p++;
                *p = (char)Unsafe.Add(ref hex, b & 0xF);
                p++;
            }

            return new string(pBuffer, 0, length);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeUnsafe2(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length << 1;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        fixed (char* pBuffer = buffer)
        {
            var p = pBuffer;
            foreach (var b in bytes)
            {
                *p = (char)Unsafe.Add(ref hex, b >> 4);
                p++;
                *p = (char)Unsafe.Add(ref hex, b & 0xF);
                p++;
            }

            return new string(pBuffer, 0, length);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeRef(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length << 1;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return new string(span);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeRef2(ReadOnlySpan<byte> bytes)
    {
        var length = bytes.Length << 1;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        foreach (var b in bytes)
        {
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return new string(span);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeRef3(ReadOnlySpan<byte> bytes)
    {
        var sourceLength = bytes.Length;
        var length = sourceLength << 1;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);
        ref var hex = ref MemoryMarshal.GetReference(HexTable);
        ref var b = ref MemoryMarshal.GetReference(bytes);

        do
        {
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
            b = ref Unsafe.Add(ref b, 1);
            sourceLength--;
        } while (sourceLength != 0);

        return new string(span);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static unsafe string EncodeRef4(ReadOnlySpan<byte> bytes)
    {
        var sourceLength = bytes.Length;
        var length = sourceLength << 1;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);
        ref var hex = ref MemoryMarshal.GetReference(HexTable);
        ref var b = ref MemoryMarshal.GetReference(bytes);

        for (var i = 0; i < sourceLength; i++)
        {
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
            b = ref Unsafe.Add(ref b, 1);
        }

        return new string(span);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string Encode2(ReadOnlyMemory<byte> source) =>
        String.Create(source.Length * 2, source, static (span, bytes) =>
        {
            ref var buffer = ref MemoryMarshal.GetReference(span);
            ref var hex = ref MemoryMarshal.GetReference(HexTable);

            foreach (var b in bytes.Span)
            {
                buffer = (char)Unsafe.Add(ref hex, b >> 4);
                buffer = ref Unsafe.Add(ref buffer, 1);
                buffer = (char)Unsafe.Add(ref hex, b & 0xF);
                buffer = ref Unsafe.Add(ref buffer, 1);
            }
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string Encode3(ReadOnlyMemory<byte> source) =>
        String.Create(source.Length * 2, source, static (span, src) =>
        {
            ref var buffer = ref MemoryMarshal.GetReference(span);
            ref var hex = ref MemoryMarshal.GetReference(HexTable);

            var bytes = src.Span;
            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                buffer = (char)Unsafe.Add(ref hex, b >> 4);
                buffer = ref Unsafe.Add(ref buffer, 1);
                buffer = (char)Unsafe.Add(ref hex, b & 0xF);
                buffer = ref Unsafe.Add(ref buffer, 1);
            }
        });

#if NET5_0_OR_GREATER
    [SkipLocalsInit]
#endif
    public static unsafe byte[] Decode(ReadOnlySpan<char> code)
    {
        if (code.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[code.Length >> 1];

        fixed (char* pCode = code)
        fixed (byte* pBuffer = &buffer[0])
        {
            var pb = pBuffer;
            var pc = pCode;
            for (var i = 0; i < buffer.Length; i++)
            {
                var b = CharToNumber(*pc) << 4;
                pc++;
                *pb = (byte)(b + CharToNumber(*pc));
                pc++;
                pb++;
            }
        }

        return buffer;
    }

#if NET5_0_OR_GREATER
    [SkipLocalsInit]
#endif
    public static byte[] Decode4(ReadOnlySpan<char> code)
    {
        if (code.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[code.Length >> 1];
        ref var destination = ref MemoryMarshal.GetReference<byte>(buffer);
        ref var source = ref MemoryMarshal.GetReference(code);

        for (var i = 0; i < buffer.Length; i++)
        {
            var b = CharToNumber(source) << 4;
            source = ref Unsafe.Add(ref source, 1);
            destination = (byte)(b + CharToNumber(source));
            source = ref Unsafe.Add(ref source, 1);
            destination = ref Unsafe.Add(ref destination, 1);
        }

        return buffer;
    }

#if NET5_0_OR_GREATER
    [SkipLocalsInit]
#endif
    public static byte[] Decode5(ReadOnlySpan<char> code)
    {
        if (code.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[code.Length >> 1];
        ref var source = ref MemoryMarshal.GetReference(code);

        for (var i = 0; i < buffer.Length; i++)
        {
            var b = CharToNumber(source) << 4;
            source = ref Unsafe.Add(ref source, 1);
            buffer[i] = (byte)(b + CharToNumber(source));
            source = ref Unsafe.Add(ref source, 1);
        }

        return buffer;
    }

#if NET5_0_OR_GREATER
    [SkipLocalsInit]
#endif
    public static byte[] Decode2(ReadOnlySpan<char> code)
    {
        if (code.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var length = code.Length >> 1;
        var buffer = new byte[length];
        ref var destination = ref MemoryMarshal.GetReference<byte>(buffer);
        ref var source = ref MemoryMarshal.GetReference(code);
        for (var i = 0; i < length; i++)
        {
            var b = CharToNumber(source) << 4;
            source = ref Unsafe.Add(ref source, 1);
            destination = (byte)(b + CharToNumber(source));
            source = ref Unsafe.Add(ref source, 1);
            destination = ref Unsafe.Add(ref destination, 1);
        }

        return buffer;
    }

    [SkipLocalsInit]
    public static byte[] Decode3(ReadOnlySpan<char> code)
    {
        if (code.IsEmpty)
        {
            return Array.Empty<byte>();
        }

        var buffer = new byte[code.Length >> 1];
        ref var destination = ref MemoryMarshal.GetReference<byte>(buffer);

        for (var i = 0; i < code.Length; i += 2)
        {
            var b = CharToNumber(code[i]) << 4;
            destination = (byte)(b + CharToNumber(code[i + 1]));
            destination = ref Unsafe.Add(ref destination, 1);
        }

        return buffer;
    }

    public static unsafe int Decode(ReadOnlySpan<char> code, Span<byte> destination)
    {
        if (code.IsEmpty)
        {
            return 0;
        }

        var length = code.Length >> 1;

        fixed (char* pCode = code)
        fixed (byte* pBuffer = destination)
        {
            var pb = pBuffer;
            var pc = pCode;
            for (var i = 0; i < length; i++)
            {
                var b = CharToNumber(*pc) << 4;
                pc++;
                *pb = (byte)(b + CharToNumber(*pc));
                pc++;
                pb++;
            }
        }

        return length;
    }

    public static int Decode5(ReadOnlySpan<char> code, Span<byte> destination)
    {
        if (code.IsEmpty)
        {
            return 0;
        }

        var length = code.Length >> 1;

        ref var source = ref MemoryMarshal.GetReference(code);
        ref var buffer = ref MemoryMarshal.GetReference(destination);

        for (var i = 0; i < length; i++)
        {
            var b = CharToNumber(source) << 4;
            source = ref Unsafe.Add(ref source, 1);
            buffer = (byte)(b + CharToNumber(source));
            source = ref Unsafe.Add(ref source, 1);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return length;
    }

    public static int Decode2(ReadOnlySpan<char> code, Span<byte> destination)
    {
        if (code.IsEmpty)
        {
            return 0;
        }

        var length = code.Length >> 1;
        ref var source = ref MemoryMarshal.GetReference(code);

        for (var i = 0; i < length; i++)
        {
            var b = CharToNumber(source) << 4;
            source = ref Unsafe.Add(ref source, 1);
            destination[i] = (byte)(b + CharToNumber(source));
            source = ref Unsafe.Add(ref source, 1);
        }

        return length;
    }

    public static int Decode3(ReadOnlySpan<char> code, Span<byte> destination)
    {
        if (code.IsEmpty)
        {
            return 0;
        }

        var length = code.Length >> 1;
        //ref var source = ref MemoryMarshal.GetReference<char>(code);

        for (var i = 0; i < length; i++)
        {
            var index = i * 2;
            var b = CharToNumber(code[index]) << 4;
            destination[i] = (byte)(b + CharToNumber(code[index + 1]));
        }

        return length;
    }

    public static int Decode4(ReadOnlySpan<char> code, Span<byte> destination)
    {
        if (code.IsEmpty)
        {
            return 0;
        }

        var length = code.Length >> 1;
        ref var buffer = ref MemoryMarshal.GetReference(destination);

        for (var i = 0; i < code.Length; i += 2)
        {
            var b = CharToNumber(code[i]) << 4;
            buffer = (byte)(b + CharToNumber(code[i + 1]));
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return length;
    }

    public static int Decode6(ReadOnlySpan<char> code, Span<byte> destination)
    {
        if (code.IsEmpty)
        {
            return 0;
        }

        var length = code.Length >> 1;

        for (var i = 0; i < length; i++)
        {
            var index = i * 2;
            var b = CharToNumber(code[index]) << 4;
            destination[i] = (byte)(b + CharToNumber(code[index + 1]));
        }

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

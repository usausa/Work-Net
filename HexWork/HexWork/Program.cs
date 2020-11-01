namespace HexWork
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

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
            AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private readonly byte[] bytes = new byte[256];

        private string text;

        private string text32;

        private string text16;

        private string text4;

        [GlobalSetup]
        public void Setup()
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte) i;
            }

            text = BitConverter.ToString(bytes).Replace("-", "");
            text32 = BitConverter.ToString(bytes, 0, 32).Replace("-", "");
            text16 = BitConverter.ToString(bytes, 0, 16).Replace("-", "");
            text4 = BitConverter.ToString(bytes, 0, 4).Replace("-", "");
        }

        //// --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe256T1() => HexEncoder.ToHexTableMember(bytes);

        //[Benchmark]
        //public string ToHexSpanUnsafe256T2() => HexEncoder.ToHexTableMember2(bytes);

        //[Benchmark]
        //public string ToHexSpanUnsafe256T3() => HexEncoder.ToHexTableMember3(bytes);

        //// --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe32T1() => HexEncoder.ToHexTableMember(bytes.AsSpan(0, 32));

        //[Benchmark]
        //public string ToHexSpanUnsafe32T2() => HexEncoder.ToHexTableMember2(bytes.AsSpan(0, 32));

        //[Benchmark]
        //public string ToHexSpanUnsafe32T3() => HexEncoder.ToHexTableMember3(bytes.AsSpan(0, 32));

        //// --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe16T1() => HexEncoder.ToHexTableMember(bytes.AsSpan(0, 16));

        //[Benchmark]
        //public string ToHexSpanUnsafe16T2() => HexEncoder.ToHexTableMember2(bytes.AsSpan(0, 16));


        //[Benchmark]
        //public string ToHexSpanUnsafe16T3() => HexEncoder.ToHexTableMember3(bytes.AsSpan(0, 16));

        //// --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe4T1() => HexEncoder.ToHexTableMember(bytes.AsSpan(0, 4));

        //[Benchmark]
        //public string ToHexSpanUnsafe4T2() => HexEncoder.ToHexTableMember2(bytes.AsSpan(0, 4));

        //[Benchmark]
        //public string ToHexSpanUnsafe4T3() => HexEncoder.ToHexTableMember3(bytes.AsSpan(0, 4));

        //// --------------------------------------------------------------------------------

        [Benchmark]
        public byte[] ToBytesT1() => HexEncoder.ToBytes(text.AsSpan());

        //[Benchmark]
        //public byte[] ToBytesT2() => HexEncoder.ToBytes2(text.AsSpan());

        //[Benchmark]
        //public byte[] ToBytesT3() => HexEncoder.ToBytes3(text.AsSpan());

        // --------------------------------------------------------------------------------

        [Benchmark]
        public byte[] ToBytes32T1() => HexEncoder.ToBytes(text32.AsSpan());

        //[Benchmark]
        //public byte[] ToBytes32T2() => HexEncoder.ToBytes2(text32.AsSpan());

        //[Benchmark]
        //public byte[] ToBytes32T3() => HexEncoder.ToBytes3(text32.AsSpan());

        // --------------------------------------------------------------------------------

        [Benchmark]
        public byte[] ToBytes16T1() => HexEncoder.ToBytes(text16.AsSpan());

        //[Benchmark]
        //public byte[] ToBytes16T2() => HexEncoder.ToBytes2(text16.AsSpan());

        //[Benchmark]
        //public byte[] ToBytes16T3() => HexEncoder.ToBytes3(text16.AsSpan());

        // --------------------------------------------------------------------------------

        [Benchmark]
        public byte[] ToBytes4T1() => HexEncoder.ToBytes(text4.AsSpan());

        //[Benchmark]
        //public byte[] ToBytes4T2() => HexEncoder.ToBytes2(text4.AsSpan());

        //[Benchmark]
        //public byte[] ToBytes4T3() => HexEncoder.ToBytes3(text4.AsSpan());
    }

    public static class HexEncoder
    {
        // TODO Unsafe, MemoryMarshal version

        private static byte[] HexTable => new[]
        {
            (byte)'0', (byte)'1', (byte)'2', (byte)'3',
            (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte)'8', (byte)'9', (byte)'A', (byte)'B',
            (byte)'C', (byte)'D', (byte)'E', (byte)'F'
        };

        private static ReadOnlySpan<byte> HexCharactersTable => new[]
        {
            (byte)'0', (byte)'1', (byte)'2', (byte)'3',
            (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte)'8', (byte)'9', (byte)'A', (byte)'B',
            (byte)'C', (byte)'D', (byte)'E', (byte)'F'
        };

        //public static unsafe string ToHexTableMember(ReadOnlySpan<byte> bytes)
        //{
        //    var length = bytes.Length * 2;
        //    var temp = length < 2048 ? stackalloc char[length] : new char[length];
        //    ref var hex = ref MemoryMarshal.GetReference(HexCharactersTable);

        //    for (var i = 0; i < bytes.Length; i++)
        //    {
        //        var offset = i * 2;
        //        var b = bytes[i];
        //        temp[offset] = (char)Unsafe.Add(ref hex, b >> 4);
        //        temp[offset + 1] = (char)Unsafe.Add(ref hex, b & 0xF);
        //    }

        //    return new string(temp);
        //}

        //// Large size faster than 3 (< 3.1)
        //public static unsafe string ToHexTableMember2(ReadOnlySpan<byte> bytes)
        //{
        //    var length = bytes.Length * 2;
        //    var temp = length < 2048 ? stackalloc char[length] : new char[length];

        //    fixed (byte* hex = &HexTable[0])
        //    fixed (char* ptr = temp)
        //    {
        //        char* p = ptr;
        //        for (var i = 0; i < bytes.Length; i++)
        //        {
        //            var b = bytes[i];
        //            *p = (char)hex[b >> 4];
        //            p++;
        //            *p = (char)hex[b & 0xF];
        //            p++;
        //        }
        //    }

        //    return new string(temp);
        //}

        // 採用
        public static unsafe string ToHexTableMember3(ReadOnlySpan<byte> bytes)
        {
            var length = bytes.Length * 2;
            var temp = length < 2048 ? stackalloc char[length] : new char[length];
            ref var hex = ref MemoryMarshal.GetReference(HexCharactersTable);

            fixed (char* ptr = temp)
            {
                char* p = ptr;
                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = bytes[i];
                    *p = (char)Unsafe.Add(ref hex, b >> 4);
                    p++;
                    *p = (char)Unsafe.Add(ref hex, b & 0xF);
                    p++;
                }
            }

            return new string(temp);
        }

        //public static unsafe string ToHexTableMember3(ReadOnlySpan<byte> bytes)
        //{
        //    var length = bytes.Length * 2;
        //    var temp = length < 2048 ? stackalloc char[length] : new char[length];

        //    ref var hex = ref MemoryMarshal.GetReference(HexCharactersTable);
        //    ref var ptr = ref MemoryMarshal.GetReference(temp);

        //    for (var i = 0; i < bytes.Length; i++)
        //    {
        //        var b = bytes[i];
        //        var high = b >> 4;
        //        var low = b & 0xF;
        //        var offset = i * 2;
        //        ref var p1 = ref Unsafe.Add(ref ptr, offset);
        //        p1 = (char)Unsafe.Add(ref hex, high);
        //        ref var p2 = ref Unsafe.Add(ref ptr, offset + 1);
        //        p2 = (char)Unsafe.Add(ref hex, low);
        //    }

        //    return new string(temp);
        //}

        // --------------------------------------------------------------------------------

        public static unsafe byte[] ToBytes(ReadOnlySpan<char> text)
        {
            var bytes = new byte[text.Length >> 1];

            fixed (byte* pBytes = &bytes[0])
            fixed (char* pString = text)
            {
                var pb = pBytes;
                var ps = pString;
                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = CharToNumber(*ps) << 4;
                    ps++;
                    *pb = (byte)(b + CharToNumber(*ps));
                    ps++;
                    pb++;
                }
            }

            return bytes;
        }

        //public static unsafe byte[] ToBytes2(ReadOnlySpan<char> text)
        //{
        //    var bytes = new byte[text.Length >> 1];

        //    fixed (byte* pBytes = &bytes[0])
        //    {
        //        var pb = pBytes;
        //        for (var i = 0; i < text.Length; i++)
        //        {
        //            var c = text[i];
        //            var b = CharToNumber(c) << 4;
        //            *pb = (byte)(b + CharToNumber(c));
        //            pb++;
        //        }
        //    }

        //    return bytes;
        //}

        //public static byte[] ToBytes3(ReadOnlySpan<char> text)
        //{
        //    var bytes = new byte[text.Length >> 1];
        //    Span<byte> span = bytes;
        //    var offset = 0;

        //    for (var i = 0; i < text.Length; i++)
        //    {
        //        var c = text[i];
        //        var b = CharToNumber(c) << 4;
        //        span[offset] = (byte)(b + CharToNumber(c));
        //        offset++;
        //    }

        //    return bytes;
        //}

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

            if ((c <= 'F') && (c >= 'a'))
            {
                return c - 'a' + 10;
            }

            return 0;
        }
    }
}

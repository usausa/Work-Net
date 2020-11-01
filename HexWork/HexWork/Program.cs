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

        [GlobalSetup]
        public void Setup()
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte) i;
            }

            text = BitConverter.ToString(bytes).Replace("-", "");
        }

        // --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe256T1() => HexEncoder.ToHexTableMember(bytes);

        [Benchmark]
        public string ToHexSpanUnsafe256T2() => HexEncoder.ToHexTableMember2(bytes);

        [Benchmark]
        public string ToHexSpanUnsafe256T3() => HexEncoder.ToHexTableMember3(bytes);

        // --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe32T1() => HexEncoder.ToHexTableMember(bytes.AsSpan(0, 32));

        [Benchmark]
        public string ToHexSpanUnsafe32T2() => HexEncoder.ToHexTableMember2(bytes.AsSpan(0, 32));

        [Benchmark]
        public string ToHexSpanUnsafe32T3() => HexEncoder.ToHexTableMember3(bytes.AsSpan(0, 32));

        // --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe16T1() => HexEncoder.ToHexTableMember(bytes.AsSpan(0, 16));

        [Benchmark]
        public string ToHexSpanUnsafe16T2() => HexEncoder.ToHexTableMember2(bytes.AsSpan(0, 16));


        [Benchmark]
        public string ToHexSpanUnsafe16T3() => HexEncoder.ToHexTableMember3(bytes.AsSpan(0, 16));

        // --------------------------------------------------------------------------------

        //[Benchmark]
        //public string ToHexSpanUnsafe4T1() => HexEncoder.ToHexTableMember(bytes.AsSpan(0, 4));

        [Benchmark]
        public string ToHexSpanUnsafe4T2() => HexEncoder.ToHexTableMember2(bytes.AsSpan(0, 4));

        [Benchmark]
        public string ToHexSpanUnsafe4T3() => HexEncoder.ToHexTableMember3(bytes.AsSpan(0, 4));

        // --------------------------------------------------------------------------------

        //[Benchmark]
        //public byte[] ToBytes2() => HexEncoder.ToBytes(text.AsSpan());
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

        //    fixed (char* ptr = temp)
        //    {
        //        char* p = ptr;
        //        for (var i = 0; i < bytes.Length; i++)
        //        {
        //            var b = bytes[i];
        //            var high = b >> 4;  // TODO?
        //            var low = b & 0xF;
        //            *p = (char)HexCharactersTable[high];
        //            p++;
        //            *p = (char)HexCharactersTable[low];
        //            p++;
        //        }
        //    }

        //    return new string(temp);
        //}

        // Large size faster than 3
        public static unsafe string ToHexTableMember2(ReadOnlySpan<byte> bytes)
        {
            var length = bytes.Length * 2;
            var temp = length < 2048 ? stackalloc char[length] : new char[length];

            fixed (byte* hex = &HexTable[0])
            fixed (char* ptr = temp)
            {
                char* p = ptr;
                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = bytes[i];
                    *p = (char)hex[b >> 4];
                    p++;
                    *p = (char)hex[b & 0xF];
                    p++;
                }
            }

            return new string(temp);
        }


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

        //    for (var i = 0; i < bytes.Length; i++)
        //    {
        //        var b = bytes[i];
        //        var high = b >> 4;
        //        var low = b & 0xF;
        //        var offset = i << 1;
        //        temp[offset] = (char)Unsafe.Add(ref hex, high);
        //        temp[offset + 1] = (char)Unsafe.Add(ref hex, low);
        //    }

        //    return new string(temp);
        //}

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

        //public static string ToHexSpan(ReadOnlySpan<byte> bytes)
        //{
        //    var length = bytes.Length * 2;
        //    var temp = length < 2048 ? stackalloc char[length] : new char[length];

        //    Span<char> hex = stackalloc char[]
        //    {
        //        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        //    };

        //    for (var i = 0; i < bytes.Length; i++)
        //    {
        //        var b = bytes[i];
        //        temp[i * 2] = hex[b >> 4];
        //        temp[i * 2 + 1] = hex[b & 0xF];
        //    }

        //    return new string(temp);
        //}

        //public static unsafe string ToHexSpanUnsafe(ReadOnlySpan<byte> bytes)
        //{
        //    var length = bytes.Length * 2;
        //    var temp = length < 2048 ? stackalloc char[length] : new char[length];

        //    Span<char> hex = stackalloc char[]
        //    {
        //        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        //    };

        //    // TODO ?
        //    fixed (char* ptr = temp)
        //    {
        //        char* p = ptr;
        //        for (var i = 0; i < bytes.Length; i++)
        //        {
        //            var b = bytes[i];
        //            *p = hex[b >> 4];
        //            p++;
        //            *p = hex[b & 0xF];
        //            p++;
        //        }
        //    }

        //    return new string(temp);
        //}

        //public static unsafe string ToHexSpanUnsafe2(ReadOnlySpan<byte> bytes)
        //{
        //    var length = bytes.Length * 2;
        //    var temp = length < 2048 ? stackalloc char[length] : new char[length];

        //    Span<char> hex = stackalloc char[]
        //    {
        //        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
        //    };

        //    fixed (char* pTemp = temp)
        //    fixed (byte* pBytes = bytes)
        //    {
        //        var p = pTemp;
        //        var bp = pBytes;
        //        for (var i = 0; i < bytes.Length; i++)
        //        {
        //            var b = *bp;
        //            *p = hex[b >> 4];
        //            p++;
        //            *p = hex[b & 0xF];
        //            p++;
        //            bp++;
        //        }
        //    }

        //    return new string(temp);
        //}

        // --------------------------------------------------------------------------------

        public static unsafe byte[] ToBytes(ReadOnlySpan<char> text)
        {
            var bytes = new byte[text.Length / 2];

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

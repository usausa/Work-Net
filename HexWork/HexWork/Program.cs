namespace HexWork
{
    using System;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;

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
            var bytes = new byte[256];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)i;
            }

            var h1 = HexEncoder.ToHex(bytes);
            var h2 = HexEncoder.ToHexSpanUnsafe(bytes);
            var b = h1 == h2;


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

        [Benchmark]
        public string ToHexBase() => HexEncoder.ToHex(bytes);

        [Benchmark]
        public string ToHexSpan() => HexEncoder.ToHexSpan(bytes);

        [Benchmark]
        public string ToHexSpanUnsafe() => HexEncoder.ToHexSpanUnsafe(bytes);

        [Benchmark]
        public string ToHexSpanUnsafeB() => HexEncoder.ToHexSpanUnsafe2(bytes);

        [Benchmark]
        public string ToHexSpan32() => HexEncoder.ToHexSpan(bytes.AsSpan(0, 32));

        [Benchmark]
        public string ToHexSpanUnsafe32() => HexEncoder.ToHexSpanUnsafe(bytes.AsSpan(0, 32));

        [Benchmark]
        public string ToHexSpanUnsafe32B() => HexEncoder.ToHexSpanUnsafe2(bytes.AsSpan(0, 32));

        [Benchmark]
        public string ToHexSpan16() => HexEncoder.ToHexSpan(bytes.AsSpan(0, 16));

        [Benchmark]
        public string ToHexSpanUnsafe16() => HexEncoder.ToHexSpanUnsafe(bytes.AsSpan(0, 16));

        [Benchmark]
        public string ToHexSpanUnsafe16B() => HexEncoder.ToHexSpanUnsafe2(bytes.AsSpan(0, 16));

        [Benchmark]
        public string ToHexSpan4() => HexEncoder.ToHexSpan(bytes.AsSpan(0, 4));

        [Benchmark]
        public string ToHexSpanUnsafe4() => HexEncoder.ToHexSpanUnsafe(bytes.AsSpan(0, 4));

        [Benchmark]
        public string ToHexSpanUnsafe4B() => HexEncoder.ToHexSpanUnsafe2(bytes.AsSpan(0, 4));

        //[Benchmark]
        //public byte[] ToBytesBase() => HexEncoder.ToBytes(text);
    }

    public static class HexEncoder
    {
        // TODO ToHex util version, Span-Length
        // TODO ToBytes Custom
        // TODO ToBytes Span-Length

        public static string ToHex(byte[] bytes)
        {
            return ToHexInternal(bytes, 0, bytes.Length, null, null, 0, Environment.NewLine);
        }

        private static string ToHexInternal(byte[] bytes, int start, int length, string prefix, string separator, int lineSize, string lineSeparator)
        {
            var addPrefix = !String.IsNullOrEmpty(prefix);
            var addSeparator = !String.IsNullOrEmpty(separator);

            var bufferSize = length * 2;
            var lines = lineSize > 0 ? (length - 1) / lineSize : 0;
            if (lineSize > 0)
            {
                bufferSize += lines * lineSeparator.Length;
            }

            if (addPrefix)
            {
                bufferSize += prefix.Length * length;
            }

            if (addSeparator)
            {
                bufferSize += (length - lines - 1) * separator.Length;
            }

            var sb = new StringBuilder(bufferSize);
            var count = 0;
            for (var i = start; i < start + length; i++)
            {
                if (count != 0)
                {
                    if (count == lineSize)
                    {
                        sb.Append(lineSeparator);
                        count = 0;
                    }
                    else if (addSeparator)
                    {
                        sb.Append(separator);
                    }
                }

                if (addPrefix)
                {
                    sb.Append(prefix);
                }

                var b = bytes[i];
                sb.Append(ToHex(b >> 4));
                sb.Append(ToHex(b & 0x0F));
                count++;
            }

            return sb.ToString();
        }

        // TODO unsafe

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToHex(int x)
        {
            return x < 10 ? (char)(x + '0') : (char)(x + 'A' - 10);
        }

        public static string ToHexSpan(ReadOnlySpan<byte> bytes)
        {
            var length = bytes.Length * 2;
            var temp = length < 2048 ? stackalloc char[length] : new char[length];

            Span<char> hex = stackalloc char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
            };

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                temp[i * 2] = hex[b >> 4];
                temp[i * 2 + 1] = hex[b & 0xF];
            }

            return new string(temp);
        }

        public static unsafe string ToHexSpanUnsafe(ReadOnlySpan<byte> bytes)
        {
            var length = bytes.Length * 2;
            var temp = length < 2048 ? stackalloc char[length] : new char[length];

            Span<char> hex = stackalloc char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
            };

            // TODO ?
            fixed (char* ptr = temp)
            {
                char* p = ptr;
                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = bytes[i];
                    *p = hex[b >> 4];
                    p++;
                    *p = hex[b & 0xF];
                    p++;
                }
            }

            return new string(temp);
        }

        public static unsafe string ToHexSpanUnsafe2(ReadOnlySpan<byte> bytes)
        {
            var length = bytes.Length * 2;
            var temp = length < 2048 ? stackalloc char[length] : new char[length];

            Span<char> hex = stackalloc char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
            };

            // TODO ?
            fixed (char* pTemp = temp)
            fixed (byte* pBytes = bytes)
            {
                var p = pTemp;
                var bp = pBytes;
                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = *bp;
                    *p = hex[b >> 4];
                    p++;
                    *p = hex[b & 0xF];
                    p++;
                    bp++;
                }
            }

            return new string(temp);
        }

        // --------------------------------------------------------------------------------

        public static byte[] ToBytes(string text)
        {
            var bytes = new byte[text.Length / 2];
            for (var index = 0; index < bytes.Length; index++)
            {
                bytes[index] = byte.Parse(text.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return bytes;
        }
    }
}

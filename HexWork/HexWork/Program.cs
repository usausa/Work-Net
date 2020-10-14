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
        public string ToHexSimple() => HexEncoder.ToHexSimple(bytes);

        [Benchmark]
        public byte[] ToBytesBase() => HexEncoder.ToBytes(text);
    }

    public static class HexEncoder
    {
        // TODO ToHex tuning
        // TODO ToHex span version 1, 2
        // TODO ToBytes tuning
        // TODO ToBytes span version 1, 2

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToHexSimple(byte[] bytes)
        {
            return ToHexSimple(bytes, 0, bytes.Length);
        }

        public static string ToHexSimple(byte[] bytes, int start, int length)
        {
            var bufferSize = length * 2;

            var sb = new StringBuilder(bufferSize);
            for (var i = start; i < start + length; i++)
            {
                var b = bytes[i];
                sb.Append(ToHex2(b >> 4));
                sb.Append(ToHex2(b & 0x0F));
            }

            return sb.ToString();
        }

        // TODO unsafe

        private static char ToHex(int x)
        {
            return x < 10 ? (char)(x + '0') : (char)(x + 'A' - 10);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToHex2(int x)
        {
            return x < 10 ? (char)(x + '0') : (char)(x + 'A' - 10);
        }

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

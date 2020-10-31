namespace FillWork
{
    using System;
    using System.Runtime.CompilerServices;

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
        private readonly byte[] buffer = new byte[1024];

        [Benchmark]
        public void Clear1024() => buffer.AsSpan().Clear();

        [Benchmark]
        public void Span1024() => buffer.AsSpan().Fill(0);

        [Benchmark]
        public void Extension1024() => buffer.Fill(0, 1024, 0);

        [Benchmark]
        public void Clear64() => buffer.AsSpan(0, 64).Clear();

        [Benchmark]
        public void Span64() => buffer.AsSpan(0, 64).Fill(0);

        [Benchmark]
        public void Extension64() => buffer.Fill(0, 64, 0);

        [Benchmark]
        public void Clear32() => buffer.AsSpan(0, 32).Clear();

        [Benchmark]
        public void Span32() => buffer.AsSpan(0, 32).Fill(0);

        [Benchmark]
        public void Extension32() => buffer.Fill(0, 32, 0);

        [Benchmark]
        public void Clear16() => buffer.AsSpan(0, 16).Clear();

        [Benchmark]
        public void Span16() => buffer.AsSpan(0, 16).Fill(0);

        [Benchmark]
        public void Extension16() => buffer.Fill(0, 16, 0);

        [Benchmark]
        public void Clear8() => buffer.AsSpan(0, 8).Clear();

        [Benchmark]
        public void Span8() => buffer.AsSpan(0, 8).Fill(0);

        [Benchmark]
        public void Extension8() => buffer.Fill(0, 8, 0);

        [Benchmark]
        public void Clear4() => buffer.AsSpan(0, 4).Clear();

        [Benchmark]
        public void Span4() => buffer.AsSpan(0, 4).Fill(0);

        [Benchmark]
        public void Extension4() => buffer.Fill(0, 4, 0);
    }

    public static class Extensions
    {
        public static byte[] Fill(this byte[] array, byte value) => Fill(array, 0, array?.Length ?? 0, value);

        public static byte[] Fill(this byte[] array, int offset, int length, byte value)
        {
            if ((array.Length == 0) || (length <= 0))
            {
                return array;
            }

            array[offset] = value;

            int copy;
            for (copy = 1; copy <= length >> 1; copy <<= 1)
            {
                Bytes.FastCopy(array, offset, array, offset + copy, copy);
            }

            Bytes.FastCopy(array, offset, array, offset + copy, length - copy);

            return array;
        }
    }

    public static class Bytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void FastCopy(byte[] src, int srcOffset, byte[] dst, int dstOffset, int length)
        {
            if (length > 0)
            {
                fixed (byte* pSource = &src[srcOffset])
                fixed (byte* pDestination = &dst[dstOffset])
                {
                    Buffer.MemoryCopy(pSource, pDestination, length, length);
                }
            }
        }
    }
}

namespace BenchmarkApp
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
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.Default, MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.ShortRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private byte[] buffer;

        [Params(4, 32, 33, 64)]
        public int Length { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            buffer = new byte[Length];
        }

        [Benchmark]
        public void Reverse()
        {
            Helper.Reverse(buffer, 0, buffer.Length);
        }

        [Benchmark]
        public void ReverseUnsafe()
        {
            Helper.ReverseUnsafe(buffer, 0, buffer.Length);
        }

        [Benchmark]
        public void ReverseSpan()
        {
            Helper.ReverseSpan(buffer);
        }
    }

    public static class Helper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reverse(byte[] bytes, int index, int length)
        {
            var end = index + length - 1;
            while (index < end)
            {
                var tmp = bytes[index];
                bytes[index] = bytes[end];
                bytes[end] = tmp;
                index++;
                end--;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReverseUnsafe(byte[] bytes, int index, int length)
        {
            fixed (byte* ptr = &bytes[index])
            {
                var start = ptr;
                var end = ptr + length - 1;
                while (start < end)
                {
                    var tmp = *start;
                    *start = *end;
                    *end = tmp;
                    start++;
                    end--;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ReverseSpan(Span<byte> span)
        {
            // Valid ?
            fixed (byte* ptr = &span[0])
            {
                var start = ptr;
                var end = ptr + span.Length - 1;
                while (start < end)
                {
                    var tmp = *start;
                    *start = *end;
                    *end = tmp;
                    start++;
                    end--;
                }
            }
        }
    }
}

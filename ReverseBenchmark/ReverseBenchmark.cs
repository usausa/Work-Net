namespace BenchmarkApp
{
    using System;
    using System.Runtime.CompilerServices;

    using BenchmarkDotNet.Attributes;

    [Config(typeof(BenchmarkConfig))]
    public class ReverseBenchmark
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
            ReverseHelper.Reverse(buffer, 0, buffer.Length);
        }

        [Benchmark]
        public void ReverseUnsafe()
        {
            ReverseHelper.ReverseUnsafe(buffer, 0, buffer.Length);
        }

        [Benchmark]
        public void ReverseSpan()
        {
            ReverseHelper.ReverseSpan(buffer);
        }
    }

    public static class ReverseHelper
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
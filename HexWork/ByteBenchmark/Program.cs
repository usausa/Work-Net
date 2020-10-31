using System.Collections.Generic;

namespace ByteBenchmark
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
        private static readonly byte[] buffer1024 = new byte[1024];
        private static readonly byte[] buffer64 = new byte[64];
        private static readonly byte[] buffer32 = new byte[32];
        private static readonly byte[] buffer16 = new byte[16];

        public static void Main()
        {
            for (var i = 0; i < buffer16.Length; i++)
            {
                buffer16[i] = (byte)i;
            }
            for (var i = 0; i < buffer32.Length; i++)
            {
                buffer32[i] = (byte)i;
            }

            var b0 = buffer1024.RemoveRange2(4, 8);
            var b1 = buffer16.RemoveRange2(4, 8);
            var b2 = buffer32.RemoveRange2(8, 16);

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
        private readonly int[] buffer = new int[256];
        //private readonly byte[] buffer1024 = new byte[1024];
        //private readonly byte[] buffer64 = new byte[64];
        //private readonly byte[] buffer32 = new byte[32];
        //private readonly byte[] buffer16 = new byte[16];
        //private readonly byte[] buffer8 = new byte[8];
        //private readonly byte[] buffer4 = new byte[4];

        //private readonly byte[] buffer1024b = new byte[1024];
        //private readonly byte[] buffer64b = new byte[64];
        //private readonly byte[] buffer32b = new byte[32];
        //private readonly byte[] buffer16b = new byte[16];
        //private readonly byte[] buffer8b = new byte[8];
        //private readonly byte[] buffer4b = new byte[4];

        [GlobalSetup]
        public void Setup()
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = i;
            }
        }

        [Benchmark]
        public int Array0() => buffer.IndexOf(x => x == 0);
        //[Benchmark]
        //public int Span0() => buffer.AsSpan().IndexOf(x => x == 0);

        //[Benchmark]
        //public byte[] NewCombine64_3() => ArrayHelper.Combine(buffer64, buffer64, buffer64);

        //[Benchmark]
        //public byte[] OldCombine64_3() => buffer64.Combine(buffer64, buffer64);
        //[Benchmark]
        //public byte[] OldCombine64_32() => buffer64.Combine2(buffer64, buffer64);

        //[Benchmark]
        //public byte[] NewCombine16_3() => ArrayHelper.Combine(buffer16, buffer16, buffer16);

        //[Benchmark]
        //public byte[] OldCombine16_3() => buffer16.Combine(buffer16, buffer16);
        //[Benchmark]
        //public byte[] OldCombine16_32() => buffer16.Combine2(buffer16, buffer16);

        //[Benchmark]
        //public byte[] NewCombine4_3() => ArrayHelper.Combine(buffer4, buffer4, buffer4);

        //[Benchmark]
        //public byte[] OldCombine4_3() => buffer4.Combine(buffer4, buffer4);
        //[Benchmark]
        //public byte[] OldCombine4_32() => buffer4.Combine2(buffer4, buffer4);


        //[Benchmark]
        //public bool Extension1024() => buffer1024.ArrayEquals(0, buffer1024b, 0, 1024);

        //[Benchmark]
        //public bool Span1024() => buffer1024.AsSpan().SequenceEqual(buffer1024b);

        //[Benchmark]
        //public bool Extension64() => buffer64.ArrayEquals(0, buffer64b, 0, 64);

        //[Benchmark]
        //public bool Span64() => buffer64.AsSpan().SequenceEqual(buffer64b);

        //[Benchmark]
        //public bool Extension16() => buffer16.ArrayEquals(0, buffer16b, 0, 16);

        //[Benchmark]
        //public bool Span16() => buffer16.AsSpan().SequenceEqual(buffer16b);

        //[Benchmark]
        //public bool Extension8() => buffer8.ArrayEquals(0, buffer8b, 0, 8);

        //[Benchmark]
        //public bool Span8() => buffer8.AsSpan().SequenceEqual(buffer8b);

        //[Benchmark]
        //public bool Extension4() => buffer4.ArrayEquals(0, buffer4b, 0, 4);

        //[Benchmark]
        //public bool Span4() => buffer4.AsSpan().SequenceEqual(buffer4b);

        //[Benchmark]
        //public void Extension1024() => buffer1024.RemoveRange(256, 512);
        //[Benchmark]
        //public void Extension1024B() => buffer1024.RemoveRange2(256, 512);

        //[Benchmark]
        //public void Extension64() => buffer64.RemoveRange(16, 32);
        //[Benchmark]
        //public void Extension64B() => buffer64.RemoveRange2(16, 32);

        //[Benchmark]
        //public void Extension32() => buffer32.RemoveRange(8, 16);
        //[Benchmark]
        //public void Extension32B() => buffer32.RemoveRange2(8, 16);

        //[Benchmark]
        //public void Extension16() => buffer16.RemoveRange(4, 8);
        //[Benchmark]
        //public void Extension16B() => buffer16.RemoveRange2(4, 8);

        //[Benchmark]
        //public byte[] Span256() => buffer.AsSpan(0, 256).ToArray();

        //[Benchmark]
        //public byte[] Extension256() => buffer.SubArray(0, 256);

        //[Benchmark]
        //public byte[] Span64() => buffer.AsSpan(0, 64).ToArray();

        //[Benchmark]
        //public byte[] Extension64() => buffer.SubArray(0, 64);

        //[Benchmark]
        //public byte[] Span32() => buffer.AsSpan(0, 32).ToArray();

        //[Benchmark]
        //public byte[] Extension32() => buffer.SubArray(0, 32);

        //[Benchmark]
        //public byte[] Span8() => buffer.AsSpan(0, 8).ToArray();

        //[Benchmark]
        //public byte[] Extension8() => buffer.SubArray(0, 8);

        //[Benchmark]
        //public byte[] Span4() => buffer.AsSpan(0, 4).ToArray();

        //[Benchmark]
        //public byte[] Extension4() => buffer.SubArray(0, 4);
    }

    public static class Extensions
    {
        public static int IndexOf<T>(this T[] array, Func<T, bool> predicate)
        {
            if (array is null)
            {
                return -1;
            }

            for (var i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int IndexOf2<T>(this ReadOnlySpan<T> span, Func<T, bool> predicate)
        {
            for (var i = 0; i < span.Length; i++)
            {
                if (predicate(span[i]))
                {
                    return i;
                }
            }

            return -1;
        }


        public static byte[] Combine(this byte[] array, params byte[][] others)
        {
            if (others is null)
            {
                return array;
            }

            var length = array?.Length ?? 0;
            for (var i = 0; i < others.Length; i++)
            {
                var other = others[i];
                if (other != null)
                {
                    length += other.Length;
                }
            }

            if (length == 0)
            {
                return array;
            }

            var result = new byte[length];

            int offset;
            if (array != null)
            {
                Bytes.FastCopy(array, 0, result, 0, array.Length);
                offset = array.Length;
            }
            else
            {
                offset = 0;
            }

            for (var i = 0; i < others.Length; i++)
            {
                var other = others[i];
                if (other != null)
                {
                    Bytes.FastCopy(other, 0, result, offset, other.Length);
                    offset += other.Length;
                }
            }

            return result;
        }

        public static byte[] Combine2(this byte[] array, params byte[][] others)
        {
            if (others is null)
            {
                return array;
            }

            var length = array?.Length ?? 0;
            for (var i = 0; i < others.Length; i++)
            {
                var other = others[i];
                if (other != null)
                {
                    length += other.Length;
                }
            }

            if (length == 0)
            {
                return array;
            }

            var result = new byte[length];

            int offset;
            if (array != null)
            {
                array.AsSpan().CopyTo(result.AsSpan());
                offset = array.Length;
            }
            else
            {
                offset = 0;
            }

            for (var i = 0; i < others.Length; i++)
            {
                var other = others[i];
                if (other != null)
                {
                    other.AsSpan().CopyTo(result.AsSpan(offset));
                    offset += other.Length;
                }
            }

            return result;
        }

        public static bool ArrayEquals<T>(this T[] array, int offset, T[] other, int otherOffset, int length)
        {
            return ArrayEquals(array, offset, other, otherOffset, length, EqualityComparer<T>.Default);
        }

        public static bool ArrayEquals<T>(this T[] array, int offset, T[] other, int otherOffset, int length, IEqualityComparer<T> comparer)
        {
            if ((array is null) && (other is null))
            {
                return true;
            }

            if ((array is null) || (other is null))
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (!comparer.Equals(array[offset + i], other[otherOffset + i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static T[] RemoveAt<T>(this T[] array, int offset) => RemoveRange2(array, offset, 1);

        public static T[] RemoveRange2<T>(this T[] array, int start, int length)
        {
            if ((array is null) || (array.Length == 0) || (length <= 0) || (start >= array.Length))
            {
                return array;
            }

            var remainStart = start + length;
            var remainLength = remainStart > array.Length ? 0 : array.Length - remainStart;
            var result = new T[start + remainLength];

            if (start > 0)
            {
                array.AsSpan(0, start).CopyTo(result);
            }

            if (remainLength > 0)
            {
                array.AsSpan(remainStart).CopyTo(result.AsSpan(start));
            }

            return result;
        }

        public static byte[] RemoveAt(this byte[] array, int offset) => RemoveRange(array, offset, 1);

        public static byte[] RemoveRange(this byte[] array, int start, int length)
        {
            if ((array is null) || (array.Length == 0) || (length <= 0) || (start >= array.Length))
            {
                return array;
            }

            var remainStart = start + length;
            var remainLength = remainStart > array.Length ? 0 : array.Length - remainStart;
            var result = new byte[start + remainLength];

            if (start > 0)
            {
                Bytes.FastCopy(array, 0, result, 0, start);
            }

            if (remainLength > 0)
            {
                Bytes.FastCopy(array, remainStart, result, start, remainLength);
            }

            return result;
        }

        public static byte[] SubArray(this byte[] array, int offset, int length)
        {
            if (array is null)
            {
                return null;
            }

            if (offset >= array.Length)
            {
                return Array.Empty<byte>();
            }

            FixLength(ref length, array.Length - offset);
            var result = new byte[length];

            Bytes.FastCopy(array, offset, result, 0, length);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FixLength(ref int length, int remain)
        {
            if (remain < length)
            {
                length = remain;
            }
        }
    }

    public static class ArrayHelper
    {

        public static T[] Combine<T>(params T[][] arrays)
        {
            var length = 0;
            for (var i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];
                if (array != null)
                {
                    length += array.Length;
                }
            }

            if (length == 0)
            {
                return Array.Empty<T>();
            }

            var result = new T[length];

            var offset = 0;
            for (var i = 0; i < arrays.Length; i++)
            {
                var array = arrays[i];
                if (array != null)
                {
                    array.AsSpan().CopyTo(result.AsSpan(offset));
                    offset += array.Length;
                }
            }

            return result;
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

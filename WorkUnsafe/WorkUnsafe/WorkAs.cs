namespace WorkUnsafe
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    using BenchmarkDotNet.Attributes;

    [Config(typeof(BenchmarkConfig))]
    public class WorkAsBenchmark
    {
        private readonly byte[] buffer = new byte[16];

        [Benchmark]
        public void Pointer() => WorkAs.ByPointer(buffer, 0, 1, Int32.MaxValue, Int32.MinValue);

        [Benchmark]
        public void Unsafe() => WorkAs.ByUnsafe(buffer, 0, 1, Int32.MaxValue, Int32.MinValue);
    }

    public static class WorkAs
    {
        public static unsafe void ByPointer(byte[] bytes, int value1, int value2, int value3, int value4)
        {
            fixed (byte* p = &bytes[0])
            {
                var ptr = (WorkAsData*)p;
                ptr->Data1 = value1;
                ptr->Data2 = value2;
                ptr->Data3 = value3;
                ptr->Data4 = value4;
            }
        }

        public static void ByUnsafe(byte[] bytes, int value1, int value2, int value3, int value4)
        {
            ref var data = ref Unsafe.As<byte, WorkAsData>(ref bytes[0]);
            data.Data1 = value1;
            data.Data2 = value2;
            data.Data3 = value3;
            data.Data4 = value4;
        }

        public static void Run()
        {
            var bytes = new byte[16];
            ref var data = ref Unsafe.As<byte, WorkAsData>(ref bytes[0]);
            data.Data1 = 1;
            data.Data2 = 0b_11111111111111;
            data.Data3 = Int32.MaxValue;
            data.Data4 = Int32.MinValue;

            Debug.WriteLine(BitConverter.ToString(bytes));
        }
    }

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 16)]
    public struct WorkAsData
    {
        [FieldOffset(0)]
        public int Data1;
        [FieldOffset(4)]
        public int Data2;
        [FieldOffset(8)]
        public int Data3;
        [FieldOffset(12)]
        public int Data4;
    }
}

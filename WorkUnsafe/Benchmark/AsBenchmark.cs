namespace Benchmark;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;

[Config(typeof(BenchmarkConfig))]
public unsafe class AsBenchmark
{
    private readonly byte[] buffer = new byte[16];

    [Benchmark]
    public void ByPointer()
    {
        fixed (byte* p = &buffer[0])
        {
            var ptr = (WorkAsData*)p;
            ptr->Data1 = 1;
            ptr->Data2 = 2;
            ptr->Data3 = 3;
            ptr->Data4 = 4;
        }
    }

    [Benchmark]
    public void ByUnsafe()
    {
        ref var data = ref Unsafe.As<byte, WorkAsData>(ref buffer[0]);
        data.Data1 = 1;
        data.Data2 = 2;
        data.Data3 = 3;
        data.Data4 = 4;
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

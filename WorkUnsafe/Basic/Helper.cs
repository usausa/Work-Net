namespace Basic;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static class Helper
{
    private static ReadOnlySpan<byte> HexCharactersTable => "0123456789ABCDEF"u8;

    public static unsafe string ToHexString<T>(T value)
        where T : unmanaged
    {
        var sizeOfT = sizeof(T);
        var bufferSize = (2 * sizeOfT) + 2;
        var p = stackalloc char[bufferSize];

        p[0] = '0';
        p[1] = 'x';

        ref var rh = ref MemoryMarshal.GetReference(HexCharactersTable);

        for (int i = 0, j = bufferSize - 2; i < sizeOfT; i++, j -= 2)
        {
            var b = ((byte*)&value)[i];
            var low = b & 0x0F;
            var high = (b & 0xF0) >> 4;

            p[j + 1] = (char)Unsafe.Add(ref rh, low);
            p[j] = (char)Unsafe.Add(ref rh, high);
        }

        return new(p, 0, bufferSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool IsBitwiseEqual<T>(T value, T target)
        where T : unmanaged
    {
        if (sizeof(T) == 1)
        {
            var valueByte = Unsafe.As<T, byte>(ref value);
            var targetByte = Unsafe.As<T, byte>(ref target);
            return valueByte == targetByte;
        }
        if (sizeof(T) == 2)
        {
            var valueUShort = Unsafe.As<T, ushort>(ref value);
            var targetUShort = Unsafe.As<T, ushort>(ref target);
            return valueUShort == targetUShort;
        }
        if (sizeof(T) == 4)
        {
            var valueUInt = Unsafe.As<T, uint>(ref value);
            var targetUInt = Unsafe.As<T, uint>(ref target);
            return valueUInt == targetUInt;
        }
        if (sizeof(T) == 8)
        {
            var valueULong = Unsafe.As<T, ulong>(ref value);
            var targetULong = Unsafe.As<T, ulong>(ref target);
            return valueULong == targetULong;
        }
        if (sizeof(T) == 16)
        {
            var valueULong0 = Unsafe.As<T, ulong>(ref value);
            var targetULong0 = Unsafe.As<T, ulong>(ref target);
            if (valueULong0 != targetULong0)
            {
                return false;
            }

            var valueULong1 = Unsafe.Add(ref Unsafe.As<T, ulong>(ref value), 1);
            var targetULong1 = Unsafe.Add(ref Unsafe.As<T, ulong>(ref target), 1);
            return valueULong1 == targetULong1;
        }

        var valueBytes = new Span<byte>(Unsafe.AsPointer(ref value), sizeof(T));
        var targetBytes = new Span<byte>(Unsafe.AsPointer(ref target), sizeof(T));
        return valueBytes.SequenceEqual(targetBytes);
    }
}

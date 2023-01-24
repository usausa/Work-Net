#pragma warning disable IDE0046
namespace WorkTextRef;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal class Program
{
    static void Main()
    {
        // TODO
    }
}

public static class HexEncoder
{
    private static ReadOnlySpan<byte> HexTable => "0123456789ABCDEF"u8;

    //--------------------------------------------------------------------------------
    // Encode
    //--------------------------------------------------------------------------------

    // bytes : index
    // buffer: index
    [SkipLocalsInit]
    public static unsafe string Encode(ReadOnlySpan<byte> bytes)
    {
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var length = bytes.Length << 1;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];

        fixed (char* pBuffer = buffer)
        {
            var p = pBuffer;

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                *p = (char)Unsafe.Add(ref hex, b >> 4);
                p++;
                *p = (char)Unsafe.Add(ref hex, b & 0xF);
                p++;
            }

            return new string(pBuffer, 0, length);
        }
    }

    // bytes : index
    // buffer: pointer
    [SkipLocalsInit]
    public static unsafe string EncodeByPointer2(ReadOnlySpan<byte> bytes)
    {
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var length = bytes.Length << 1;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];

        fixed (char* pBuffer = buffer)
        {
            var p = pBuffer;

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                *p = (char)Unsafe.Add(ref hex, b >> 4);
                p++;
                *p = (char)Unsafe.Add(ref hex, b & 0xF);
                p++;
            }

            return new string(pBuffer, 0, length);
        }
    }

    // bytes : pointer
    // buffer: pointer
    [SkipLocalsInit]
    public static unsafe string EncodeByPointer1(ReadOnlySpan<byte> bytes)
    {
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var sourceLength = bytes.Length;
        var length = sourceLength << 1;
        var buffer = length < 512 ? stackalloc char[length] : new char[length];

        fixed (byte* pBytes = bytes)
        fixed (char* pBuffer = &buffer[0])
        {
            var pb = pBytes;
            var p = pBuffer;

            for (var i = 0; i < sourceLength; i++)
            {
                var b = *pb;
                *p = (char)Unsafe.Add(ref hex, b >> 4);
                p++;
                *p = (char)Unsafe.Add(ref hex, b & 0xF);
                p++;
                pb++;
            }

            return new string(pBuffer, 0, length);
        }
    }

    // TODO
    // bytes : reference
    // buffer: pointer

    // bytes : index
    // buffer: reference
    [SkipLocalsInit]
    public static unsafe string EncodeByReference2(ReadOnlySpan<byte> bytes)
    {
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var length = bytes.Length * 2;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var buffer = ref MemoryMarshal.GetReference(span);

        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return new string(span);
    }

    // bytes : pointer
    // buffer: reference
    [SkipLocalsInit]
    public static unsafe string EncodeByReference1(ReadOnlySpan<byte> bytes)
    {
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var sourceLength = bytes.Length;
        var length = bytes.Length * 2;
        var span = length < 512 ? stackalloc char[length] : new char[length];
        ref var r = ref MemoryMarshal.GetReference(bytes);
        ref var buffer = ref MemoryMarshal.GetReference(span);

        for (var i = 0; i < sourceLength; i++)
        {
            var b = r;
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
            r = ref Unsafe.Add(ref r, 1);
        }

        return new string(span);
    }

    // TODO
    // bytes : reference
    // buffer: reference

    //--------------------------------------------------------------------------------
    // Encode
    //--------------------------------------------------------------------------------

    // bytes : index
    // destination: pointer
    public static unsafe int EncodeByPointer(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        var length = bytes.Length << 1;
        fixed (char* ptr = destination)
        {
            var p = ptr;

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                *p = (char)Unsafe.Add(ref hex, b >> 4);
                p++;
                *p = (char)Unsafe.Add(ref hex, b & 0xF);
                p++;
            }
        }

        return length;
    }

    // TODO
    // bytes : pointer
    // destination: pointer

    // TODO
    // bytes : reference
    // destination: pointer

    // bytes : index
    // destination: reference
    public static int EncodeByReference(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        if (bytes.IsEmpty)
        {
            return 0;
        }

        var length = bytes.Length * 2;
        ref var buffer = ref MemoryMarshal.GetReference(destination);
        ref var hex = ref MemoryMarshal.GetReference(HexTable);

        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            buffer = (char)Unsafe.Add(ref hex, b >> 4);
            buffer = ref Unsafe.Add(ref buffer, 1);
            buffer = (char)Unsafe.Add(ref hex, b & 0xF);
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return length;
    }

    // TODO
    // bytes : pointer
    // destination: reference

    // TODO
    // bytes : reference
    // buffer: reference

    //--------------------------------------------------------------------------------
    // Decode
    //--------------------------------------------------------------------------------

    // TODO
    // code : index+2
    // buffer: pointer

    // code : pointer
    // buffer: pointer
    [SkipLocalsInit]
    public static unsafe byte[] DecodeByPointer(ReadOnlySpan<char> code)
    {
        // TODO stack
        var buffer = new byte[code.Length >> 1];

        fixed (char* pSource = code)
        fixed (byte* pBuffer = &buffer[0])
        {
            var pb = pBuffer;
            var ps = pSource;
            for (var i = 0; i < buffer.Length; i++)
            {
                var b = CharToNumber(*ps) << 4;
                ps++;
                *pb = (byte)(b + CharToNumber(*ps));
                ps++;
                pb++;
            }
        }

        return buffer;
    }

    // TODO
    // bytes : reference
    // buffer: pointer

    // TODO
    // bytes : index
    // buffer: reference

    [SkipLocalsInit]
    public static byte[] DecodeByReference(ReadOnlySpan<char> code)
    {
        // TODO stack
        var buffer = new byte[code.Length >> 1];
        ref var source = ref MemoryMarshal.GetReference(code);

        for (var i = 0; i < buffer.Length; i++)
        {
            var b = CharToNumber(source) << 4;
            source = ref Unsafe.Add(ref source, 1);
            buffer[i] = (byte)(b + CharToNumber(source));
            source = ref Unsafe.Add(ref source, 1);
        }

        return buffer;
    }

    //--------------------------------------------------------------------------------
    // Decode
    //--------------------------------------------------------------------------------

    public static unsafe int DecodeByPointer(ReadOnlySpan<char> code, Span<byte> destination)
    {
        var length = code.Length >> 1;

        fixed (char* pSource = code)
        fixed (byte* pBuffer = destination)
        {
            var pb = pBuffer;
            var ps = pSource;
            for (var i = 0; i < length; i++)
            {
                var b = CharToNumber(*ps) << 4;
                ps++;
                *pb = (byte)(b + CharToNumber(*ps));
                ps++;
                pb++;
            }
        }

        return length;
    }

    // TODO bはindex先

    // TODO bもreference

    public static unsafe int DecodeByReference(ReadOnlySpan<char> code, Span<byte> destination)
    {
        var length = code.Length >> 1;

        ref var source = ref MemoryMarshal.GetReference(code);

        for (var i = 0; i < length; i++)
        {
            var b = CharToNumber(source) << 4;
            source = ref Unsafe.Add(ref source, 1);
            destination[i] = (byte)(b + CharToNumber(source));
            source = ref Unsafe.Add(ref source, 1);
        }

        fixed (char* pSource = code)
        fixed (byte* pBuffer = destination)
        {
            var pb = pBuffer;
            var ps = pSource;
            for (var i = 0; i < length; i++)
            {
                var b = CharToNumber(*ps) << 4;
                ps++;
                *pb = (byte)(b + CharToNumber(*ps));
                ps++;
                pb++;
            }
        }

        return length;
    }

    public static int DecodeByReference3(ReadOnlySpan<char> code, Span<byte> destination)
    {
        var length = code.Length >> 1;
        ref var buffer = ref MemoryMarshal.GetReference(destination);

        for (var i = 0; i < code.Length; i += 2)
        {
            var b = CharToNumber(code[i]) << 4;
            buffer = (byte)(b + CharToNumber(code[i + 1]));
            buffer = ref Unsafe.Add(ref buffer, 1);
        }

        return length;
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

        if ((c <= 'f') && (c >= 'a'))
        {
            return c - 'a' + 10;
        }

        return 0;
    }
}

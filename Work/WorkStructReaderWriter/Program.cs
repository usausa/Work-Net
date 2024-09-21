namespace WorkStructReaderWriter;

using System.Runtime.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal class Program
{
    public static void Main()
    {
        var writer = DataWriter.Create();
        writer.WriteInt32(123);
        writer.WriteString("test");
        writer.WriteDate(new DateTime(2000, 12, 31, 23, 59, 59));

        var reader = DataReader.From(writer.Slice());
        var id = reader.ReadInt32();
        var name = reader.ReadString();
        var createAt = reader.ReadDate();

        Debug.WriteLine($"id={id}, name={name}, createAt={createAt}");
    }
}

//--------------------------------------------------------------------------------
// Writer
//--------------------------------------------------------------------------------

public ref struct DataWriter
{
    private static readonly UTF8Encoding Encoding = new();

    private Span<byte> buffer;

    public int Length { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DataWriter Create() => new(Array.Empty<byte>());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DataWriter From(byte[] buffer) => new(buffer);

    private DataWriter(Span<byte> buffer)
    {
        this.buffer = buffer;
        Length = buffer.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> Slice() => buffer.Slice(0, Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToArray() => Slice().ToArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowBy(int amount)
    {
        if (Length + amount > buffer.Length)
        {
            var newBuffer = new Span<byte>(new byte[(Length + amount) << 1]);
            buffer.CopyTo(newBuffer);
            buffer = newBuffer;
        }
        Length += amount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        const int size = 1;
        var index = Length;
        GrowBy(size);
        buffer[index] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32(int value)
    {
        const int size = 4;
        var index = Length;
        GrowBy(size);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer.Slice(index, size)), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64(long value)
    {
        const int size = 8;
        var index = Length;
        GrowBy(size);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer.Slice(index, size)), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDate(DateTime date)
    {
        WriteInt64(date.ToUniversalTime().ToBinary());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteGuid(Guid guid)
    {
        const int size = 16;
        var index = Length;
        GrowBy(size);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(buffer.Slice(index, size)), guid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value)
    {
        unsafe
        {
            fixed (char* c = value)
            {
                var size = Encoding.GetByteCount(c, value.Length);
                WriteInt32(size);
                var index = Length;
                GrowBy(size);
                fixed (byte* o = buffer.Slice(index, size))
                {
                    Encoding.GetBytes(c, value.Length, o, size);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBytes(byte[] value)
    {
        WriteInt32(value.Length);
        if (value.Length == 0)
        {
            return;
        }
        var index = Length;
        GrowBy(value.Length);
        value.AsSpan().CopyTo(buffer.Slice(index, value.Length));
    }
}

//--------------------------------------------------------------------------------
// Reader
//--------------------------------------------------------------------------------

public ref struct DataReader
{
    private static readonly UTF8Encoding Encoding = new();

    private readonly ReadOnlySpan<byte> buffer;

    public int Position { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DataReader From(ReadOnlySpan<byte> buffer) => new(buffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DataReader From(byte[] buffer) => new(buffer);

    private DataReader(ReadOnlySpan<byte> buffer)
    {
        this.buffer = buffer;
        Position = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte() => buffer[Position++];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        const int size = 4;
        var value = Unsafe.ReadUnaligned<int>(ref MemoryMarshal.GetReference(buffer.Slice(Position, size)));
        Position += size;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        const int size = 8;
        var value = Unsafe.ReadUnaligned<long>(ref MemoryMarshal.GetReference(buffer.Slice(Position, size)));
        Position += size;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ReadDate() => DateTime.FromBinary(ReadInt64());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Guid ReadGuid()
    {
        const int size = 16;
        var index = Position;
        Position += size;
        return Unsafe.ReadUnaligned<Guid>(ref MemoryMarshal.GetReference(buffer.Slice(index, size)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
        var stringByteCount = ReadInt32();
        var stringSpan = buffer.Slice(Position, stringByteCount);
        Position += stringByteCount;

       unsafe
        {
            fixed (byte* bytePtr = stringSpan)
            {
                return Encoding.GetString(bytePtr, stringByteCount);
            }
        }
    }
}

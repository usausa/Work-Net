namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class Int64ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<long, byte>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(Byte.MaxValue, converter(-1L));
            // Min/Max
            Assert.Equal(unchecked((byte)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(unchecked((byte)Int64.MaxValue), converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<long, sbyte>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal((sbyte)unchecked((byte)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(unchecked((sbyte)unchecked((byte)Int64.MaxValue)), converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<long, char>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(Char.MaxValue, converter(-1L));
            // Min/Max
            Assert.Equal(unchecked((char)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(unchecked((char)Int64.MaxValue), converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<long, short>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(unchecked((short)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(unchecked((short)Int64.MaxValue), converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal((long)Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal((long)Int16.MaxValue, converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<long, ushort>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(UInt16.MaxValue, converter(-1L));
            // Min/Max
            Assert.Equal(unchecked((ushort)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(unchecked((ushort)Int64.MaxValue), converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((ulong)UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<long, int>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(unchecked((int)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(unchecked((int)Int64.MaxValue), converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal(Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal(Int32.MaxValue, converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<long, uint>();

            // Base
            Assert.Equal(0u, converter(0L));
            Assert.Equal(1u, converter(1L));
            Assert.Equal(unchecked((uint)Int64.MaxValue), converter(-1L));
            // Min/Max
            Assert.Equal(unchecked((uint)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(unchecked((uint)Int64.MaxValue), converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal(UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
        }

        // Nop long

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<long, ulong>();

            // Base
            Assert.Equal(0ul, converter(0L));
            Assert.Equal(1ul, converter(1L));
            // Min/Max
            Assert.Equal(unchecked((ulong)Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal((ulong)Int64.MaxValue, converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)unchecked((long)UInt64.MinValue), converter(unchecked((long)UInt64.MinValue)));
            Assert.Equal(unchecked((ulong)unchecked((long)UInt64.MaxValue)), converter(unchecked((long)UInt64.MaxValue)));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<long, float>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(Int64.MinValue, converter(Int64.MinValue));
            Assert.Equal(Int64.MaxValue, converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((long)Single.MinValue), converter(unchecked((long)Single.MinValue)));
            Assert.Equal(unchecked((long)Single.MaxValue), converter(unchecked((long)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<long, double>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(Int64.MinValue, converter(Int64.MinValue));
            Assert.Equal(Int64.MaxValue, converter(Int64.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((long)Double.MinValue), converter(unchecked((long)Double.MinValue)));
            Assert.Equal(unchecked((long)Double.MaxValue), converter(unchecked((long)Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<long, decimal>();

            // Base
            Assert.Equal(0m, converter(0L));
            Assert.Equal(1m, converter(1L));
            // Min/Max
            Assert.Equal(Int64.MinValue, converter(Int64.MinValue));
            Assert.Equal(Int64.MaxValue, converter(Int64.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<long, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0L));
            Assert.Equal((IntPtr)1, converter(1L));
            // Min/Max
            Assert.Equal((IntPtr)Int64.MinValue, converter(Int64.MinValue));
            Assert.Equal((IntPtr)Int64.MaxValue, converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<long, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0L));
            Assert.Equal((UIntPtr)1, converter(1L));
            // Min/Max
            Assert.Equal((UIntPtr)Int64.MinValue, converter(Int64.MinValue));
            Assert.Equal((UIntPtr)Int64.MaxValue, converter(Int64.MaxValue));
        }
    }
}

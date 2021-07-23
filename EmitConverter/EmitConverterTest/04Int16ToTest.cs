namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class Int16ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<short, byte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Byte.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((byte)Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(unchecked((byte)Int16.MaxValue), converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<short, sbyte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal((sbyte)unchecked((byte)Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(unchecked((sbyte)unchecked((byte)Int16.MaxValue)), converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<short, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Char.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((char)Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal((char)Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal((char)(short)Char.MinValue, converter((short)Char.MinValue));
            Assert.Equal(unchecked((char)unchecked((short)Char.MaxValue)), converter(unchecked((short)Char.MaxValue)));
        }

        // Nop short

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<short, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(UInt16.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((ushort)Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal((ushort)Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal((ushort)(short)UInt16.MinValue, converter((short)UInt16.MinValue));
            Assert.Equal(unchecked((ushort)unchecked((short)UInt16.MaxValue)), converter(unchecked((short)UInt16.MaxValue)));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<short, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal(Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)Int32.MinValue), converter(unchecked((short)Int32.MinValue)));
            Assert.Equal(unchecked((short)Int32.MaxValue), converter(unchecked((short)Int32.MaxValue)));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<short, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            Assert.Equal(UInt32.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((uint)Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal((uint)Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal((uint)(short)UInt32.MinValue, converter((short)UInt32.MinValue));
            Assert.Equal(unchecked((uint)unchecked((short)UInt32.MaxValue)), converter(unchecked((short)UInt32.MaxValue)));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<short, long>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal(Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)Int64.MinValue), converter(unchecked((short)Int64.MinValue)));
            Assert.Equal(unchecked((short)Int64.MaxValue), converter(unchecked((short)Int64.MaxValue)));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<short, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(unchecked((ulong)Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal((ulong)Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)unchecked((short)UInt64.MinValue), converter(unchecked((short)UInt64.MinValue)));
            Assert.Equal(unchecked((ulong)unchecked((short)UInt64.MaxValue)), converter(unchecked((short)UInt64.MaxValue)));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<short, float>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal(Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)Single.MinValue), converter(unchecked((short)Single.MinValue)));
            Assert.Equal(unchecked((short)Single.MaxValue), converter(unchecked((short)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<short, double>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal(Int16.MaxValue, converter(Int16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)Double.MinValue), converter(unchecked((short)Double.MinValue)));
            Assert.Equal(unchecked((short)Double.MaxValue), converter(unchecked((short)Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<short, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal(Int16.MaxValue, converter(Int16.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<short, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((IntPtr)Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal((IntPtr)Int16.MaxValue, converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<short, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((UIntPtr)Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal((UIntPtr)Int16.MaxValue, converter(Int16.MaxValue));
        }
    }
}

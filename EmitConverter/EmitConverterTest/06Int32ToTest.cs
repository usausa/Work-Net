namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class Int32ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<int, byte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Byte.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((byte)Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(unchecked((byte)Int32.MaxValue), converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<int, sbyte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal((sbyte)unchecked((byte)Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(unchecked((sbyte)unchecked((byte)Int32.MaxValue)), converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<int, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Char.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((char)Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(unchecked((char)Int32.MaxValue), converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<int, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((short)Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(unchecked((short)Int32.MaxValue), converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal((int)Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal((int)Int16.MaxValue, converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<int, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(UInt16.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((ushort)Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(unchecked((ushort)Int32.MaxValue), converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal((uint)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((uint)UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        // Nop int

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<int, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            Assert.Equal(UInt32.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((uint)Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal((uint)Int32.MaxValue, converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal((uint)(int)UInt32.MinValue, converter((int)UInt32.MinValue));
            Assert.Equal(unchecked((uint)unchecked((int)UInt32.MaxValue)), converter(unchecked((int)UInt32.MaxValue)));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<int, long>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal(Int32.MaxValue, converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((int)Int64.MinValue), converter(unchecked((int)Int64.MinValue)));
            Assert.Equal(unchecked((int)Int64.MaxValue), converter(unchecked((int)Int64.MaxValue)));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<int, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(unchecked((ulong)Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal((ulong)Int32.MaxValue, converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)unchecked((int)UInt64.MinValue), converter(unchecked((int)UInt64.MinValue)));
            Assert.Equal(unchecked((ulong)unchecked((int)UInt64.MaxValue)), converter(unchecked((int)UInt64.MaxValue)));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<int, float>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal(Int32.MaxValue, converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((int)Single.MinValue), converter(unchecked((int)Single.MinValue)));
            Assert.Equal(unchecked((int)Single.MaxValue), converter(unchecked((int)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<int, double>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal(Int32.MaxValue, converter(Int32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((int)Double.MinValue), converter(unchecked((int)Double.MinValue)));
            Assert.Equal(unchecked((int)Double.MaxValue), converter(unchecked((int)Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<int, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal(Int32.MaxValue, converter(Int32.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<int, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((IntPtr)Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal((IntPtr)Int32.MaxValue, converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<int, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((UIntPtr)Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal((UIntPtr)Int32.MaxValue, converter(Int32.MaxValue));
        }
    }
}

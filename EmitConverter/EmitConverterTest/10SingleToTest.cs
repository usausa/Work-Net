namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class SingleToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<float, byte>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(Byte.MaxValue, converter(-1f));
            // Min/Max
            Assert.Equal(unchecked((byte)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((byte)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<float, sbyte>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal((sbyte)unchecked((byte)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((sbyte)unchecked((byte)Single.MaxValue)), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<float, char>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(Char.MaxValue, converter(-1f));
            // Min/Max
            Assert.Equal(unchecked((char)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((char)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<float, short>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(unchecked((short)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((short)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal((float)Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal((float)Int16.MaxValue, converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<float, ushort>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(UInt16.MaxValue, converter(-1f));
            // Min/Max
            Assert.Equal(unchecked((ushort)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((ushort)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal((float)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((float)UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<float, int>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(unchecked((int)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((int)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal((float)Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal((float)Int32.MaxValue, converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<float, uint>();

            // Base
            Assert.Equal(0ul, converter(0f));
            Assert.Equal(1ul, converter(1f));
            // Min/Max
            Assert.Equal(unchecked((uint)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((uint)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal((uint)(float)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(unchecked((uint)(float)UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<float, long>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(unchecked((long)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((long)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal((float)Int64.MinValue, converter(Int64.MinValue));
            Assert.Equal((float)Int64.MaxValue, converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<float, ulong>();

            // Base
            Assert.Equal(0ul, converter(0f));
            Assert.Equal(1ul, converter(1f));
            // Min/Max
            Assert.Equal(unchecked((ulong)Single.MinValue), converter(Single.MinValue));
            Assert.Equal(unchecked((ulong)Single.MaxValue), converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)(float)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((ulong)(float)UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        // Nop float

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<float, double>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(Single.MinValue, converter(Single.MinValue));
            Assert.Equal(Single.MaxValue, converter(Single.MaxValue));
            // Compare to cast
            Assert.Equal((float)Double.MinValue, converter((float)Double.MinValue));
            Assert.Equal((float)Double.MaxValue, converter((float)Double.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<float, decimal>();

            // Base
            Assert.Equal(0m, converter(0f));
            Assert.Equal(1m, converter(1f));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<float, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0f));
            Assert.Equal((IntPtr)1, converter(1f));
            // Min/Max
            Assert.Equal((IntPtr)Single.MinValue, converter(Single.MinValue));
            Assert.Equal((IntPtr)Single.MaxValue, converter(Single.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<float, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0f));
            Assert.Equal((UIntPtr)1, converter(1f));
            // Min/Max
            Assert.Equal((UIntPtr)Single.MinValue, converter(Single.MinValue));
            Assert.Equal((UIntPtr)Single.MaxValue, converter(Single.MaxValue));
        }
    }
}

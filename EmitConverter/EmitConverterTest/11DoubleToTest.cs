namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class DoubleToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<double, byte>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(Byte.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((byte)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((byte)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<double, sbyte>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal((sbyte)unchecked((byte)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((sbyte)unchecked((byte)Double.MaxValue)), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<double, char>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(Char.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((char)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((char)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<double, short>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((short)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((short)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal((double)Int16.MinValue, converter(Int16.MinValue));
            Assert.Equal((double)Int16.MaxValue, converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<double, ushort>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(UInt16.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((ushort)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((ushort)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal((double)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((double)UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        private int Convert(double value) => (int)value;

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<double, int>();

            var min = unchecked((int)Double.MinValue);
            var min2 = converter(Double.MinValue);
            var min3 = Convert(Double.MinValue);

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((int)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((int)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal((double)Int32.MinValue, converter(Int32.MinValue));
            Assert.Equal((double)Int32.MaxValue, converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<double, uint>();

            // Base
            Assert.Equal(0ul, converter(0d));
            Assert.Equal(1ul, converter(1d));
            // Min/Max
            Assert.Equal(unchecked((uint)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((uint)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)(double)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<double, long>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((long)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((long)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal((double)Int64.MinValue, converter(Int64.MinValue));
            Assert.Equal((double)Int64.MaxValue, converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<double, ulong>();

            // Base
            Assert.Equal(0ul, converter(0d));
            Assert.Equal(1ul, converter(1d));
            // Min/Max
            Assert.Equal(unchecked((ulong)Double.MinValue), converter(Double.MinValue));
            Assert.Equal(unchecked((ulong)Double.MaxValue), converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)(double)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((ulong)(double)UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<double, float>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal((float)Double.MinValue, converter(Double.MinValue));
            Assert.Equal((float)Double.MaxValue, converter(Double.MaxValue));
            // Compare to cast
            Assert.Equal((double)Single.MinValue, converter(Single.MinValue));
            Assert.Equal((double)Single.MaxValue, converter(Single.MaxValue));
        }

        // Nop double

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<double, decimal>();

            // Base
            Assert.Equal(0m, converter(0d));
            Assert.Equal(1m, converter(1d));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<double, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0d));
            Assert.Equal((IntPtr)1, converter(1d));
            // Min/Max
            Assert.Equal((IntPtr)Double.MinValue, converter(Double.MinValue));
            Assert.Equal((IntPtr)Double.MaxValue, converter(Double.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<double, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0d));
            Assert.Equal((UIntPtr)1, converter(1d));
            // Min/Max
            Assert.Equal((UIntPtr)Double.MinValue, converter(Double.MinValue));
            Assert.Equal((UIntPtr)Double.MaxValue, converter(Double.MaxValue));
        }
    }
}

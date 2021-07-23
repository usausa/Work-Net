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
            Assert.Equal(ManualConverter.DoubleToByte(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToByte(Double.MaxValue), converter(Double.MaxValue));
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
            Assert.Equal(ManualConverter.DoubleToSByte(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToSByte(Double.MaxValue), converter(Double.MaxValue));
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
            Assert.Equal(ManualConverter.DoubleToChar(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToChar(Double.MaxValue), converter(Double.MaxValue));
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
            Assert.Equal(ManualConverter.DoubleToInt16(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToInt16(Double.MaxValue), converter(Double.MaxValue));
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
            Assert.Equal(ManualConverter.DoubleToUInt16(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUInt16(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<double, int>();

            // Base
            Assert.Equal(0, converter(0d));
            Assert.Equal(1, converter(1d));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.DoubleToInt32(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToInt32(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<double, uint>();

            // Base
            Assert.Equal(0ul, converter(0d));
            Assert.Equal(1ul, converter(1d));
            // Min/Max
            Assert.Equal(ManualConverter.DoubleToUInt32(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUInt32(Double.MaxValue), converter(Double.MaxValue));
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
            Assert.Equal(ManualConverter.DoubleToInt64(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToInt64(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<double, ulong>();

            // Base
            Assert.Equal(0ul, converter(0d));
            Assert.Equal(1ul, converter(1d));
            // Min/Max
            Assert.Equal(ManualConverter.DoubleToUInt64(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUInt64(Double.MaxValue), converter(Double.MaxValue));
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
            //Assert.Equal(ManualConverter.DoubleToDecimal(Double.MinValue), converter(Double.MinValue));
            //Assert.Equal(ManualConverter.DoubleToDecimal(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<double, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0d));
            Assert.Equal((IntPtr)1, converter(1d));
            Assert.Equal(ManualConverter.DoubleToIntPtr(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToIntPtr(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<double, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0d));
            Assert.Equal((UIntPtr)1, converter(1d));
            Assert.Equal(ManualConverter.DoubleToUIntPtr(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUIntPtr(Double.MaxValue), converter(Double.MaxValue));
        }
    }
}

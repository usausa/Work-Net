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

            Assert.Equal(ManualConverter.DoubleToByte(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToByte(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToByte(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToByte(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToByte(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<double, sbyte>();

            Assert.Equal(ManualConverter.DoubleToSByte(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToSByte(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToSByte(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToSByte(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToSByte(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<double, char>();

            Assert.Equal(ManualConverter.DoubleToChar(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToChar(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToChar(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToChar(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToChar(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<double, short>();

            Assert.Equal(ManualConverter.DoubleToInt16(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToInt16(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToInt16(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToInt16(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToInt16(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<double, ushort>();

            Assert.Equal(ManualConverter.DoubleToUInt16(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToUInt16(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToUInt16(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToUInt16(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUInt16(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<double, int>();

            Assert.Equal(ManualConverter.DoubleToInt32(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToInt32(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToInt32(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToInt32(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToInt32(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<double, uint>();

            Assert.Equal(ManualConverter.DoubleToUInt32(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToUInt32(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToUInt32(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToUInt32(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUInt32(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<double, long>();

            Assert.Equal(ManualConverter.DoubleToInt64(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToInt64(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToInt64(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToInt64(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToInt64(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<double, ulong>();

            Assert.Equal(ManualConverter.DoubleToUInt64(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToUInt64(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToUInt64(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToUInt64(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUInt64(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<double, float>();

            Assert.Equal(ManualConverter.DoubleToSingle(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToSingle(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToSingle(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToSingle(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToSingle(Double.MinValue), converter(Double.MinValue));
        }

        // Nop double

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<double, decimal>();

            Assert.Equal(ManualConverter.DoubleToDecimal(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToDecimal(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToDecimal(-1d), converter(-1d));
            //Assert.Equal(ManualConverter.DoubleToDecimal(Double.MinValue), converter(Double.MinValue));
            //Assert.Equal(ManualConverter.DoubleToDecimal(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<double, IntPtr>();

            Assert.Equal(ManualConverter.DoubleToIntPtr(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToIntPtr(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToIntPtr(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToIntPtr(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToIntPtr(Double.MaxValue), converter(Double.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<double, UIntPtr>();

            Assert.Equal(ManualConverter.DoubleToUIntPtr(0d), converter(0d));
            Assert.Equal(ManualConverter.DoubleToUIntPtr(1d), converter(1d));
            Assert.Equal(ManualConverter.DoubleToUIntPtr(-1d), converter(-1d));
            Assert.Equal(ManualConverter.DoubleToUIntPtr(Double.MinValue), converter(Double.MinValue));
            Assert.Equal(ManualConverter.DoubleToUIntPtr(Double.MaxValue), converter(Double.MaxValue));
        }
    }
}

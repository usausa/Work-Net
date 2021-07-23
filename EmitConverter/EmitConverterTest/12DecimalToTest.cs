namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class DecimalToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<decimal, byte>();

            Assert.Equal(ManualConverter.DecimalToByte(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToByte(1m), converter(1m));
            //Assert.Equal(ManualConverter.DecimalToByte(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToByte(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToByte(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<decimal, sbyte>();

            Assert.Equal(ManualConverter.DecimalToSByte(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToSByte(1m), converter(1m));
            Assert.Equal(ManualConverter.DecimalToSByte(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToSByte(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToSByte(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<decimal, char>();

            Assert.Equal(ManualConverter.DecimalToChar(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToChar(1m), converter(1m));
            //Assert.Equal(ManualConverter.DecimalToChar(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToChar(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToChar(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<decimal, short>();

            Assert.Equal(ManualConverter.DecimalToInt16(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToInt16(1m), converter(1m));
            Assert.Equal(ManualConverter.DecimalToInt16(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToInt16(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToInt16(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<decimal, ushort>();

            Assert.Equal(ManualConverter.DecimalToUInt16(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToUInt16(1m), converter(1m));
            //Assert.Equal(ManualConverter.DecimalToUInt16(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToUInt16(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToUInt16(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<decimal, int>();

            Assert.Equal(ManualConverter.DecimalToInt32(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToInt32(1m), converter(1m));
            Assert.Equal(ManualConverter.DecimalToInt32(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToInt32(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToInt32(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<decimal, uint>();

            Assert.Equal(ManualConverter.DecimalToUInt32(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToUInt32(1m), converter(1m));
            //Assert.Equal(ManualConverter.DecimalToUInt32(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToUInt32(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToUInt32(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<decimal, long>();

            Assert.Equal(ManualConverter.DecimalToInt64(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToInt64(1m), converter(1m));
            Assert.Equal(ManualConverter.DecimalToInt64(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToInt64(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToInt64(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<decimal, ulong>();

            Assert.Equal(ManualConverter.DecimalToUInt64(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToUInt64(1m), converter(1m));
            //Assert.Equal(ManualConverter.DecimalToUInt64(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToUInt64(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToUInt64(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<decimal, float>();

            Assert.Equal(ManualConverter.DecimalToSingle(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToSingle(1m), converter(1m));
            Assert.Equal(ManualConverter.DecimalToSingle(-1m), converter(-1m));
            Assert.Equal(ManualConverter.DecimalToSingle(Decimal.MinValue), converter(Decimal.MinValue));
            Assert.Equal(ManualConverter.DecimalToSingle(Decimal.MinValue), converter(Decimal.MinValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<decimal, double>();

            Assert.Equal(ManualConverter.DecimalToDouble(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToDouble(1m), converter(1m));
            Assert.Equal(ManualConverter.DecimalToDouble(-1m), converter(-1m));
            Assert.Equal(ManualConverter.DecimalToDouble(Decimal.MinValue), converter(Decimal.MinValue));
            Assert.Equal(ManualConverter.DecimalToDouble(Decimal.MinValue), converter(Decimal.MinValue));
        }

        // Nop decimal

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<decimal, IntPtr>();

            Assert.Equal(ManualConverter.DecimalToIntPtr(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToIntPtr(1m), converter(1m));
            Assert.Equal(ManualConverter.DecimalToIntPtr(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToIntPtr(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToIntPtr(Decimal.MaxValue), converter(Decimal.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<decimal, UIntPtr>();

            Assert.Equal(ManualConverter.DecimalToUIntPtr(0m), converter(0m));
            Assert.Equal(ManualConverter.DecimalToUIntPtr(1m), converter(1m));
            //Assert.Equal(ManualConverter.DecimalToUIntPtr(-1m), converter(-1m));
            //Assert.Equal(ManualConverter.DecimalToUIntPtr(Decimal.MinValue), converter(Decimal.MinValue));
            //Assert.Equal(ManualConverter.DecimalToUIntPtr(Decimal.MaxValue), converter(Decimal.MaxValue));
        }
    }
}

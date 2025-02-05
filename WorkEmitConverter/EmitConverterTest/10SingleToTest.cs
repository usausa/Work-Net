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

            Assert.Equal(ManualConverter.SingleToByte(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToByte(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToByte(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToByte(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToByte(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<float, sbyte>();

            Assert.Equal(ManualConverter.SingleToSByte(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToSByte(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToSByte(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToSByte(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToSByte(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<float, char>();

            Assert.Equal(ManualConverter.SingleToChar(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToChar(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToChar(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToChar(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToChar(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<float, short>();

            Assert.Equal(ManualConverter.SingleToInt16(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToInt16(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToInt16(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToInt16(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToInt16(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<float, ushort>();

            Assert.Equal(ManualConverter.SingleToUInt16(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToUInt16(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToUInt16(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToUInt16(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUInt16(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<float, int>();

            Assert.Equal(ManualConverter.SingleToInt32(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToInt32(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToInt32(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToInt32(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToInt32(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<float, uint>();

            Assert.Equal(ManualConverter.SingleToUInt32(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToUInt32(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToUInt32(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToUInt32(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUInt32(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<float, long>();

            Assert.Equal(ManualConverter.SingleToInt64(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToInt64(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToInt64(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToInt64(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToInt64(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<float, ulong>();

            Assert.Equal(ManualConverter.SingleToUInt64(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToUInt64(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToUInt64(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToUInt64(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUInt64(Single.MaxValue), converter(Single.MaxValue));
        }

        // Nop float

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<float, double>();

            Assert.Equal(ManualConverter.SingleToDouble(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToDouble(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToDouble(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToDouble(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToDouble(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<float, decimal>();

            Assert.Equal(ManualConverter.SingleToDecimal(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToDecimal(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToDecimal(-1f), converter(-1f));
            //Assert.Equal(ManualConverter.SingleToDecimal(Single.MinValue), converter(Single.MinValue));
            //Assert.Equal(ManualConverter.SingleToDecimal(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<float, IntPtr>();

            Assert.Equal(ManualConverter.SingleToIntPtr(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToIntPtr(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToIntPtr(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToIntPtr(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToIntPtr(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<float, UIntPtr>();

            Assert.Equal(ManualConverter.SingleToUIntPtr(0f), converter(0f));
            Assert.Equal(ManualConverter.SingleToUIntPtr(1f), converter(1f));
            Assert.Equal(ManualConverter.SingleToUIntPtr(-1f), converter(-1f));
            Assert.Equal(ManualConverter.SingleToUIntPtr(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUIntPtr(Single.MaxValue), converter(Single.MaxValue));
        }
    }
}

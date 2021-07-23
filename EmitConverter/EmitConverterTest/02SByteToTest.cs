namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class SByteToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<sbyte, byte>();

            Assert.Equal(ManualConverter.SByteToByte(0), converter(0));
            Assert.Equal(ManualConverter.SByteToByte(1), converter(1));
            Assert.Equal(ManualConverter.SByteToByte(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToByte(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToByte(SByte.MaxValue), converter(SByte.MaxValue));
        }

        // Nop sbyte

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<sbyte, char>();

            Assert.Equal(ManualConverter.SByteToChar(0), converter(0));
            Assert.Equal(ManualConverter.SByteToChar(1), converter(1));
            Assert.Equal(ManualConverter.SByteToChar(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToChar(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToChar(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<sbyte, short>();

            Assert.Equal(ManualConverter.SByteToInt16(0), converter(0));
            Assert.Equal(ManualConverter.SByteToInt16(1), converter(1));
            Assert.Equal(ManualConverter.SByteToInt16(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToInt16(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToInt16(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<sbyte, ushort>();

            Assert.Equal(ManualConverter.SByteToUInt16(0), converter(0));
            Assert.Equal(ManualConverter.SByteToUInt16(1), converter(1));
            Assert.Equal(ManualConverter.SByteToUInt16(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToUInt16(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUInt16(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<sbyte, int>();

            Assert.Equal(ManualConverter.SByteToInt32(0), converter(0));
            Assert.Equal(ManualConverter.SByteToInt32(1), converter(1));
            Assert.Equal(ManualConverter.SByteToInt32(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToInt32(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToInt32(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<sbyte, uint>();

            Assert.Equal(ManualConverter.SByteToUInt32(0), converter(0));
            Assert.Equal(ManualConverter.SByteToUInt32(1), converter(1));
            Assert.Equal(ManualConverter.SByteToUInt32(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToUInt32(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUInt32(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<sbyte, long>();

            Assert.Equal(ManualConverter.SByteToInt64(0), converter(0));
            Assert.Equal(ManualConverter.SByteToInt64(1), converter(1));
            Assert.Equal(ManualConverter.SByteToInt64(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToInt64(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToInt64(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<sbyte, ulong>();

            Assert.Equal(ManualConverter.SByteToUInt64(0), converter(0));
            Assert.Equal(ManualConverter.SByteToUInt64(1), converter(1));
            Assert.Equal(ManualConverter.SByteToUInt64(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToUInt64(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUInt64(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<sbyte, float>();

            Assert.Equal(ManualConverter.SByteToSingle(0), converter(0));
            Assert.Equal(ManualConverter.SByteToSingle(1), converter(1));
            Assert.Equal(ManualConverter.SByteToSingle(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToSingle(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToSingle(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<sbyte, double>();

            Assert.Equal(ManualConverter.SByteToDouble(0), converter(0));
            Assert.Equal(ManualConverter.SByteToDouble(1), converter(1));
            Assert.Equal(ManualConverter.SByteToDouble(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToDouble(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToDouble(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<sbyte, decimal>();

            Assert.Equal(ManualConverter.SByteToDecimal(0), converter(0));
            Assert.Equal(ManualConverter.SByteToDecimal(1), converter(1));
            Assert.Equal(ManualConverter.SByteToDecimal(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToDecimal(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToDecimal(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<sbyte, IntPtr>();

            Assert.Equal(ManualConverter.SByteToIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.SByteToIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.SByteToIntPtr(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToIntPtr(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToIntPtr(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<sbyte, UIntPtr>();

            Assert.Equal(ManualConverter.SByteToUIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.SByteToUIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.SByteToUIntPtr(-1), converter(-1));
            Assert.Equal(ManualConverter.SByteToUIntPtr(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUIntPtr(SByte.MaxValue), converter(SByte.MaxValue));
        }
    }
}

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

            Assert.Equal(ManualConverter.Int16ToByte(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToByte(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToByte(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToByte(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToByte(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<short, sbyte>();

            Assert.Equal(ManualConverter.Int16ToSByte(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToSByte(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToSByte(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToSByte(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToSByte(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<short, char>();

            Assert.Equal(ManualConverter.Int16ToChar(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToChar(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToChar(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToChar(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToChar(Int16.MaxValue), converter(Int16.MaxValue));
        }

        // Nop short

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<short, ushort>();

            Assert.Equal(ManualConverter.Int16ToUInt16(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToUInt16(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToUInt16(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToUInt16(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUInt16(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<short, int>();

            Assert.Equal(ManualConverter.Int16ToInt32(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToInt32(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToInt32(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToInt32(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToInt32(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<short, uint>();

            Assert.Equal(ManualConverter.Int16ToUInt32(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToUInt32(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToUInt32(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToUInt32(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUInt32(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<short, long>();

            Assert.Equal(ManualConverter.Int16ToInt64(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToInt64(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToInt64(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToInt64(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToInt64(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<short, ulong>();

            Assert.Equal(ManualConverter.Int16ToUInt64(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToUInt64(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToUInt64(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToUInt64(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUInt64(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<short, float>();

            Assert.Equal(ManualConverter.Int16ToSingle(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToSingle(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToSingle(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToSingle(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToSingle(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<short, double>();

            Assert.Equal(ManualConverter.Int16ToDouble(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToDouble(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToDouble(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToDouble(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToDouble(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<short, decimal>();

            Assert.Equal(ManualConverter.Int16ToDecimal(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToDecimal(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToDecimal(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToDecimal(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToDecimal(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<short, IntPtr>();

            Assert.Equal(ManualConverter.Int16ToIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToIntPtr(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToIntPtr(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToIntPtr(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<short, UIntPtr>();

            Assert.Equal(ManualConverter.Int16ToUIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.Int16ToUIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.Int16ToUIntPtr(-1), converter(-1));
            Assert.Equal(ManualConverter.Int16ToUIntPtr(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUIntPtr(Int16.MaxValue), converter(Int16.MaxValue));
        }
    }
}

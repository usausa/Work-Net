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

            Assert.Equal(ManualConverter.Int32ToByte(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToByte(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToByte(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToByte(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToByte(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<int, sbyte>();

            Assert.Equal(ManualConverter.Int32ToSByte(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToSByte(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToSByte(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToSByte(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToSByte(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<int, char>();

            Assert.Equal(ManualConverter.Int32ToChar(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToChar(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToChar(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToChar(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToChar(Int32.MaxValue), converter(Int32.MaxValue));
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
            Assert.Equal(ManualConverter.Int32ToInt16(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToInt16(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToInt16(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToInt16(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToInt16(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<int, ushort>();

            Assert.Equal(ManualConverter.Int32ToUInt16(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToUInt16(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToUInt16(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToUInt16(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToUInt16(Int32.MaxValue), converter(Int32.MaxValue));
        }

        // Nop int

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<int, uint>();

            Assert.Equal(ManualConverter.Int32ToUInt32(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToUInt32(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToUInt32(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToUInt32(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToUInt32(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<int, long>();

            Assert.Equal(ManualConverter.Int32ToInt64(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToInt64(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToInt64(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToInt64(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToInt64(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<int, ulong>();

            Assert.Equal(ManualConverter.Int32ToUInt64(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToUInt64(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToUInt64(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToUInt64(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToUInt64(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<int, float>();

            Assert.Equal(ManualConverter.Int32ToSingle(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToSingle(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToSingle(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToSingle(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToSingle(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<int, double>();

            Assert.Equal(ManualConverter.Int32ToDouble(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToDouble(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToDouble(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToDouble(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToDouble(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<int, decimal>();

            Assert.Equal(ManualConverter.Int32ToDecimal(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToDecimal(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToDecimal(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToDecimal(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToDecimal(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<int, IntPtr>();

            Assert.Equal(ManualConverter.Int32ToIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToIntPtr(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToIntPtr(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToIntPtr(Int32.MaxValue), converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<int, UIntPtr>();

            Assert.Equal(ManualConverter.Int32ToUIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.Int32ToUIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.Int32ToUIntPtr(-1), converter(-1));
            Assert.Equal(ManualConverter.Int32ToUIntPtr(Int32.MinValue), converter(Int32.MinValue));
            Assert.Equal(ManualConverter.Int32ToUIntPtr(Int32.MaxValue), converter(Int32.MaxValue));
        }
    }
}

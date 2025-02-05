namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class Int64ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<long, byte>();

            Assert.Equal(ManualConverter.Int64ToByte(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToByte(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToByte(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToByte(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToByte(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<long, sbyte>();

            Assert.Equal(ManualConverter.Int64ToSByte(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToSByte(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToSByte(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToSByte(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToSByte(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<long, char>();

            Assert.Equal(ManualConverter.Int64ToChar(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToChar(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToChar(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToChar(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToChar(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<long, short>();

            Assert.Equal(ManualConverter.Int64ToInt16(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToInt16(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToInt16(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToInt16(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToInt16(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<long, ushort>();

            Assert.Equal(ManualConverter.Int64ToUInt16(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToUInt16(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToUInt16(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToUInt16(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUInt16(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<long, int>();

            Assert.Equal(ManualConverter.Int64ToInt32(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToInt32(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToInt32(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToInt32(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToInt32(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<long, uint>();

            Assert.Equal(ManualConverter.Int64ToUInt32(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToUInt32(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToUInt32(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToUInt32(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUInt32(Int64.MaxValue), converter(Int64.MaxValue));
        }

        // Nop long

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<long, ulong>();

            Assert.Equal(ManualConverter.Int64ToUInt64(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToUInt64(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToUInt64(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToUInt64(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUInt64(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<long, float>();

            Assert.Equal(ManualConverter.Int64ToSingle(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToSingle(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToSingle(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToSingle(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToSingle(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<long, double>();

            Assert.Equal(ManualConverter.Int64ToDouble(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToDouble(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToDouble(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToDouble(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToDouble(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<long, decimal>();

            Assert.Equal(ManualConverter.Int64ToDecimal(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToDecimal(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToDecimal(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToDecimal(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToDecimal(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<long, IntPtr>();

            Assert.Equal(ManualConverter.Int64ToIntPtr(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToIntPtr(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToIntPtr(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToIntPtr(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToIntPtr(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<long, UIntPtr>();

            Assert.Equal(ManualConverter.Int64ToUIntPtr(0L), converter(0L));
            Assert.Equal(ManualConverter.Int64ToUIntPtr(1L), converter(1L));
            Assert.Equal(ManualConverter.Int64ToUIntPtr(-1L), converter(-1L));
            Assert.Equal(ManualConverter.Int64ToUIntPtr(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUIntPtr(Int64.MaxValue), converter(Int64.MaxValue));
        }
    }
}

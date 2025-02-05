namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class UInt64ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<ulong, byte>();

            Assert.Equal(ManualConverter.UInt64ToByte(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToByte(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToByte(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToByte(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<ulong, sbyte>();

            Assert.Equal(ManualConverter.UInt64ToSByte(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToSByte(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToSByte(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToSByte(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<ulong, char>();

            Assert.Equal(ManualConverter.UInt64ToChar(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToChar(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToChar(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToChar(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<ulong, short>();

            Assert.Equal(ManualConverter.UInt64ToInt16(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToInt16(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToInt16(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToInt16(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<ulong, ushort>();

            Assert.Equal(ManualConverter.UInt64ToUInt16(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToUInt16(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToUInt16(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToUInt16(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<ulong, int>();

            Assert.Equal(ManualConverter.UInt64ToInt32(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToInt32(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToInt32(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToInt32(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<ulong, uint>();

            Assert.Equal(ManualConverter.UInt64ToUInt32(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToUInt32(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToUInt32(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToUInt32(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<ulong, long>();

            Assert.Equal(ManualConverter.UInt64ToInt64(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToInt64(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToInt64(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToInt64(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        // Nop ulong

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<ulong, float>();

            Assert.Equal(ManualConverter.UInt64ToSingle(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToSingle(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToSingle(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToSingle(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<ulong, double>();

            Assert.Equal(ManualConverter.UInt64ToDouble(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToDouble(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToDouble(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToDouble(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<ulong, decimal>();

            Assert.Equal(ManualConverter.UInt64ToDecimal(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToDecimal(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToDecimal(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToDecimal(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<ulong, IntPtr>();

            Assert.Equal(ManualConverter.UInt64ToIntPtr(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToIntPtr(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToIntPtr(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToIntPtr(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<ulong, UIntPtr>();

            Assert.Equal(ManualConverter.UInt64ToUIntPtr(0ul), converter(0ul));
            Assert.Equal(ManualConverter.UInt64ToUIntPtr(1ul), converter(1ul));
            Assert.Equal(ManualConverter.UInt64ToUIntPtr(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToUIntPtr(UInt64.MaxValue), converter(UInt64.MaxValue));
        }
    }
}

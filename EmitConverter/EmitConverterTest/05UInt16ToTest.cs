namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class UInt16ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<ushort, byte>();

            Assert.Equal(ManualConverter.UInt16ToByte(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToByte(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToByte(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToByte(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<ushort, sbyte>();

            Assert.Equal(ManualConverter.UInt16ToSByte(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToSByte(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToSByte(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToSByte(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<ushort, char>();

            Assert.Equal(ManualConverter.UInt16ToChar(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToChar(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToChar(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToChar(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<ushort, short>();

            Assert.Equal(ManualConverter.UInt16ToInt16(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToInt16(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToInt16(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToInt16(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        // Nop ushort

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<ushort, int>();

            Assert.Equal(ManualConverter.UInt16ToInt32(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToInt32(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToInt32(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToInt32(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<ushort, uint>();

            Assert.Equal(ManualConverter.UInt16ToUInt32(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToUInt32(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToUInt32(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToUInt32(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<ushort, long>();

            Assert.Equal(ManualConverter.UInt16ToInt64(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToInt64(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToInt64(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToInt64(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<ushort, ulong>();

            Assert.Equal(ManualConverter.UInt16ToUInt64(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToUInt64(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToUInt64(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToUInt64(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<ushort, float>();

            Assert.Equal(ManualConverter.UInt16ToSingle(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToSingle(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToSingle(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToSingle(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<ushort, double>();

            Assert.Equal(ManualConverter.UInt16ToDouble(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToDouble(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToDouble(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToDouble(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<ushort, decimal>();

            Assert.Equal(ManualConverter.UInt16ToDecimal(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToDecimal(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToDecimal(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToDecimal(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<ushort, IntPtr>();

            Assert.Equal(ManualConverter.UInt16ToIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToIntPtr(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToIntPtr(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<ushort, UIntPtr>();

            Assert.Equal(ManualConverter.UInt16ToUIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.UInt16ToUIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.UInt16ToUIntPtr(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToUIntPtr(UInt16.MaxValue), converter(UInt16.MaxValue));
        }
    }
}

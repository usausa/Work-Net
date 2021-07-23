namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class ByteToTest
    {
        // Nop byte

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<byte, sbyte>();

            Assert.Equal(ManualConverter.ByteToSByte(0), converter(0));
            Assert.Equal(ManualConverter.ByteToSByte(1), converter(1));
            Assert.Equal(ManualConverter.ByteToSByte(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToSByte(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<byte, char>();

            Assert.Equal(ManualConverter.ByteToChar(0), converter(0));
            Assert.Equal(ManualConverter.ByteToChar(1), converter(1));
            Assert.Equal(ManualConverter.ByteToChar(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToChar(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<byte, short>();

            Assert.Equal(ManualConverter.ByteToInt16(0), converter(0));
            Assert.Equal(ManualConverter.ByteToInt16(1), converter(1));
            Assert.Equal(ManualConverter.ByteToInt16(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToInt16(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<byte, ushort>();

            Assert.Equal(ManualConverter.ByteToUInt16(0), converter(0));
            Assert.Equal(ManualConverter.ByteToUInt16(1), converter(1));
            Assert.Equal(ManualConverter.ByteToUInt16(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUInt16(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<byte, int>();

            Assert.Equal(ManualConverter.ByteToInt32(0), converter(0));
            Assert.Equal(ManualConverter.ByteToInt32(1), converter(1));
            Assert.Equal(ManualConverter.ByteToInt32(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToInt32(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<byte, uint>();

            Assert.Equal(ManualConverter.ByteToUInt32(0), converter(0));
            Assert.Equal(ManualConverter.ByteToUInt32(1), converter(1));
            Assert.Equal(ManualConverter.ByteToUInt32(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUInt32(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<byte, long>();

            Assert.Equal(ManualConverter.ByteToInt64(0), converter(0));
            Assert.Equal(ManualConverter.ByteToInt64(1), converter(1));
            Assert.Equal(ManualConverter.ByteToInt64(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToInt64(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<byte, ulong>();

            Assert.Equal(ManualConverter.ByteToUInt64(0), converter(0));
            Assert.Equal(ManualConverter.ByteToUInt64(1), converter(1));
            Assert.Equal(ManualConverter.ByteToUInt64(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUInt64(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<byte, float>();

            Assert.Equal(ManualConverter.ByteToSingle(0), converter(0));
            Assert.Equal(ManualConverter.ByteToSingle(1), converter(1));
            Assert.Equal(ManualConverter.ByteToSingle(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToSingle(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<byte, double>();

            Assert.Equal(ManualConverter.ByteToDouble(0), converter(0));
            Assert.Equal(ManualConverter.ByteToDouble(1), converter(1));
            Assert.Equal(ManualConverter.ByteToDouble(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToDouble(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<byte, decimal>();

            Assert.Equal(ManualConverter.ByteToDecimal(0), converter(0));
            Assert.Equal(ManualConverter.ByteToDecimal(1), converter(1));
            Assert.Equal(ManualConverter.ByteToDecimal(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToDecimal(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<byte, IntPtr>();

            Assert.Equal(ManualConverter.ByteToIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.ByteToIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.ByteToIntPtr(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToIntPtr(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<byte, UIntPtr>();

            Assert.Equal(ManualConverter.ByteToUIntPtr(0), converter(0));
            Assert.Equal(ManualConverter.ByteToUIntPtr(1), converter(1));
            Assert.Equal(ManualConverter.ByteToUIntPtr(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUIntPtr(Byte.MaxValue), converter(Byte.MaxValue));
        }
    }
}

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

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToSByte(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToSByte(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<byte, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToChar(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToChar(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<byte, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToInt16(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToInt16(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<byte, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToUInt16(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUInt16(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<byte, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToInt32(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToInt32(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<byte, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToUInt32(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUInt32(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<byte, long>();

            // Base
            Assert.Equal(0L, converter(0));
            Assert.Equal(1L, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToInt64(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToInt64(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<byte, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToUInt64(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUInt64(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<byte, float>();

            // Base
            Assert.Equal(0f, converter(0));
            Assert.Equal(1f, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToSingle(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToSingle(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<byte, double>();

            // Base
            Assert.Equal(0d, converter(0));
            Assert.Equal(1d, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToDouble(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToDouble(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<byte, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToDecimal(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToDecimal(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<byte, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToIntPtr(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToIntPtr(Byte.MaxValue), converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<byte, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.ByteToUIntPtr(Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(ManualConverter.ByteToUIntPtr(Byte.MaxValue), converter(Byte.MaxValue));
        }
    }
}

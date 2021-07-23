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

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Byte.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToByte(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToByte(SByte.MaxValue), converter(SByte.MaxValue));
        }

        // Nop sbyte

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<sbyte, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Char.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToChar(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToChar(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<sbyte, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToInt16(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToInt16(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<sbyte, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(UInt16.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToUInt16(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUInt16(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<sbyte, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToInt32(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToInt32(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<sbyte, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            Assert.Equal(UInt32.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToUInt32(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUInt32(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<sbyte, long>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToInt64(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToInt64(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<sbyte, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToUInt64(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUInt64(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<sbyte, float>();

            // Base
            Assert.Equal(0f, converter(0));
            Assert.Equal(1f, converter(1));
            Assert.Equal(-1f, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToSingle(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToSingle(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<sbyte, double>();

            // Base
            Assert.Equal(0d, converter(0));
            Assert.Equal(1d, converter(1));
            Assert.Equal(-1d, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToDouble(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToDouble(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<sbyte, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToDecimal(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToDecimal(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<sbyte, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToIntPtr(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToIntPtr(SByte.MaxValue), converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<sbyte, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.SByteToUIntPtr(SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal(ManualConverter.SByteToUIntPtr(SByte.MaxValue), converter(SByte.MaxValue));
        }
    }
}

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

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(Byte.MaxValue, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToByte(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToByte(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<float, sbyte>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToSByte(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToSByte(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<float, char>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(Char.MaxValue, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToChar(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToChar(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<float, short>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToInt16(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToInt16(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<float, ushort>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(UInt16.MaxValue, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToUInt16(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUInt16(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<float, int>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToInt32(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToInt32(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<float, uint>();

            // Base
            Assert.Equal(0ul, converter(0f));
            Assert.Equal(1ul, converter(1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToUInt32(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUInt32(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<float, long>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToInt64(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToInt64(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<float, ulong>();

            // Base
            Assert.Equal(0ul, converter(0f));
            Assert.Equal(1ul, converter(1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToUInt64(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUInt64(Single.MaxValue), converter(Single.MaxValue));
        }

        // Nop float

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<float, double>();

            // Base
            Assert.Equal(0, converter(0f));
            Assert.Equal(1, converter(1f));
            Assert.Equal(-1, converter(-1f));
            // Min/Max
            Assert.Equal(ManualConverter.SingleToDouble(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToDouble(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<float, decimal>();

            // Base
            Assert.Equal(0m, converter(0f));
            Assert.Equal(1m, converter(1f));
            //Assert.Equal(ManualConverter.SingleToDecimal(Single.MinValue), converter(Single.MinValue));
            //Assert.Equal(ManualConverter.SingleToDecimal(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<float, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0f));
            Assert.Equal((IntPtr)1, converter(1f));
            Assert.Equal(ManualConverter.SingleToIntPtr(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToIntPtr(Single.MaxValue), converter(Single.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<float, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0f));
            Assert.Equal((UIntPtr)1, converter(1f));
            Assert.Equal(ManualConverter.SingleToUIntPtr(Single.MinValue), converter(Single.MinValue));
            Assert.Equal(ManualConverter.SingleToUIntPtr(Single.MaxValue), converter(Single.MaxValue));
        }
    }
}

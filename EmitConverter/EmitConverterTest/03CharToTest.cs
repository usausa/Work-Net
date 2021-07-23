namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class CharToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<char, byte>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToByte(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToByte(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<char, sbyte>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToSByte(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToSByte(Char.MaxValue), converter(Char.MaxValue));
        }

        // Nop char

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<char, short>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToInt16(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToInt16(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<char, ushort>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToUInt16(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUInt16(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<char, int>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToInt32(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToInt32(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<char, uint>();

            // Base
            Assert.Equal(0u, converter((char)0));
            Assert.Equal(1u, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToUInt32(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUInt32(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<char, long>();

            // Base
            Assert.Equal(0L, converter((char)0));
            Assert.Equal(1L, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToInt64(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToInt64(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<char, ulong>();

            // Base
            Assert.Equal(0ul, converter((char)0));
            Assert.Equal(1ul, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToUInt64(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUInt64(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<char, float>();

            // Base
            Assert.Equal(0f, converter((char)0));
            Assert.Equal(1f, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToSingle(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToSingle(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<char, double>();

            // Base
            Assert.Equal(0d, converter((char)0));
            Assert.Equal(1d, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToDouble(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToDouble(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<char, decimal>();

            // Base
            Assert.Equal(0m, converter((char)0));
            Assert.Equal(1m, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToDecimal(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToDecimal(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<char, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter((char)0));
            Assert.Equal((IntPtr)1, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToIntPtr(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToIntPtr(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<char, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter((char)0));
            Assert.Equal((UIntPtr)1, converter((char)1));
            // Min/Max
            Assert.Equal(ManualConverter.CharToUIntPtr(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUIntPtr(Char.MaxValue), converter(Char.MaxValue));
        }
    }
}

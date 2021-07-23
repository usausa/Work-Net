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

            Assert.Equal(ManualConverter.CharToByte((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToByte((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToByte(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToByte(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<char, sbyte>();

            Assert.Equal(ManualConverter.CharToSByte((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToSByte((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToSByte(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToSByte(Char.MaxValue), converter(Char.MaxValue));
        }

        // Nop char

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<char, short>();

            Assert.Equal(ManualConverter.CharToInt16((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToInt16((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToInt16(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToInt16(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<char, ushort>();

            Assert.Equal(ManualConverter.CharToUInt16((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToUInt16((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToUInt16(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUInt16(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<char, int>();

            Assert.Equal(ManualConverter.CharToInt32((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToInt32((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToInt32(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToInt32(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<char, uint>();

            Assert.Equal(ManualConverter.CharToUInt32((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToUInt32((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToUInt32(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUInt32(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<char, long>();

            Assert.Equal(ManualConverter.CharToInt64((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToInt64((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToInt64(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToInt64(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<char, ulong>();

            Assert.Equal(ManualConverter.CharToUInt64((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToUInt64((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToUInt64(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUInt64(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<char, float>();

            Assert.Equal(ManualConverter.CharToSingle((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToSingle((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToSingle(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToSingle(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<char, double>();

            Assert.Equal(ManualConverter.CharToDouble((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToDouble((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToDouble(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToDouble(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<char, decimal>();

            Assert.Equal(ManualConverter.CharToDecimal((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToDecimal((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToDecimal(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToDecimal(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<char, IntPtr>();

            Assert.Equal(ManualConverter.CharToIntPtr((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToIntPtr((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToIntPtr(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToIntPtr(Char.MaxValue), converter(Char.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<char, UIntPtr>();

            Assert.Equal(ManualConverter.CharToUIntPtr((char)0), converter((char)0));
            Assert.Equal(ManualConverter.CharToUIntPtr((char)1), converter((char)1));
            Assert.Equal(ManualConverter.CharToUIntPtr(Char.MinValue), converter(Char.MinValue));
            Assert.Equal(ManualConverter.CharToUIntPtr(Char.MaxValue), converter(Char.MaxValue));
        }
    }
}

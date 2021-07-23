namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class Int16ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<short, byte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Byte.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToByte(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToByte(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<short, sbyte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToSByte(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToSByte(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<short, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Char.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToChar(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToChar(Int16.MaxValue), converter(Int16.MaxValue));
        }

        // Nop short

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<short, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(UInt16.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToUInt16(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUInt16(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<short, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToInt32(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToInt32(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<short, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            Assert.Equal(UInt32.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToUInt32(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUInt32(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<short, long>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToInt64(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToInt64(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<short, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToUInt64(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUInt64(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<short, float>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToSingle(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToSingle(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<short, double>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToDouble(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToDouble(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<short, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToDecimal(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToDecimal(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<short, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToIntPtr(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToIntPtr(Int16.MaxValue), converter(Int16.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<short, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.Int16ToUIntPtr(Int16.MinValue), converter(Int16.MinValue));
            Assert.Equal(ManualConverter.Int16ToUIntPtr(Int16.MaxValue), converter(Int16.MaxValue));
        }
    }
}

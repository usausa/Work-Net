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

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToByte(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToByte(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<ushort, sbyte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToSByte(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToSByte(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<ushort, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToChar(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToChar(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<ushort, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToInt16(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToInt16(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        // Nop ushort

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<ushort, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToInt32(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToInt32(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<ushort, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToUInt32(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToUInt32(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<ushort, long>();

            // Base
            Assert.Equal(0L, converter(0));
            Assert.Equal(1L, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToInt64(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToInt64(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<ushort, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToUInt64(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToUInt64(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<ushort, float>();

            // Base
            Assert.Equal(0f, converter(0));
            Assert.Equal(1f, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToSingle(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToSingle(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<ushort, double>();

            // Base
            Assert.Equal(0d, converter(0));
            Assert.Equal(1d, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToDouble(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToDouble(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<ushort, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToDecimal(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToDecimal(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<ushort, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToIntPtr(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToIntPtr(UInt16.MaxValue), converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<ushort, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal(ManualConverter.UInt16ToUIntPtr(UInt16.MinValue), converter(UInt16.MinValue));
            Assert.Equal(ManualConverter.UInt16ToUIntPtr(UInt16.MaxValue), converter(UInt16.MaxValue));
        }
    }
}

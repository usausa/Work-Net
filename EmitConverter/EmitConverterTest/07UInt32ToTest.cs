using System;

using Xunit;

namespace EmitConverterTest
{
    public class UInt32ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<uint, byte>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToByte(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToByte(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<uint, sbyte>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToSByte(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToSByte(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<uint, char>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToChar(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToChar(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<uint, short>();

            // Base
            Assert.Equal(0L, converter(0u));
            Assert.Equal(1L, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToInt16(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToInt16(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<uint, ushort>();

            // Base
            Assert.Equal(0ul, converter(0u));
            Assert.Equal(1ul, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToUInt16(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToUInt16(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<uint, int>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToInt32(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToInt32(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        // Nop uint

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<uint, long>();

            // Base
            Assert.Equal(0L, converter(0u));
            Assert.Equal(1L, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToInt64(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToInt64(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<uint, uint>();

            // Base
            Assert.Equal(0ul, converter(0u));
            Assert.Equal(1ul, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToUInt64(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToUInt64(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<uint, float>();

            // Base
            Assert.Equal(0f, converter(0u));
            Assert.Equal(1f, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToSingle(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToSingle(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<uint, double>();

            // Base
            Assert.Equal(0d, converter(0u));
            Assert.Equal(1d, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToDouble(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToDouble(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<uint, decimal>();

            // Base
            Assert.Equal(0m, converter(0u));
            Assert.Equal(1m, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToDecimal(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToDecimal(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<uint, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0u));
            Assert.Equal((IntPtr)1, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToIntPtr(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToIntPtr(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<uint, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0u));
            Assert.Equal((UIntPtr)1, converter(1u));
            // Min/Max
            Assert.Equal(ManualConverter.UInt32ToUIntPtr(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToUIntPtr(UInt32.MaxValue), converter(UInt32.MaxValue));
        }
    }
}

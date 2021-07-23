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

            Assert.Equal(ManualConverter.UInt32ToByte(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToByte(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToByte(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToByte(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<uint, sbyte>();

            Assert.Equal(ManualConverter.UInt32ToSByte(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToSByte(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToSByte(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToSByte(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<uint, char>();

            Assert.Equal(ManualConverter.UInt32ToChar(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToChar(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToChar(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToChar(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<uint, short>();

            Assert.Equal(ManualConverter.UInt32ToInt16(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToInt16(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToInt16(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToInt16(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<uint, ushort>();

            Assert.Equal(ManualConverter.UInt32ToUInt16(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToUInt16(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToUInt16(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToUInt16(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<uint, int>();

            Assert.Equal(ManualConverter.UInt32ToInt32(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToInt32(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToInt32(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToInt32(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        // Nop uint

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<uint, long>();

            Assert.Equal(ManualConverter.UInt32ToInt64(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToInt64(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToInt64(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToInt64(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<uint, uint>();

            Assert.Equal(ManualConverter.UInt32ToUInt64(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToUInt64(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToUInt64(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToUInt64(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<uint, float>();

            Assert.Equal(ManualConverter.UInt32ToSingle(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToSingle(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToSingle(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToSingle(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<uint, double>();

            Assert.Equal(ManualConverter.UInt32ToDouble(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToDouble(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToDouble(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToDouble(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<uint, decimal>();

            Assert.Equal(ManualConverter.UInt32ToDecimal(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToDecimal(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToDecimal(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToDecimal(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<uint, IntPtr>();

            Assert.Equal(ManualConverter.UInt32ToIntPtr(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToIntPtr(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToIntPtr(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToIntPtr(UInt32.MaxValue), converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<uint, UIntPtr>();

            Assert.Equal(ManualConverter.UInt32ToUIntPtr(0u), converter(0u));
            Assert.Equal(ManualConverter.UInt32ToUIntPtr(1u), converter(1u));
            Assert.Equal(ManualConverter.UInt32ToUIntPtr(UInt32.MinValue), converter(UInt32.MinValue));
            Assert.Equal(ManualConverter.UInt32ToUIntPtr(UInt32.MaxValue), converter(UInt32.MaxValue));
        }
    }
}

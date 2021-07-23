namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class UInt64ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<ulong, byte>();

            // Base
            Assert.Equal(0, converter(0ul));
            Assert.Equal(1, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToByte(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToByte(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<ulong, sbyte>();

            // Base
            Assert.Equal(0, converter(0ul));
            Assert.Equal(1, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToSByte(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToSByte(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<ulong, char>();

            // Base
            Assert.Equal(0, converter(0ul));
            Assert.Equal(1, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToChar(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToChar(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<ulong, short>();

            // Base
            Assert.Equal(0L, converter(0ul));
            Assert.Equal(1L, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToInt16(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToInt16(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<ulong, ushort>();

            // Base
            Assert.Equal(0ul, converter(0ul));
            Assert.Equal(1ul, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToUInt16(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToUInt16(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<ulong, int>();

            // Base
            Assert.Equal(0, converter(0ul));
            Assert.Equal(1, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToInt32(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToInt32(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<ulong, uint>();

            // Base
            Assert.Equal(0u, converter(0ul));
            Assert.Equal(1u, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToUInt32(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToUInt32(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<ulong, long>();

            // Base
            Assert.Equal(0L, converter(0ul));
            Assert.Equal(1L, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToInt64(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToInt64(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        // Nop ulong

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<ulong, float>();

            // Base
            Assert.Equal(0f, converter(0ul));
            Assert.Equal(1f, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToSingle(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToSingle(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<ulong, double>();

            // Base
            Assert.Equal(0d, converter(0ul));
            Assert.Equal(1d, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToDouble(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToDouble(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<ulong, decimal>();

            // Base
            Assert.Equal(0m, converter(0ul));
            Assert.Equal(1m, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToDecimal(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToDecimal(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<ulong, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0ul));
            Assert.Equal((IntPtr)1, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToIntPtr(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToIntPtr(UInt64.MaxValue), converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<ulong, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0ul));
            Assert.Equal((UIntPtr)1, converter(1ul));
            // Min/Max
            Assert.Equal(ManualConverter.UInt64ToUIntPtr(UInt64.MinValue), converter(UInt64.MinValue));
            Assert.Equal(ManualConverter.UInt64ToUIntPtr(UInt64.MaxValue), converter(UInt64.MaxValue));
        }
    }
}

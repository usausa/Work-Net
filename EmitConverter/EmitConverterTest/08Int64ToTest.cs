namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class Int64ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<long, byte>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(Byte.MaxValue, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToByte(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToByte(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<long, sbyte>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToSByte(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToSByte(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<long, char>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(Char.MaxValue, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToChar(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToChar(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<long, short>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToInt16(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToInt16(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<long, ushort>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(UInt16.MaxValue, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToUInt16(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUInt16(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<long, int>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToInt32(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToInt32(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<long, uint>();

            // Base
            Assert.Equal(0u, converter(0L));
            Assert.Equal(1u, converter(1L));
            Assert.Equal(unchecked((uint)Int64.MaxValue), converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToUInt32(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUInt32(Int64.MaxValue), converter(Int64.MaxValue));
        }

        // Nop long

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<long, ulong>();

            // Base
            Assert.Equal(0ul, converter(0L));
            Assert.Equal(1ul, converter(1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToUInt64(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUInt64(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<long, float>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToSingle(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToSingle(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<long, double>();

            // Base
            Assert.Equal(0, converter(0L));
            Assert.Equal(1, converter(1L));
            Assert.Equal(-1, converter(-1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToDouble(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToDouble(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<long, decimal>();

            // Base
            Assert.Equal(0m, converter(0L));
            Assert.Equal(1m, converter(1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToDecimal(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToDecimal(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<long, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0L));
            Assert.Equal((IntPtr)1, converter(1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToIntPtr(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToIntPtr(Int64.MaxValue), converter(Int64.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<long, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0L));
            Assert.Equal((UIntPtr)1, converter(1L));
            // Min/Max
            Assert.Equal(ManualConverter.Int64ToUIntPtr(Int64.MinValue), converter(Int64.MinValue));
            Assert.Equal(ManualConverter.Int64ToUIntPtr(Int64.MaxValue), converter(Int64.MaxValue));
        }
    }
}

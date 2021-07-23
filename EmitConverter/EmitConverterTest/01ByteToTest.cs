namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class ByteToTest
    {
        // Nop byte

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<byte, sbyte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(0, converter(Byte.MinValue));
            Assert.Equal(-1, converter(Byte.MaxValue));
            // Boundary
            Assert.Equal(SByte.MinValue, converter(unchecked((byte)SByte.MinValue)));
            Assert.Equal(SByte.MaxValue, converter((byte)SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<byte, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(0, converter(Byte.MinValue));
            Assert.Equal(255, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<byte, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(0, converter(Byte.MinValue));
            Assert.Equal(255, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<byte, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(0, converter(Byte.MinValue));
            Assert.Equal(255, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<byte, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(0, converter(Byte.MinValue));
            Assert.Equal(255, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<byte, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            // Min/Max
            Assert.Equal(0u, converter(Byte.MinValue));
            Assert.Equal(255u, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<byte, long>();

            // Base
            Assert.Equal(0L, converter(0));
            Assert.Equal(1L, converter(1));
            // Min/Max
            Assert.Equal(0L, converter(Byte.MinValue));
            Assert.Equal(255L, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<byte, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(0ul, converter(Byte.MinValue));
            Assert.Equal(255ul, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<byte, float>();

            // Base
            Assert.Equal(0f, converter(0));
            Assert.Equal(1f, converter(1));
            // Min/Max
            Assert.Equal(0f, converter(Byte.MinValue));
            Assert.Equal(255f, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<byte, double>();

            // Base
            Assert.Equal(0d, converter(0));
            Assert.Equal(1d, converter(1));
            // Min/Max
            Assert.Equal(0d, converter(Byte.MinValue));
            Assert.Equal(255d, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<byte, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(0m, converter(Byte.MinValue));
            Assert.Equal(255m, converter(Byte.MaxValue));
        }

        // TODO decimal
        // TODO IntPtr
        // TODO UIntPtr
    }
}

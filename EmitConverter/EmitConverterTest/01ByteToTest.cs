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
            Assert.Equal((sbyte)Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(unchecked((sbyte)Byte.MaxValue), converter(Byte.MaxValue));
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
            Assert.Equal((char)Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal((char)Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<byte, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<byte, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<byte, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<byte, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<byte, long>();

            // Base
            Assert.Equal(0L, converter(0));
            Assert.Equal(1L, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<byte, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<byte, float>();

            // Base
            Assert.Equal(0f, converter(0));
            Assert.Equal(1f, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<byte, double>();

            // Base
            Assert.Equal(0d, converter(0));
            Assert.Equal(1d, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<byte, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<byte, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((IntPtr)Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal((IntPtr)Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<byte, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((UIntPtr)Byte.MinValue, converter(Byte.MinValue));
            Assert.Equal((UIntPtr)Byte.MaxValue, converter(Byte.MaxValue));
        }
    }
}

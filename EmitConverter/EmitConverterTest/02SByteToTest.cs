namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class SByteToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<sbyte, byte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(255, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((byte)SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal((byte)SByte.MaxValue, converter(SByte.MaxValue));
            // Boundary
            Assert.Equal(Byte.MinValue, converter((sbyte)Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(unchecked((sbyte)Byte.MaxValue)));
        }

        // Nop sbyte

        // TODO char
        // TODO short
        // TODO ushort
        // TODO int
        // TODO uint
        // TODO long
        // TODO ulong
        // TODO float
        // TODO double
        // TODO decimal
        // TODO IntPtr
        // TODO UIntPtr
    }
}

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

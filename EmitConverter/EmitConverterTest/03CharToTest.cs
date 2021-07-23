namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class CharToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<char, byte>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal((byte)Char.MinValue, converter(Char.MinValue));
            Assert.Equal(unchecked((byte)Char.MaxValue), converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal((byte)unchecked((sbyte)(char)Byte.MinValue), converter((char)Byte.MinValue));
            Assert.Equal((byte)(char)Byte.MaxValue, converter((char)Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<char, sbyte>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal((sbyte)Char.MinValue, converter(Char.MinValue));
            Assert.Equal(unchecked((sbyte)Char.MaxValue), converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)unchecked((char)SByte.MinValue)), converter(unchecked((char)SByte.MinValue)));
            Assert.Equal((sbyte)(char)SByte.MaxValue, converter((char)SByte.MaxValue));
        }

        // Nop char

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<char, short>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal((short)Char.MinValue, converter(Char.MinValue));
            Assert.Equal(unchecked((short)Char.MaxValue), converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)unchecked((char)Int16.MinValue)), converter(unchecked((char)Int16.MinValue)));
            Assert.Equal((short)unchecked((char)Int16.MaxValue), converter(unchecked((char)Int16.MaxValue)));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<char, ushort>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal((char)UInt16.MinValue, converter((char)UInt16.MinValue));
            Assert.Equal((char)UInt16.MaxValue, converter((char)UInt16.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<char, int>();

            // Base
            Assert.Equal(0, converter((char)0));
            Assert.Equal(1, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((char)Int32.MinValue), converter(unchecked((char)Int32.MinValue)));
            Assert.Equal(unchecked((char)Int32.MaxValue), converter(unchecked((char)Int32.MaxValue)));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<char, uint>();

            // Base
            Assert.Equal(0u, converter((char)0));
            Assert.Equal(1u, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((char)UInt32.MinValue), converter(unchecked((char)UInt32.MinValue)));
            Assert.Equal(unchecked((char)UInt32.MaxValue), converter(unchecked((char)UInt32.MaxValue)));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<char, long>();

            // Base
            Assert.Equal(0L, converter((char)0));
            Assert.Equal(1L, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((char)Int64.MinValue), converter(unchecked((char)Int64.MinValue)));
            Assert.Equal(unchecked((char)Int64.MaxValue), converter(unchecked((char)Int64.MaxValue)));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<char, ulong>();

            // Base
            Assert.Equal(0ul, converter((char)0));
            Assert.Equal(1ul, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((char)UInt64.MinValue), converter(unchecked((char)UInt64.MinValue)));
            Assert.Equal(unchecked((char)UInt64.MaxValue), converter(unchecked((char)UInt64.MaxValue)));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<char, float>();

            // Base
            Assert.Equal(0f, converter((char)0));
            Assert.Equal(1f, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((char)Single.MinValue), converter(unchecked((char)Single.MinValue)));
            Assert.Equal(unchecked((char)Single.MaxValue), converter(unchecked((char)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<char, double>();

            // Base
            Assert.Equal(0d, converter((char)0));
            Assert.Equal(1d, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((char)Double.MinValue), converter(unchecked((char)Double.MinValue)));
            Assert.Equal(unchecked((char)Double.MaxValue), converter(unchecked((char)Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<char, decimal>();

            // Base
            Assert.Equal(0m, converter((char)0));
            Assert.Equal(1m, converter((char)1));
            // Min/Max
            Assert.Equal(Char.MinValue, converter(Char.MinValue));
            Assert.Equal(Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<char, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter((char)0));
            Assert.Equal((IntPtr)1, converter((char)1));
            // Min/Max
            Assert.Equal((IntPtr)Char.MinValue, converter(Char.MinValue));
            Assert.Equal((IntPtr)Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<char, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter((char)0));
            Assert.Equal((UIntPtr)1, converter((char)1));
            // Min/Max
            Assert.Equal((UIntPtr)Char.MinValue, converter(Char.MinValue));
            Assert.Equal((UIntPtr)Char.MaxValue, converter(Char.MaxValue));
        }
    }
}

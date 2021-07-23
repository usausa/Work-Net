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
            Assert.Equal((byte)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(unchecked((byte)UInt16.MaxValue), converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal((byte)unchecked((sbyte)Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<ushort, sbyte>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal((sbyte)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(unchecked((sbyte)UInt16.MaxValue), converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)unchecked((ushort)SByte.MinValue)), converter(unchecked((ushort)SByte.MinValue)));
            Assert.Equal((sbyte)(ushort)SByte.MaxValue, converter((ushort)SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<ushort, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal((ushort)Char.MinValue, converter(Char.MinValue));
            Assert.Equal((ushort)Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<ushort, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            // Min/Max
            Assert.Equal((short)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(unchecked((short)UInt16.MaxValue), converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)unchecked((ushort)Int16.MinValue)), converter(unchecked((ushort)Int16.MinValue)));
            Assert.Equal((short)unchecked((ushort)Int16.MaxValue), converter(unchecked((ushort)Int16.MaxValue)));
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
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((ushort)Int32.MinValue), converter(unchecked((ushort)Int32.MinValue)));
            Assert.Equal(unchecked((ushort)Int32.MaxValue), converter(unchecked((ushort)Int32.MaxValue)));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<ushort, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            // Min/Max
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((ushort)UInt32.MinValue), converter(unchecked((ushort)UInt32.MinValue)));
            Assert.Equal(unchecked((ushort)UInt32.MaxValue), converter(unchecked((ushort)UInt32.MaxValue)));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<ushort, long>();

            // Base
            Assert.Equal(0L, converter(0));
            Assert.Equal(1L, converter(1));
            // Min/Max
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((ushort)Int64.MinValue), converter(unchecked((ushort)Int64.MinValue)));
            Assert.Equal(unchecked((ushort)Int64.MaxValue), converter(unchecked((ushort)Int64.MaxValue)));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<ushort, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((ushort)UInt64.MinValue), converter(unchecked((ushort)UInt64.MinValue)));
            Assert.Equal(unchecked((ushort)UInt64.MaxValue), converter(unchecked((ushort)UInt64.MaxValue)));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<ushort, float>();

            // Base
            Assert.Equal(0f, converter(0));
            Assert.Equal(1f, converter(1));
            // Min/Max
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((ushort)Single.MinValue), converter(unchecked((ushort)Single.MinValue)));
            Assert.Equal(unchecked((ushort)Single.MaxValue), converter(unchecked((ushort)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<ushort, double>();

            // Base
            Assert.Equal(0d, converter(0));
            Assert.Equal(1d, converter(1));
            // Min/Max
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((ushort)Double.MinValue), converter(unchecked((ushort) Double.MinValue)));
            Assert.Equal(unchecked((ushort)Double.MaxValue), converter(unchecked((ushort) Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<ushort, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal(UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<ushort, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((IntPtr)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((IntPtr)UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<ushort, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((UIntPtr)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((UIntPtr)UInt16.MaxValue, converter(UInt16.MaxValue));
        }
    }
}

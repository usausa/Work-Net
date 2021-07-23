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
            Assert.Equal(Byte.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((byte)SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal((byte)SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal((byte)(sbyte)Byte.MinValue, converter((sbyte)Byte.MinValue));
            Assert.Equal(unchecked((byte)unchecked((sbyte)Byte.MaxValue)), converter(unchecked((sbyte)Byte.MaxValue)));
        }

        // Nop sbyte

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<sbyte, char>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(Char.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((char)SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal((char)SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal((char)(sbyte)Char.MinValue, converter((sbyte)Char.MinValue));
            Assert.Equal(unchecked((char)unchecked((sbyte)Char.MaxValue)), converter(unchecked((sbyte)Char.MaxValue)));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<sbyte, short>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)Int16.MinValue), converter(unchecked((sbyte)Int16.MinValue)));
            Assert.Equal(unchecked((sbyte)Int16.MaxValue), converter(unchecked((sbyte)Int16.MaxValue)));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<sbyte, ushort>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(UInt16.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((ushort)SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal((ushort)SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal((ushort)(sbyte)UInt16.MinValue, converter((sbyte)UInt16.MinValue));
            Assert.Equal(unchecked((ushort)unchecked((sbyte)UInt16.MaxValue)), converter(unchecked((sbyte)UInt16.MaxValue)));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<sbyte, int>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)Int32.MinValue), converter(unchecked((sbyte)Int32.MinValue)));
            Assert.Equal(unchecked((sbyte)Int32.MaxValue), converter(unchecked((sbyte)Int32.MaxValue)));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<sbyte, uint>();

            // Base
            Assert.Equal(0u, converter(0));
            Assert.Equal(1u, converter(1));
            Assert.Equal(UInt32.MaxValue, converter(-1));
            // Min/Max
            Assert.Equal(unchecked((uint)SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal((uint)SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal((uint)(sbyte)UInt32.MinValue, converter((sbyte)UInt32.MinValue));
            Assert.Equal(unchecked((uint)unchecked((sbyte)UInt32.MaxValue)), converter(unchecked((sbyte)UInt32.MaxValue)));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<sbyte, long>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)Int64.MinValue), converter(unchecked((sbyte)Int64.MinValue)));
            Assert.Equal(unchecked((sbyte)Int64.MaxValue), converter(unchecked((sbyte)Int64.MaxValue)));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<sbyte, ulong>();

            // Base
            Assert.Equal(0ul, converter(0));
            Assert.Equal(1ul, converter(1));
            // Min/Max
            Assert.Equal(unchecked((ulong)SByte.MinValue), converter(SByte.MinValue));
            Assert.Equal((ulong)SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)unchecked((sbyte)UInt64.MinValue), converter(unchecked((sbyte)UInt64.MinValue)));
            Assert.Equal(unchecked((ulong)unchecked((sbyte)UInt64.MaxValue)), converter(unchecked((sbyte)UInt64.MaxValue)));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<sbyte, float>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)Single.MinValue), converter(unchecked((sbyte)Single.MinValue)));
            Assert.Equal(unchecked((sbyte)Single.MaxValue), converter(unchecked((sbyte)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<sbyte, double>();

            // Base
            Assert.Equal(0, converter(0));
            Assert.Equal(1, converter(1));
            Assert.Equal(-1, converter(-1));
            // Min/Max
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)Double.MinValue), converter(unchecked((sbyte)Double.MinValue)));
            Assert.Equal(unchecked((sbyte)Double.MaxValue), converter(unchecked((sbyte)Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<sbyte, decimal>();

            // Base
            Assert.Equal(0m, converter(0));
            Assert.Equal(1m, converter(1));
            // Min/Max
            Assert.Equal(SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal(SByte.MaxValue, converter(SByte.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<sbyte, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0));
            Assert.Equal((IntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((IntPtr)SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal((IntPtr)SByte.MaxValue, converter(SByte.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<sbyte, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0));
            Assert.Equal((UIntPtr)1, converter(1));
            // Min/Max
            Assert.Equal((UIntPtr)SByte.MinValue, converter(SByte.MinValue));
            Assert.Equal((UIntPtr)SByte.MaxValue, converter(SByte.MaxValue));
        }
    }
}

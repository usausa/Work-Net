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
            Assert.Equal((byte)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((byte)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal((byte)unchecked((sbyte)Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<ulong, sbyte>();

            // Base
            Assert.Equal(0, converter(0ul));
            Assert.Equal(1, converter(1ul));
            // Min/Max
            Assert.Equal((sbyte)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((sbyte)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)unchecked((ulong)SByte.MinValue)), converter(unchecked((ulong)SByte.MinValue)));
            Assert.Equal((sbyte)(ulong)SByte.MaxValue, converter((ulong)SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<ulong, char>();

            // Base
            Assert.Equal(0, converter(0ul));
            Assert.Equal(1, converter(1ul));
            // Min/Max
            Assert.Equal((char)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((char)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)Char.MinValue, converter(Char.MinValue));
            Assert.Equal((ulong)Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<ulong, short>();

            // Base
            Assert.Equal(0L, converter(0ul));
            Assert.Equal(1L, converter(1ul));
            // Min/Max
            Assert.Equal((short)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((short)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)unchecked((ulong)Int16.MinValue)), converter(unchecked((ulong)Int16.MinValue)));
            Assert.Equal((short)unchecked((ulong)Int16.MaxValue), converter(unchecked((ulong)Int16.MaxValue)));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<ulong, ushort>();

            // Base
            Assert.Equal(0ul, converter(0ul));
            Assert.Equal(1ul, converter(1ul));
            // Min/Max
            Assert.Equal((ushort)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((ushort)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal((ulong)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((ulong)UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<ulong, int>();

            // Base
            Assert.Equal(0, converter(0ul));
            Assert.Equal(1, converter(1ul));
            // Min/Max
            Assert.Equal((int)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((int)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((int)unchecked((ulong)Int32.MinValue)), converter(unchecked((ulong)Int32.MinValue)));
            Assert.Equal(Int32.MaxValue, converter(Int32.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<ulong, uint>();

            // Base
            Assert.Equal(0u, converter(0ul));
            Assert.Equal(1u, converter(1ul));
            // Min/Max
            Assert.Equal((uint)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((uint)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal(UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<ulong, long>();

            // Base
            Assert.Equal(0L, converter(0ul));
            Assert.Equal(1L, converter(1ul));
            // Min/Max
            Assert.Equal((long)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(unchecked((long)UInt64.MaxValue), converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal(Int64.MinValue, converter(unchecked((ulong)Int64.MinValue)));
            Assert.Equal(Int64.MaxValue, converter(Int64.MaxValue));
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
            Assert.Equal(UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(UInt64.MaxValue, converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((uint)Single.MinValue), converter(unchecked((uint)Single.MinValue)));
            Assert.Equal(unchecked((uint)Single.MaxValue), converter(unchecked((uint)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<ulong, double>();

            // Base
            Assert.Equal(0d, converter(0ul));
            Assert.Equal(1d, converter(1ul));
            // Min/Max
            Assert.Equal(UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(UInt64.MaxValue, converter(UInt64.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((uint)Double.MinValue), converter(unchecked((uint) Double.MinValue)));
            Assert.Equal(unchecked((uint)Double.MaxValue), converter(unchecked((uint) Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<ulong, decimal>();

            // Base
            Assert.Equal(0m, converter(0ul));
            Assert.Equal(1m, converter(1ul));
            // Min/Max
            Assert.Equal(UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal(UInt64.MaxValue, converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<ulong, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0ul));
            Assert.Equal((IntPtr)1, converter(1ul));
            // Min/Max
            Assert.Equal((IntPtr)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal((IntPtr)UInt64.MaxValue, converter(UInt64.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<ulong, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0ul));
            Assert.Equal((UIntPtr)1, converter(1ul));
            // Min/Max
            Assert.Equal((UIntPtr)UInt64.MinValue, converter(UInt64.MinValue));
            Assert.Equal((UIntPtr)UInt64.MaxValue, converter(UInt64.MaxValue));
        }
    }
}

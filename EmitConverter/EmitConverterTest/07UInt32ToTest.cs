using System;

using Xunit;

namespace EmitConverterTest
{
    public class UInt32ToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<uint, byte>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal((byte)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(unchecked((byte)UInt32.MaxValue), converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal((byte)unchecked((sbyte)Byte.MinValue), converter(Byte.MinValue));
            Assert.Equal(Byte.MaxValue, converter(Byte.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<uint, sbyte>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal((sbyte)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(unchecked((sbyte)UInt32.MaxValue), converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((sbyte)unchecked((uint)SByte.MinValue)), converter(unchecked((uint)SByte.MinValue)));
            Assert.Equal((sbyte)(uint)SByte.MaxValue, converter((uint)SByte.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<uint, char>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal((char)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(unchecked((char)UInt32.MaxValue), converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal((uint)Char.MinValue, converter(Char.MinValue));
            Assert.Equal((uint)Char.MaxValue, converter(Char.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<uint, short>();

            // Base
            Assert.Equal(0L, converter(0u));
            Assert.Equal(1L, converter(1u));
            // Min/Max
            Assert.Equal((short)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(unchecked((short)UInt32.MaxValue), converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((short)unchecked((uint)Int16.MinValue)), converter(unchecked((uint)Int16.MinValue)));
            Assert.Equal((short)unchecked((uint)Int16.MaxValue), converter(unchecked((uint)Int16.MaxValue)));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<uint, ushort>();

            // Base
            Assert.Equal(0ul, converter(0u));
            Assert.Equal(1ul, converter(1u));
            // Min/Max
            Assert.Equal((ushort)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(unchecked((ushort)UInt32.MaxValue), converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal((uint)UInt16.MinValue, converter(UInt16.MinValue));
            Assert.Equal((uint)UInt16.MaxValue, converter(UInt16.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<uint, int>();

            // Base
            Assert.Equal(0, converter(0u));
            Assert.Equal(1, converter(1u));
            // Min/Max
            Assert.Equal((int)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(unchecked((int)UInt32.MaxValue), converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((int)unchecked((uint)Int32.MinValue)), converter(unchecked((uint)Int32.MinValue)));
            Assert.Equal(Int32.MaxValue, converter(Int32.MaxValue));
        }

        // Nop uint

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<uint, long>();

            // Base
            Assert.Equal(0L, converter(0u));
            Assert.Equal(1L, converter(1u));
            // Min/Max
            Assert.Equal(UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((uint)Int64.MinValue), converter(unchecked((uint)Int64.MinValue)));
            Assert.Equal(unchecked((uint)Int64.MaxValue), converter(unchecked((uint)Int64.MaxValue)));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<uint, uint>();

            // Base
            Assert.Equal(0ul, converter(0u));
            Assert.Equal(1ul, converter(1u));
            // Min/Max
            Assert.Equal(UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((uint)UInt64.MinValue), converter(unchecked((uint)UInt64.MinValue)));
            Assert.Equal(unchecked((uint)UInt64.MaxValue), converter(unchecked((uint)UInt64.MaxValue)));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<uint, float>();

            // Base
            Assert.Equal(0f, converter(0u));
            Assert.Equal(1f, converter(1u));
            // Min/Max
            Assert.Equal(UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((uint)Single.MinValue), converter(unchecked((uint)Single.MinValue)));
            Assert.Equal(unchecked((uint)Single.MaxValue), converter(unchecked((uint)Single.MaxValue)));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<uint, double>();

            // Base
            Assert.Equal(0d, converter(0u));
            Assert.Equal(1d, converter(1u));
            // Min/Max
            Assert.Equal(UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
            // Compare to cast
            Assert.Equal(unchecked((uint)Double.MinValue), converter(unchecked((uint) Double.MinValue)));
            Assert.Equal(unchecked((uint)Double.MaxValue), converter(unchecked((uint) Double.MaxValue)));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<uint, decimal>();

            // Base
            Assert.Equal(0m, converter(0u));
            Assert.Equal(1m, converter(1u));
            // Min/Max
            Assert.Equal(UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal(UInt32.MaxValue, converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<uint, IntPtr>();

            // Base
            Assert.Equal(IntPtr.Zero, converter(0u));
            Assert.Equal((IntPtr)1, converter(1u));
            // Min/Max
            Assert.Equal((IntPtr)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal((IntPtr)UInt32.MaxValue, converter(UInt32.MaxValue));
        }

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<uint, UIntPtr>();

            // Base
            Assert.Equal(UIntPtr.Zero, converter(0u));
            Assert.Equal((UIntPtr)1, converter(1u));
            // Min/Max
            Assert.Equal((UIntPtr)UInt32.MinValue, converter(UInt32.MinValue));
            Assert.Equal((UIntPtr)UInt32.MaxValue, converter(UInt32.MaxValue));
        }
    }
}

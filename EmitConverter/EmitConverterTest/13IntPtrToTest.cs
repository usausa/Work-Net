namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class IntPtrToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<IntPtr, byte>();

            Assert.Equal(ManualConverter.IntPtrToByte(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToByte((IntPtr)1), converter((IntPtr)1));
            //Assert.Equal(ManualConverter.IntPtrToByte(IntPtr.MinValue), converter(IntPtr.MinValue));
            //Assert.Equal(ManualConverter.IntPtrToByte(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<IntPtr, sbyte>();

            Assert.Equal(ManualConverter.IntPtrToSByte(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToSByte((IntPtr)1), converter((IntPtr)1));
            //Assert.Equal(ManualConverter.IntPtrToSByte(IntPtr.MinValue), converter(IntPtr.MinValue));
            //Assert.Equal(ManualConverter.IntPtrToSByte(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<IntPtr, char>();

            Assert.Equal(ManualConverter.IntPtrToChar(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToChar((IntPtr)1), converter((IntPtr)1));
            //Assert.Equal(ManualConverter.IntPtrToChar(IntPtr.MinValue), converter(IntPtr.MinValue));
            //Assert.Equal(ManualConverter.IntPtrToChar(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<IntPtr, short>();

            Assert.Equal(ManualConverter.IntPtrToInt16(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToInt16((IntPtr)1), converter((IntPtr)1));
            //Assert.Equal(ManualConverter.IntPtrToInt16(IntPtr.MinValue), converter(IntPtr.MinValue));
            //Assert.Equal(ManualConverter.IntPtrToInt16(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<IntPtr, ushort>();

            Assert.Equal(ManualConverter.IntPtrToUInt16(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToUInt16((IntPtr)1), converter((IntPtr)1));
            //Assert.Equal(ManualConverter.IntPtrToUInt16(IntPtr.MinValue), converter(IntPtr.MinValue));
            //Assert.Equal(ManualConverter.IntPtrToUInt16(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<IntPtr, int>();

            Assert.Equal(ManualConverter.IntPtrToInt32(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToInt32((IntPtr)1), converter((IntPtr)1));
            //Assert.Equal(ManualConverter.IntPtrToInt32(IntPtr.MinValue), converter(IntPtr.MinValue));
            //Assert.Equal(ManualConverter.IntPtrToInt32(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<IntPtr, uint>();

            Assert.Equal(ManualConverter.IntPtrToUInt32(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToUInt32((IntPtr)1), converter((IntPtr)1));
            //Assert.Equal(ManualConverter.IntPtrToUInt32(IntPtr.MinValue), converter(IntPtr.MinValue));
            //Assert.Equal(ManualConverter.IntPtrToUInt32(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<IntPtr, long>();

            Assert.Equal(ManualConverter.IntPtrToInt64(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToInt64((IntPtr)1), converter((IntPtr)1));
            Assert.Equal(ManualConverter.IntPtrToInt64(IntPtr.MinValue), converter(IntPtr.MinValue));
            Assert.Equal(ManualConverter.IntPtrToInt64(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<IntPtr, ulong>();

            Assert.Equal(ManualConverter.IntPtrToUInt64(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToUInt64((IntPtr)1), converter((IntPtr)1));
            Assert.Equal(ManualConverter.IntPtrToUInt64(IntPtr.MinValue), converter(IntPtr.MinValue));
            Assert.Equal(ManualConverter.IntPtrToUInt64(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<IntPtr, float>();

            Assert.Equal(ManualConverter.IntPtrToSingle(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToSingle((IntPtr)1), converter((IntPtr)1));
            Assert.Equal(ManualConverter.IntPtrToSingle(IntPtr.MinValue), converter(IntPtr.MinValue));
            Assert.Equal(ManualConverter.IntPtrToSingle(IntPtr.MinValue), converter(IntPtr.MinValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<IntPtr, double>();

            Assert.Equal(ManualConverter.IntPtrToDouble(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToDouble((IntPtr)1), converter((IntPtr)1));
            Assert.Equal(ManualConverter.IntPtrToDouble(IntPtr.MinValue), converter(IntPtr.MinValue));
            Assert.Equal(ManualConverter.IntPtrToDouble(IntPtr.MinValue), converter(IntPtr.MinValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<IntPtr, decimal>();

            Assert.Equal(ManualConverter.IntPtrToDecimal(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToDecimal((IntPtr)1), converter((IntPtr)1));
            Assert.Equal(ManualConverter.IntPtrToDecimal(IntPtr.MinValue), converter(IntPtr.MinValue));
            Assert.Equal(ManualConverter.IntPtrToDecimal(IntPtr.MinValue), converter(IntPtr.MinValue));
        }

        // Nop IntPtr

        [Fact]
        public void ToUIntPtr()
        {
            var converter = ConverterFactory.Create<IntPtr, UIntPtr>();

            Assert.Equal(ManualConverter.IntPtrToUIntPtr(IntPtr.Zero), converter(IntPtr.Zero));
            Assert.Equal(ManualConverter.IntPtrToUIntPtr((IntPtr)1), converter((IntPtr)1));
            Assert.Equal(ManualConverter.IntPtrToUIntPtr(IntPtr.MinValue), converter(IntPtr.MinValue));
            Assert.Equal(ManualConverter.IntPtrToUIntPtr(IntPtr.MaxValue), converter(IntPtr.MaxValue));
        }
    }
}

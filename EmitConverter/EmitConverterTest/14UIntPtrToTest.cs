namespace EmitConverterTest
{
    using System;

    using Xunit;

    public class UIntPtrToTest
    {
        [Fact]
        public void ToByte()
        {
            var converter = ConverterFactory.Create<UIntPtr, byte>();

            Assert.Equal(ManualConverter.UIntPtrToByte(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToByte((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToByte(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            //Assert.Equal(ManualConverter.UIntPtrToByte(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToSByte()
        {
            var converter = ConverterFactory.Create<UIntPtr, sbyte>();

            Assert.Equal(ManualConverter.UIntPtrToSByte(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToSByte((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToSByte(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            //Assert.Equal(ManualConverter.UIntPtrToSByte(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToChar()
        {
            var converter = ConverterFactory.Create<UIntPtr, char>();

            Assert.Equal(ManualConverter.UIntPtrToChar(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToChar((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToChar(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            //Assert.Equal(ManualConverter.UIntPtrToChar(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToInt16()
        {
            var converter = ConverterFactory.Create<UIntPtr, short>();

            Assert.Equal(ManualConverter.UIntPtrToInt16(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToInt16((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToInt16(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            //Assert.Equal(ManualConverter.UIntPtrToInt16(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToUInt16()
        {
            var converter = ConverterFactory.Create<UIntPtr, ushort>();

            Assert.Equal(ManualConverter.UIntPtrToUInt16(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToUInt16((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToUInt16(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            //Assert.Equal(ManualConverter.UIntPtrToUInt16(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToInt32()
        {
            var converter = ConverterFactory.Create<UIntPtr, int>();

            Assert.Equal(ManualConverter.UIntPtrToInt32(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToInt32((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToInt32(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            //Assert.Equal(ManualConverter.UIntPtrToInt32(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToUInt32()
        {
            var converter = ConverterFactory.Create<UIntPtr, uint>();

            Assert.Equal(ManualConverter.UIntPtrToUInt32(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToUInt32((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToUInt32(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            //Assert.Equal(ManualConverter.UIntPtrToUInt32(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToInt64()
        {
            var converter = ConverterFactory.Create<UIntPtr, long>();

            Assert.Equal(ManualConverter.UIntPtrToInt64(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToInt64((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToInt64(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            Assert.Equal(ManualConverter.UIntPtrToInt64(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToUInt64()
        {
            var converter = ConverterFactory.Create<UIntPtr, ulong>();

            Assert.Equal(ManualConverter.UIntPtrToUInt64(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToUInt64((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToUInt64(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            Assert.Equal(ManualConverter.UIntPtrToUInt64(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        [Fact]
        public void ToSingle()
        {
            var converter = ConverterFactory.Create<UIntPtr, float>();

            Assert.Equal(ManualConverter.UIntPtrToSingle(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToSingle((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToSingle(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            Assert.Equal(ManualConverter.UIntPtrToSingle(UIntPtr.MinValue), converter(UIntPtr.MinValue));
        }

        [Fact]
        public void ToDouble()
        {
            var converter = ConverterFactory.Create<UIntPtr, double>();

            Assert.Equal(ManualConverter.UIntPtrToDouble(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToDouble((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToDouble(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            Assert.Equal(ManualConverter.UIntPtrToDouble(UIntPtr.MinValue), converter(UIntPtr.MinValue));
        }

        [Fact]
        public void ToDecimal()
        {
            var converter = ConverterFactory.Create<UIntPtr, decimal>();

            Assert.Equal(ManualConverter.UIntPtrToDecimal(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToDecimal((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToDecimal(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            Assert.Equal(ManualConverter.UIntPtrToDecimal(UIntPtr.MinValue), converter(UIntPtr.MinValue));
        }

        [Fact]
        public void ToIntPtr()
        {
            var converter = ConverterFactory.Create<UIntPtr, IntPtr>();

            Assert.Equal(ManualConverter.UIntPtrToIntPtr(UIntPtr.Zero), converter(UIntPtr.Zero));
            Assert.Equal(ManualConverter.UIntPtrToIntPtr((UIntPtr)1), converter((UIntPtr)1));
            Assert.Equal(ManualConverter.UIntPtrToIntPtr(UIntPtr.MinValue), converter(UIntPtr.MinValue));
            Assert.Equal(ManualConverter.UIntPtrToIntPtr(UIntPtr.MaxValue), converter(UIntPtr.MaxValue));
        }

        // Nop UIntPtr
    }
}

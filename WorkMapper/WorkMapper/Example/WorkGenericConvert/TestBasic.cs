// ReSharper disable CompareOfFloatsByEqualityOperator
namespace WorkGenericConvert
{
    using System;
    using System.Diagnostics;

    public static class TestBasic
    {
        public static void Test()
        {
            ByteToSByte();
            ByteToChar();
            ByteToInt16();
            ByteToUInt16();
            ByteToInt32();
            ByteToUInt32();
            ByteToInt64();
            ByteToUInt64();
            ByteToSingle();
            ByteToDouble();
            ByteToDecimal();
            ByteToIntPtr();
            ByteToUIntPtr();
            SByteToByte();
            SByteToChar();
            SByteToInt16();
            SByteToUInt16();
            SByteToInt32();
            SByteToUInt32();
            SByteToInt64();
            SByteToUInt64();
            SByteToSingle();
            SByteToDouble();
            SByteToDecimal();
            SByteToIntPtr();
            SByteToUIntPtr();
            CharToByte();
            CharToSByte();
            CharToInt16();
            CharToUInt16();
            CharToInt32();
            CharToUInt32();
            CharToInt64();
            CharToUInt64();
            CharToSingle();
            CharToDouble();
            CharToDecimal();
            CharToIntPtr();
            CharToUIntPtr();
            Int16ToByte();
            Int16ToSByte();
            Int16ToChar();
            Int16ToUInt16();
            Int16ToInt32();
            Int16ToUInt32();
            Int16ToInt64();
            Int16ToUInt64();
            Int16ToSingle();
            Int16ToDouble();
            Int16ToDecimal();
            Int16ToIntPtr();
            Int16ToUIntPtr();
            UInt16ToByte();
            UInt16ToSByte();
            UInt16ToChar();
            UInt16ToInt16();
            UInt16ToInt32();
            UInt16ToUInt32();
            UInt16ToInt64();
            UInt16ToUInt64();
            UInt16ToSingle();
            UInt16ToDouble();
            UInt16ToDecimal();
            UInt16ToIntPtr();
            UInt16ToUIntPtr();
            Int32ToByte();
            Int32ToSByte();
            Int32ToChar();
            Int32ToInt16();
            Int32ToUInt16();
            Int32ToUInt32();
            Int32ToInt64();
            Int32ToUInt64();
            Int32ToSingle();
            Int32ToDouble();
            Int32ToDecimal();
            Int32ToIntPtr();
            Int32ToUIntPtr();
            UInt32ToByte();
            UInt32ToSByte();
            UInt32ToChar();
            UInt32ToInt16();
            UInt32ToUInt16();
            UInt32ToInt32();
            UInt32ToInt64();
            UInt32ToUInt64();
            UInt32ToSingle();
            UInt32ToDouble();
            UInt32ToDecimal();
            UInt32ToIntPtr();
            UInt32ToUIntPtr();
            Int64ToByte();
            Int64ToSByte();
            Int64ToChar();
            Int64ToInt16();
            Int64ToUInt16();
            Int64ToInt32();
            Int64ToUInt32();
            Int64ToUInt64();
            Int64ToSingle();
            Int64ToDouble();
            Int64ToDecimal();
            Int64ToIntPtr();
            Int64ToUIntPtr();
            UInt64ToByte();
            UInt64ToSByte();
            UInt64ToChar();
            UInt64ToInt16();
            UInt64ToUInt16();
            UInt64ToInt32();
            UInt64ToUInt32();
            UInt64ToInt64();
            UInt64ToSingle();
            UInt64ToDouble();
            UInt64ToDecimal();
            UInt64ToIntPtr();
            UInt64ToUIntPtr();
            SingleToByte();
            SingleToSByte();
            SingleToChar();
            SingleToInt16();
            SingleToUInt16();
            SingleToInt32();
            SingleToUInt32();
            SingleToInt64();
            SingleToUInt64();
            SingleToDouble();
            SingleToDecimal();
            SingleToIntPtr();
            SingleToUIntPtr();
            DoubleToByte();
            DoubleToSByte();
            DoubleToChar();
            DoubleToInt16();
            DoubleToUInt16();
            DoubleToInt32();
            DoubleToUInt32();
            DoubleToInt64();
            DoubleToUInt64();
            DoubleToSingle();
            DoubleToDecimal();
            DoubleToIntPtr();
            DoubleToUIntPtr();
            DecimalToByte();
            DecimalToSByte();
            DecimalToChar();
            DecimalToInt16();
            DecimalToUInt16();
            DecimalToInt32();
            DecimalToUInt32();
            DecimalToInt64();
            DecimalToUInt64();
            DecimalToSingle();
            DecimalToDouble();
            DecimalToIntPtr();
            DecimalToUIntPtr();
            IntPtrToByte();
            IntPtrToSByte();
            IntPtrToChar();
            IntPtrToInt16();
            IntPtrToUInt16();
            IntPtrToInt32();
            IntPtrToUInt32();
            IntPtrToInt64();
            IntPtrToUInt64();
            IntPtrToSingle();
            IntPtrToDouble();
            IntPtrToDecimal();
            IntPtrToUIntPtr();
            UIntPtrToByte();
            UIntPtrToSByte();
            UIntPtrToChar();
            UIntPtrToInt16();
            UIntPtrToUInt16();
            UIntPtrToInt32();
            UIntPtrToUInt32();
            UIntPtrToInt64();
            UIntPtrToUInt64();
            UIntPtrToSingle();
            UIntPtrToDouble();
            UIntPtrToDecimal();
            UIntPtrToIntPtr();
        }

        public static void ByteToSByte()
        {
            var f = Factory.Create<byte, sbyte>();

            Debug.Assert(ManualConverter.ByteToSByte(0) == f(0));
            Debug.Assert(ManualConverter.ByteToSByte(1) == f(1));
            Debug.Assert(ManualConverter.ByteToSByte(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToSByte(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToChar()
        {
            var f = Factory.Create<byte, char>();

            Debug.Assert(ManualConverter.ByteToChar(0) == f(0));
            Debug.Assert(ManualConverter.ByteToChar(1) == f(1));
            Debug.Assert(ManualConverter.ByteToChar(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToChar(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToInt16()
        {
            var f = Factory.Create<byte, short>();

            Debug.Assert(ManualConverter.ByteToInt16(0) == f(0));
            Debug.Assert(ManualConverter.ByteToInt16(1) == f(1));
            Debug.Assert(ManualConverter.ByteToInt16(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToInt16(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToUInt16()
        {
            var f = Factory.Create<byte, ushort>();

            Debug.Assert(ManualConverter.ByteToUInt16(0) == f(0));
            Debug.Assert(ManualConverter.ByteToUInt16(1) == f(1));
            Debug.Assert(ManualConverter.ByteToUInt16(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToUInt16(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToInt32()
        {
            var f = Factory.Create<byte, int>();

            Debug.Assert(ManualConverter.ByteToInt32(0) == f(0));
            Debug.Assert(ManualConverter.ByteToInt32(1) == f(1));
            Debug.Assert(ManualConverter.ByteToInt32(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToInt32(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToUInt32()
        {
            var f = Factory.Create<byte, uint>();

            Debug.Assert(ManualConverter.ByteToUInt32(0) == f(0));
            Debug.Assert(ManualConverter.ByteToUInt32(1) == f(1));
            Debug.Assert(ManualConverter.ByteToUInt32(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToUInt32(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToInt64()
        {
            var f = Factory.Create<byte, long>();

            Debug.Assert(ManualConverter.ByteToInt64(0) == f(0));
            Debug.Assert(ManualConverter.ByteToInt64(1) == f(1));
            Debug.Assert(ManualConverter.ByteToInt64(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToInt64(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToUInt64()
        {
            var f = Factory.Create<byte, ulong>();

            Debug.Assert(ManualConverter.ByteToUInt64(0) == f(0));
            Debug.Assert(ManualConverter.ByteToUInt64(1) == f(1));
            Debug.Assert(ManualConverter.ByteToUInt64(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToUInt64(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToSingle()
        {
            var f = Factory.Create<byte, float>();

            Debug.Assert(ManualConverter.ByteToSingle(0) == f(0));
            Debug.Assert(ManualConverter.ByteToSingle(1) == f(1));
            Debug.Assert(ManualConverter.ByteToSingle(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToSingle(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToDouble()
        {
            var f = Factory.Create<byte, double>();

            Debug.Assert(ManualConverter.ByteToDouble(0) == f(0));
            Debug.Assert(ManualConverter.ByteToDouble(1) == f(1));
            Debug.Assert(ManualConverter.ByteToDouble(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToDouble(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToDecimal()
        {
            var f = Factory.Create<byte, decimal>();

            Debug.Assert(ManualConverter.ByteToDecimal(0) == f(0));
            Debug.Assert(ManualConverter.ByteToDecimal(1) == f(1));
            Debug.Assert(ManualConverter.ByteToDecimal(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToDecimal(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToIntPtr()
        {
            var f = Factory.Create<byte, IntPtr>();

            Debug.Assert(ManualConverter.ByteToIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.ByteToIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.ByteToIntPtr(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToIntPtr(Byte.MaxValue) == f(Byte.MaxValue));
        }


        public static void ByteToUIntPtr()
        {
            var f = Factory.Create<byte, UIntPtr>();

            Debug.Assert(ManualConverter.ByteToUIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.ByteToUIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.ByteToUIntPtr(Byte.MinValue) == f(Byte.MinValue));
            Debug.Assert(ManualConverter.ByteToUIntPtr(Byte.MaxValue) == f(Byte.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // SByteTo
        //--------------------------------------------------------------------------------


        public static void SByteToByte()
        {
            var f = Factory.Create<sbyte, byte>();

            Debug.Assert(ManualConverter.SByteToByte(0) == f(0));
            Debug.Assert(ManualConverter.SByteToByte(1) == f(1));
            Debug.Assert(ManualConverter.SByteToByte(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToByte(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToByte(SByte.MaxValue) == f(SByte.MaxValue));
        }

        // Nop sbyte


        public static void SByteToChar()
        {
            var f = Factory.Create<sbyte, char>();

            Debug.Assert(ManualConverter.SByteToChar(0) == f(0));
            Debug.Assert(ManualConverter.SByteToChar(1) == f(1));
            Debug.Assert(ManualConverter.SByteToChar(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToChar(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToChar(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToInt16()
        {
            var f = Factory.Create<sbyte, short>();

            Debug.Assert(ManualConverter.SByteToInt16(0) == f(0));
            Debug.Assert(ManualConverter.SByteToInt16(1) == f(1));
            Debug.Assert(ManualConverter.SByteToInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToInt16(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToInt16(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToUInt16()
        {
            var f = Factory.Create<sbyte, ushort>();

            Debug.Assert(ManualConverter.SByteToUInt16(0) == f(0));
            Debug.Assert(ManualConverter.SByteToUInt16(1) == f(1));
            Debug.Assert(ManualConverter.SByteToUInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToUInt16(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToUInt16(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToInt32()
        {
            var f = Factory.Create<sbyte, int>();

            Debug.Assert(ManualConverter.SByteToInt32(0) == f(0));
            Debug.Assert(ManualConverter.SByteToInt32(1) == f(1));
            Debug.Assert(ManualConverter.SByteToInt32(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToInt32(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToInt32(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToUInt32()
        {
            var f = Factory.Create<sbyte, uint>();

            Debug.Assert(ManualConverter.SByteToUInt32(0) == f(0));
            Debug.Assert(ManualConverter.SByteToUInt32(1) == f(1));
            Debug.Assert(ManualConverter.SByteToUInt32(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToUInt32(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToUInt32(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToInt64()
        {
            var f = Factory.Create<sbyte, long>();

            Debug.Assert(ManualConverter.SByteToInt64(0) == f(0));
            Debug.Assert(ManualConverter.SByteToInt64(1) == f(1));
            Debug.Assert(ManualConverter.SByteToInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToInt64(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToInt64(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToUInt64()
        {
            var f = Factory.Create<sbyte, ulong>();

            Debug.Assert(ManualConverter.SByteToUInt64(0) == f(0));
            Debug.Assert(ManualConverter.SByteToUInt64(1) == f(1));
            Debug.Assert(ManualConverter.SByteToUInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToUInt64(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToUInt64(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToSingle()
        {
            var f = Factory.Create<sbyte, float>();

            Debug.Assert(ManualConverter.SByteToSingle(0) == f(0));
            Debug.Assert(ManualConverter.SByteToSingle(1) == f(1));
            Debug.Assert(ManualConverter.SByteToSingle(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToSingle(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToSingle(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToDouble()
        {
            var f = Factory.Create<sbyte, double>();

            Debug.Assert(ManualConverter.SByteToDouble(0) == f(0));
            Debug.Assert(ManualConverter.SByteToDouble(1) == f(1));
            Debug.Assert(ManualConverter.SByteToDouble(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToDouble(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToDouble(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToDecimal()
        {
            var f = Factory.Create<sbyte, decimal>();

            Debug.Assert(ManualConverter.SByteToDecimal(0) == f(0));
            Debug.Assert(ManualConverter.SByteToDecimal(1) == f(1));
            Debug.Assert(ManualConverter.SByteToDecimal(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToDecimal(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToDecimal(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToIntPtr()
        {
            var f = Factory.Create<sbyte, IntPtr>();

            Debug.Assert(ManualConverter.SByteToIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.SByteToIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.SByteToIntPtr(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToIntPtr(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToIntPtr(SByte.MaxValue) == f(SByte.MaxValue));
        }


        public static void SByteToUIntPtr()
        {
            var f = Factory.Create<sbyte, UIntPtr>();

            Debug.Assert(ManualConverter.SByteToUIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.SByteToUIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.SByteToUIntPtr(-1) == f(-1));
            Debug.Assert(ManualConverter.SByteToUIntPtr(SByte.MinValue) == f(SByte.MinValue));
            Debug.Assert(ManualConverter.SByteToUIntPtr(SByte.MaxValue) == f(SByte.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // CharTo
        //--------------------------------------------------------------------------------


        public static void CharToByte()
        {
            var f = Factory.Create<char, byte>();

            Debug.Assert(ManualConverter.CharToByte((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToByte((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToByte(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToByte(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToSByte()
        {
            var f = Factory.Create<char, sbyte>();

            Debug.Assert(ManualConverter.CharToSByte((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToSByte((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToSByte(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToSByte(Char.MaxValue) == f(Char.MaxValue));
        }

        // Nop char


        public static void CharToInt16()
        {
            var f = Factory.Create<char, short>();

            Debug.Assert(ManualConverter.CharToInt16((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToInt16((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToInt16(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToInt16(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToUInt16()
        {
            var f = Factory.Create<char, ushort>();

            Debug.Assert(ManualConverter.CharToUInt16((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToUInt16((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToUInt16(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToUInt16(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToInt32()
        {
            var f = Factory.Create<char, int>();

            Debug.Assert(ManualConverter.CharToInt32((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToInt32((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToInt32(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToInt32(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToUInt32()
        {
            var f = Factory.Create<char, uint>();

            Debug.Assert(ManualConverter.CharToUInt32((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToUInt32((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToUInt32(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToUInt32(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToInt64()
        {
            var f = Factory.Create<char, long>();

            Debug.Assert(ManualConverter.CharToInt64((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToInt64((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToInt64(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToInt64(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToUInt64()
        {
            var f = Factory.Create<char, ulong>();

            Debug.Assert(ManualConverter.CharToUInt64((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToUInt64((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToUInt64(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToUInt64(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToSingle()
        {
            var f = Factory.Create<char, float>();

            Debug.Assert(ManualConverter.CharToSingle((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToSingle((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToSingle(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToSingle(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToDouble()
        {
            var f = Factory.Create<char, double>();

            Debug.Assert(ManualConverter.CharToDouble((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToDouble((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToDouble(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToDouble(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToDecimal()
        {
            var f = Factory.Create<char, decimal>();

            Debug.Assert(ManualConverter.CharToDecimal((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToDecimal((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToDecimal(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToDecimal(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToIntPtr()
        {
            var f = Factory.Create<char, IntPtr>();

            Debug.Assert(ManualConverter.CharToIntPtr((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToIntPtr((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToIntPtr(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToIntPtr(Char.MaxValue) == f(Char.MaxValue));
        }


        public static void CharToUIntPtr()
        {
            var f = Factory.Create<char, UIntPtr>();

            Debug.Assert(ManualConverter.CharToUIntPtr((char)0) == f((char)0));
            Debug.Assert(ManualConverter.CharToUIntPtr((char)1) == f((char)1));
            Debug.Assert(ManualConverter.CharToUIntPtr(Char.MinValue) == f(Char.MinValue));
            Debug.Assert(ManualConverter.CharToUIntPtr(Char.MaxValue) == f(Char.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // Int16To
        //--------------------------------------------------------------------------------


        public static void Int16ToByte()
        {
            var f = Factory.Create<short, byte>();

            Debug.Assert(ManualConverter.Int16ToByte(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToByte(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToByte(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToByte(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToByte(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToSByte()
        {
            var f = Factory.Create<short, sbyte>();

            Debug.Assert(ManualConverter.Int16ToSByte(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToSByte(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToSByte(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToSByte(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToSByte(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToChar()
        {
            var f = Factory.Create<short, char>();

            Debug.Assert(ManualConverter.Int16ToChar(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToChar(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToChar(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToChar(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToChar(Int16.MaxValue) == f(Int16.MaxValue));
        }

        // Nop short


        public static void Int16ToUInt16()
        {
            var f = Factory.Create<short, ushort>();

            Debug.Assert(ManualConverter.Int16ToUInt16(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToUInt16(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToUInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToUInt16(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToUInt16(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToInt32()
        {
            var f = Factory.Create<short, int>();

            Debug.Assert(ManualConverter.Int16ToInt32(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToInt32(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToInt32(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToInt32(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToInt32(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToUInt32()
        {
            var f = Factory.Create<short, uint>();

            Debug.Assert(ManualConverter.Int16ToUInt32(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToUInt32(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToUInt32(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToUInt32(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToUInt32(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToInt64()
        {
            var f = Factory.Create<short, long>();

            Debug.Assert(ManualConverter.Int16ToInt64(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToInt64(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToInt64(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToInt64(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToUInt64()
        {
            var f = Factory.Create<short, ulong>();

            Debug.Assert(ManualConverter.Int16ToUInt64(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToUInt64(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToUInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToUInt64(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToUInt64(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToSingle()
        {
            var f = Factory.Create<short, float>();

            Debug.Assert(ManualConverter.Int16ToSingle(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToSingle(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToSingle(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToSingle(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToSingle(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToDouble()
        {
            var f = Factory.Create<short, double>();

            Debug.Assert(ManualConverter.Int16ToDouble(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToDouble(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToDouble(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToDouble(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToDouble(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToDecimal()
        {
            var f = Factory.Create<short, decimal>();

            Debug.Assert(ManualConverter.Int16ToDecimal(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToDecimal(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToDecimal(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToDecimal(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToDecimal(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToIntPtr()
        {
            var f = Factory.Create<short, IntPtr>();

            Debug.Assert(ManualConverter.Int16ToIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToIntPtr(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToIntPtr(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToIntPtr(Int16.MaxValue) == f(Int16.MaxValue));
        }


        public static void Int16ToUIntPtr()
        {
            var f = Factory.Create<short, UIntPtr>();

            Debug.Assert(ManualConverter.Int16ToUIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.Int16ToUIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.Int16ToUIntPtr(-1) == f(-1));
            Debug.Assert(ManualConverter.Int16ToUIntPtr(Int16.MinValue) == f(Int16.MinValue));
            Debug.Assert(ManualConverter.Int16ToUIntPtr(Int16.MaxValue) == f(Int16.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // UInt16To
        //--------------------------------------------------------------------------------


        public static void UInt16ToByte()
        {
            var f = Factory.Create<ushort, byte>();

            Debug.Assert(ManualConverter.UInt16ToByte(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToByte(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToByte(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToByte(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToSByte()
        {
            var f = Factory.Create<ushort, sbyte>();

            Debug.Assert(ManualConverter.UInt16ToSByte(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToSByte(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToSByte(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToSByte(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToChar()
        {
            var f = Factory.Create<ushort, char>();

            Debug.Assert(ManualConverter.UInt16ToChar(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToChar(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToChar(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToChar(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToInt16()
        {
            var f = Factory.Create<ushort, short>();

            Debug.Assert(ManualConverter.UInt16ToInt16(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToInt16(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToInt16(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToInt16(UInt16.MaxValue) == f(UInt16.MaxValue));
        }

        // Nop ushort


        public static void UInt16ToInt32()
        {
            var f = Factory.Create<ushort, int>();

            Debug.Assert(ManualConverter.UInt16ToInt32(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToInt32(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToInt32(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToInt32(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToUInt32()
        {
            var f = Factory.Create<ushort, uint>();

            Debug.Assert(ManualConverter.UInt16ToUInt32(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToUInt32(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToUInt32(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToUInt32(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToInt64()
        {
            var f = Factory.Create<ushort, long>();

            Debug.Assert(ManualConverter.UInt16ToInt64(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToInt64(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToInt64(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToInt64(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToUInt64()
        {
            var f = Factory.Create<ushort, ulong>();

            Debug.Assert(ManualConverter.UInt16ToUInt64(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToUInt64(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToUInt64(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToUInt64(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToSingle()
        {
            var f = Factory.Create<ushort, float>();

            Debug.Assert(ManualConverter.UInt16ToSingle(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToSingle(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToSingle(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToSingle(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToDouble()
        {
            var f = Factory.Create<ushort, double>();

            Debug.Assert(ManualConverter.UInt16ToDouble(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToDouble(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToDouble(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToDouble(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToDecimal()
        {
            var f = Factory.Create<ushort, decimal>();

            Debug.Assert(ManualConverter.UInt16ToDecimal(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToDecimal(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToDecimal(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToDecimal(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToIntPtr()
        {
            var f = Factory.Create<ushort, IntPtr>();

            Debug.Assert(ManualConverter.UInt16ToIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToIntPtr(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToIntPtr(UInt16.MaxValue) == f(UInt16.MaxValue));
        }


        public static void UInt16ToUIntPtr()
        {
            var f = Factory.Create<ushort, UIntPtr>();

            Debug.Assert(ManualConverter.UInt16ToUIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.UInt16ToUIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.UInt16ToUIntPtr(UInt16.MinValue) == f(UInt16.MinValue));
            Debug.Assert(ManualConverter.UInt16ToUIntPtr(UInt16.MaxValue) == f(UInt16.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // Int32To
        //--------------------------------------------------------------------------------


        public static void Int32ToByte()
        {
            var f = Factory.Create<int, byte>();

            Debug.Assert(ManualConverter.Int32ToByte(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToByte(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToByte(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToByte(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToByte(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToSByte()
        {
            var f = Factory.Create<int, sbyte>();

            Debug.Assert(ManualConverter.Int32ToSByte(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToSByte(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToSByte(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToSByte(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToSByte(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToChar()
        {
            var f = Factory.Create<int, char>();

            Debug.Assert(ManualConverter.Int32ToChar(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToChar(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToChar(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToChar(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToChar(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToInt16()
        {
            var f = Factory.Create<int, short>();

            // Base
            Debug.Assert(0 == f(0));
            Debug.Assert(1 == f(1));
            Debug.Assert(-1 == f(-1));
            // Min/Max
            Debug.Assert(ManualConverter.Int32ToInt16(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt16(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToUInt16()
        {
            var f = Factory.Create<int, ushort>();

            Debug.Assert(ManualConverter.Int32ToUInt16(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToUInt16(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToUInt16(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToUInt16(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToUInt16(Int32.MaxValue) == f(Int32.MaxValue));
        }

        // Nop int


        public static void Int32ToUInt32()
        {
            var f = Factory.Create<int, uint>();

            Debug.Assert(ManualConverter.Int32ToUInt32(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToUInt32(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToUInt32(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToUInt32(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToUInt32(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToInt64()
        {
            var f = Factory.Create<int, long>();

            Debug.Assert(ManualConverter.Int32ToInt64(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToInt64(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToUInt64()
        {
            var f = Factory.Create<int, ulong>();

            Debug.Assert(ManualConverter.Int32ToUInt64(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToUInt64(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToUInt64(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToUInt64(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToUInt64(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToSingle()
        {
            var f = Factory.Create<int, float>();

            Debug.Assert(ManualConverter.Int32ToSingle(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToSingle(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToSingle(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToSingle(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToSingle(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToDouble()
        {
            var f = Factory.Create<int, double>();

            Debug.Assert(ManualConverter.Int32ToDouble(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToDouble(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToDouble(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToDouble(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToDouble(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToDecimal()
        {
            var f = Factory.Create<int, decimal>();

            Debug.Assert(ManualConverter.Int32ToDecimal(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToDecimal(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToDecimal(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToDecimal(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToDecimal(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToIntPtr()
        {
            var f = Factory.Create<int, IntPtr>();

            Debug.Assert(ManualConverter.Int32ToIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToIntPtr(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToIntPtr(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToIntPtr(Int32.MaxValue) == f(Int32.MaxValue));
        }


        public static void Int32ToUIntPtr()
        {
            var f = Factory.Create<int, UIntPtr>();

            Debug.Assert(ManualConverter.Int32ToUIntPtr(0) == f(0));
            Debug.Assert(ManualConverter.Int32ToUIntPtr(1) == f(1));
            Debug.Assert(ManualConverter.Int32ToUIntPtr(-1) == f(-1));
            Debug.Assert(ManualConverter.Int32ToUIntPtr(Int32.MinValue) == f(Int32.MinValue));
            Debug.Assert(ManualConverter.Int32ToUIntPtr(Int32.MaxValue) == f(Int32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // UInt32To
        //--------------------------------------------------------------------------------


        public static void UInt32ToByte()
        {
            var f = Factory.Create<uint, byte>();

            Debug.Assert(ManualConverter.UInt32ToByte(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToByte(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToByte(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToByte(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToSByte()
        {
            var f = Factory.Create<uint, sbyte>();

            Debug.Assert(ManualConverter.UInt32ToSByte(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToSByte(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToSByte(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToSByte(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToChar()
        {
            var f = Factory.Create<uint, char>();

            Debug.Assert(ManualConverter.UInt32ToChar(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToChar(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToChar(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToChar(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToInt16()
        {
            var f = Factory.Create<uint, short>();

            Debug.Assert(ManualConverter.UInt32ToInt16(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToInt16(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToInt16(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToInt16(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToUInt16()
        {
            var f = Factory.Create<uint, ushort>();

            Debug.Assert(ManualConverter.UInt32ToUInt16(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToUInt16(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToUInt16(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToUInt16(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToInt32()
        {
            var f = Factory.Create<uint, int>();

            Debug.Assert(ManualConverter.UInt32ToInt32(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToInt32(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToInt32(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToInt32(UInt32.MaxValue) == f(UInt32.MaxValue));
        }

        // Nop uint


        public static void UInt32ToInt64()
        {
            var f = Factory.Create<uint, long>();

            Debug.Assert(ManualConverter.UInt32ToInt64(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToInt64(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToInt64(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToInt64(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToUInt64()
        {
            var f = Factory.Create<uint, uint>();

            Debug.Assert(ManualConverter.UInt32ToUInt64(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToUInt64(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToUInt64(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToUInt64(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToSingle()
        {
            var f = Factory.Create<uint, float>();

            Debug.Assert(ManualConverter.UInt32ToSingle(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToSingle(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToSingle(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToSingle(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToDouble()
        {
            var f = Factory.Create<uint, double>();

            Debug.Assert(ManualConverter.UInt32ToDouble(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToDouble(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToDouble(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToDouble(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToDecimal()
        {
            var f = Factory.Create<uint, decimal>();

            Debug.Assert(ManualConverter.UInt32ToDecimal(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToDecimal(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToDecimal(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToDecimal(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToIntPtr()
        {
            var f = Factory.Create<uint, IntPtr>();

            Debug.Assert(ManualConverter.UInt32ToIntPtr(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToIntPtr(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToIntPtr(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToIntPtr(UInt32.MaxValue) == f(UInt32.MaxValue));
        }


        public static void UInt32ToUIntPtr()
        {
            var f = Factory.Create<uint, UIntPtr>();

            Debug.Assert(ManualConverter.UInt32ToUIntPtr(0u) == f(0u));
            Debug.Assert(ManualConverter.UInt32ToUIntPtr(1u) == f(1u));
            Debug.Assert(ManualConverter.UInt32ToUIntPtr(UInt32.MinValue) == f(UInt32.MinValue));
            Debug.Assert(ManualConverter.UInt32ToUIntPtr(UInt32.MaxValue) == f(UInt32.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // Int64To
        //--------------------------------------------------------------------------------


        public static void Int64ToByte()
        {
            var f = Factory.Create<long, byte>();

            Debug.Assert(ManualConverter.Int64ToByte(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToByte(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToByte(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToByte(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToByte(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToSByte()
        {
            var f = Factory.Create<long, sbyte>();

            Debug.Assert(ManualConverter.Int64ToSByte(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToSByte(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToSByte(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToSByte(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToSByte(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToChar()
        {
            var f = Factory.Create<long, char>();

            Debug.Assert(ManualConverter.Int64ToChar(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToChar(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToChar(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToChar(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToChar(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToInt16()
        {
            var f = Factory.Create<long, short>();

            Debug.Assert(ManualConverter.Int64ToInt16(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToInt16(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToInt16(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToInt16(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToInt16(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToUInt16()
        {
            var f = Factory.Create<long, ushort>();

            Debug.Assert(ManualConverter.Int64ToUInt16(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToUInt16(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToUInt16(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToUInt16(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToUInt16(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToInt32()
        {
            var f = Factory.Create<long, int>();

            Debug.Assert(ManualConverter.Int64ToInt32(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToInt32(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToInt32(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToInt32(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToInt32(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToUInt32()
        {
            var f = Factory.Create<long, uint>();

            Debug.Assert(ManualConverter.Int64ToUInt32(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToUInt32(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToUInt32(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToUInt32(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToUInt32(Int64.MaxValue) == f(Int64.MaxValue));
        }

        // Nop long


        public static void Int64ToUInt64()
        {
            var f = Factory.Create<long, ulong>();

            Debug.Assert(ManualConverter.Int64ToUInt64(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToUInt64(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToUInt64(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToUInt64(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToUInt64(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToSingle()
        {
            var f = Factory.Create<long, float>();

            Debug.Assert(ManualConverter.Int64ToSingle(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToSingle(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToSingle(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToSingle(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToSingle(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToDouble()
        {
            var f = Factory.Create<long, double>();

            Debug.Assert(ManualConverter.Int64ToDouble(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToDouble(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToDouble(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToDouble(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToDouble(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToDecimal()
        {
            var f = Factory.Create<long, decimal>();

            Debug.Assert(ManualConverter.Int64ToDecimal(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToDecimal(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToDecimal(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToDecimal(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToDecimal(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToIntPtr()
        {
            var f = Factory.Create<long, IntPtr>();

            Debug.Assert(ManualConverter.Int64ToIntPtr(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToIntPtr(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToIntPtr(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToIntPtr(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToIntPtr(Int64.MaxValue) == f(Int64.MaxValue));
        }


        public static void Int64ToUIntPtr()
        {
            var f = Factory.Create<long, UIntPtr>();

            Debug.Assert(ManualConverter.Int64ToUIntPtr(0L) == f(0L));
            Debug.Assert(ManualConverter.Int64ToUIntPtr(1L) == f(1L));
            Debug.Assert(ManualConverter.Int64ToUIntPtr(-1L) == f(-1L));
            Debug.Assert(ManualConverter.Int64ToUIntPtr(Int64.MinValue) == f(Int64.MinValue));
            Debug.Assert(ManualConverter.Int64ToUIntPtr(Int64.MaxValue) == f(Int64.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // UInt64To
        //--------------------------------------------------------------------------------


        public static void UInt64ToByte()
        {
            var f = Factory.Create<ulong, byte>();

            Debug.Assert(ManualConverter.UInt64ToByte(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToByte(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToByte(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToByte(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToSByte()
        {
            var f = Factory.Create<ulong, sbyte>();

            Debug.Assert(ManualConverter.UInt64ToSByte(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToSByte(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToSByte(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToSByte(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToChar()
        {
            var f = Factory.Create<ulong, char>();

            Debug.Assert(ManualConverter.UInt64ToChar(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToChar(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToChar(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToChar(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToInt16()
        {
            var f = Factory.Create<ulong, short>();

            Debug.Assert(ManualConverter.UInt64ToInt16(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToInt16(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToInt16(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToInt16(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToUInt16()
        {
            var f = Factory.Create<ulong, ushort>();

            Debug.Assert(ManualConverter.UInt64ToUInt16(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToUInt16(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToUInt16(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToUInt16(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToInt32()
        {
            var f = Factory.Create<ulong, int>();

            Debug.Assert(ManualConverter.UInt64ToInt32(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToInt32(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToInt32(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToInt32(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToUInt32()
        {
            var f = Factory.Create<ulong, uint>();

            Debug.Assert(ManualConverter.UInt64ToUInt32(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToUInt32(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToUInt32(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToUInt32(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToInt64()
        {
            var f = Factory.Create<ulong, long>();

            Debug.Assert(ManualConverter.UInt64ToInt64(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToInt64(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToInt64(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToInt64(UInt64.MaxValue) == f(UInt64.MaxValue));
        }

        // Nop ulong


        public static void UInt64ToSingle()
        {
            var f = Factory.Create<ulong, float>();

            Debug.Assert(ManualConverter.UInt64ToSingle(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToSingle(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToSingle(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToSingle(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToDouble()
        {
            var f = Factory.Create<ulong, double>();

            Debug.Assert(ManualConverter.UInt64ToDouble(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToDouble(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToDouble(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToDouble(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToDecimal()
        {
            var f = Factory.Create<ulong, decimal>();

            Debug.Assert(ManualConverter.UInt64ToDecimal(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToDecimal(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToDecimal(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToDecimal(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToIntPtr()
        {
            var f = Factory.Create<ulong, IntPtr>();

            Debug.Assert(ManualConverter.UInt64ToIntPtr(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToIntPtr(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToIntPtr(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToIntPtr(UInt64.MaxValue) == f(UInt64.MaxValue));
        }


        public static void UInt64ToUIntPtr()
        {
            var f = Factory.Create<ulong, UIntPtr>();

            Debug.Assert(ManualConverter.UInt64ToUIntPtr(0ul) == f(0ul));
            Debug.Assert(ManualConverter.UInt64ToUIntPtr(1ul) == f(1ul));
            Debug.Assert(ManualConverter.UInt64ToUIntPtr(UInt64.MinValue) == f(UInt64.MinValue));
            Debug.Assert(ManualConverter.UInt64ToUIntPtr(UInt64.MaxValue) == f(UInt64.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // SingleTo
        //--------------------------------------------------------------------------------


        public static void SingleToByte()
        {
            var f = Factory.Create<float, byte>();

            Debug.Assert(ManualConverter.SingleToByte(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToByte(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToByte(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToByte(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToByte(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToSByte()
        {
            var f = Factory.Create<float, sbyte>();

            Debug.Assert(ManualConverter.SingleToSByte(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToSByte(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToSByte(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToSByte(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToSByte(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToChar()
        {
            var f = Factory.Create<float, char>();

            Debug.Assert(ManualConverter.SingleToChar(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToChar(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToChar(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToChar(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToChar(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToInt16()
        {
            var f = Factory.Create<float, short>();

            Debug.Assert(ManualConverter.SingleToInt16(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToInt16(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToInt16(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToInt16(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToInt16(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToUInt16()
        {
            var f = Factory.Create<float, ushort>();

            Debug.Assert(ManualConverter.SingleToUInt16(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToUInt16(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToUInt16(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToUInt16(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToUInt16(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToInt32()
        {
            var f = Factory.Create<float, int>();

            Debug.Assert(ManualConverter.SingleToInt32(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToInt32(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToInt32(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToInt32(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToInt32(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToUInt32()
        {
            var f = Factory.Create<float, uint>();

            Debug.Assert(ManualConverter.SingleToUInt32(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToUInt32(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToUInt32(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToUInt32(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToUInt32(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToInt64()
        {
            var f = Factory.Create<float, long>();

            Debug.Assert(ManualConverter.SingleToInt64(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToInt64(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToInt64(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToInt64(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToInt64(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToUInt64()
        {
            var f = Factory.Create<float, ulong>();

            Debug.Assert(ManualConverter.SingleToUInt64(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToUInt64(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToUInt64(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToUInt64(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToUInt64(Single.MaxValue) == f(Single.MaxValue));
        }

        // Nop float


        public static void SingleToDouble()
        {
            var f = Factory.Create<float, double>();

            Debug.Assert(ManualConverter.SingleToDouble(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToDouble(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToDouble(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToDouble(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToDouble(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToDecimal()
        {
            var f = Factory.Create<float, decimal>();

            Debug.Assert(ManualConverter.SingleToDecimal(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToDecimal(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToDecimal(-1f) == f(-1f));
            //Debug.Assert(ManualConverter.SingleToDecimal(Single.MinValue) == f(Single.MinValue));
            //Debug.Assert(ManualConverter.SingleToDecimal(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToIntPtr()
        {
            var f = Factory.Create<float, IntPtr>();

            Debug.Assert(ManualConverter.SingleToIntPtr(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToIntPtr(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToIntPtr(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToIntPtr(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToIntPtr(Single.MaxValue) == f(Single.MaxValue));
        }


        public static void SingleToUIntPtr()
        {
            var f = Factory.Create<float, UIntPtr>();

            Debug.Assert(ManualConverter.SingleToUIntPtr(0f) == f(0f));
            Debug.Assert(ManualConverter.SingleToUIntPtr(1f) == f(1f));
            Debug.Assert(ManualConverter.SingleToUIntPtr(-1f) == f(-1f));
            Debug.Assert(ManualConverter.SingleToUIntPtr(Single.MinValue) == f(Single.MinValue));
            Debug.Assert(ManualConverter.SingleToUIntPtr(Single.MaxValue) == f(Single.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // DoubleTo
        //--------------------------------------------------------------------------------


        public static void DoubleToByte()
        {
            var f = Factory.Create<double, byte>();

            Debug.Assert(ManualConverter.DoubleToByte(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToByte(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToByte(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToByte(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToByte(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToSByte()
        {
            var f = Factory.Create<double, sbyte>();

            Debug.Assert(ManualConverter.DoubleToSByte(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToSByte(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToSByte(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToSByte(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToSByte(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToChar()
        {
            var f = Factory.Create<double, char>();

            Debug.Assert(ManualConverter.DoubleToChar(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToChar(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToChar(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToChar(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToChar(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToInt16()
        {
            var f = Factory.Create<double, short>();

            Debug.Assert(ManualConverter.DoubleToInt16(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToInt16(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToInt16(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToInt16(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToInt16(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToUInt16()
        {
            var f = Factory.Create<double, ushort>();

            Debug.Assert(ManualConverter.DoubleToUInt16(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToUInt16(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToUInt16(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToUInt16(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToUInt16(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToInt32()
        {
            var f = Factory.Create<double, int>();

            Debug.Assert(ManualConverter.DoubleToInt32(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToInt32(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToInt32(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToInt32(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToInt32(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToUInt32()
        {
            var f = Factory.Create<double, uint>();

            Debug.Assert(ManualConverter.DoubleToUInt32(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToUInt32(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToUInt32(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToUInt32(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToUInt32(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToInt64()
        {
            var f = Factory.Create<double, long>();

            Debug.Assert(ManualConverter.DoubleToInt64(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToInt64(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToInt64(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToInt64(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToInt64(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToUInt64()
        {
            var f = Factory.Create<double, ulong>();

            Debug.Assert(ManualConverter.DoubleToUInt64(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToUInt64(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToUInt64(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToUInt64(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToUInt64(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToSingle()
        {
            var f = Factory.Create<double, float>();

            Debug.Assert(ManualConverter.DoubleToSingle(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToSingle(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToSingle(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToSingle(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToSingle(Double.MinValue) == f(Double.MinValue));
        }

        // Nop double


        public static void DoubleToDecimal()
        {
            var f = Factory.Create<double, decimal>();

            Debug.Assert(ManualConverter.DoubleToDecimal(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToDecimal(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToDecimal(-1d) == f(-1d));
            //Debug.Assert(ManualConverter.DoubleToDecimal(Double.MinValue) == f(Double.MinValue));
            //Debug.Assert(ManualConverter.DoubleToDecimal(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToIntPtr()
        {
            var f = Factory.Create<double, IntPtr>();

            Debug.Assert(ManualConverter.DoubleToIntPtr(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToIntPtr(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToIntPtr(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToIntPtr(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToIntPtr(Double.MaxValue) == f(Double.MaxValue));
        }


        public static void DoubleToUIntPtr()
        {
            var f = Factory.Create<double, UIntPtr>();

            Debug.Assert(ManualConverter.DoubleToUIntPtr(0d) == f(0d));
            Debug.Assert(ManualConverter.DoubleToUIntPtr(1d) == f(1d));
            Debug.Assert(ManualConverter.DoubleToUIntPtr(-1d) == f(-1d));
            Debug.Assert(ManualConverter.DoubleToUIntPtr(Double.MinValue) == f(Double.MinValue));
            Debug.Assert(ManualConverter.DoubleToUIntPtr(Double.MaxValue) == f(Double.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // DecimalTo
        //--------------------------------------------------------------------------------


        public static void DecimalToByte()
        {
            var f = Factory.Create<decimal, byte>();

            Debug.Assert(ManualConverter.DecimalToByte(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToByte(1m) == f(1m));
            //Debug.Assert(ManualConverter.DecimalToByte(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToByte(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToByte(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToSByte()
        {
            var f = Factory.Create<decimal, sbyte>();

            Debug.Assert(ManualConverter.DecimalToSByte(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToSByte(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToSByte(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToSByte(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToSByte(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToChar()
        {
            var f = Factory.Create<decimal, char>();

            Debug.Assert(ManualConverter.DecimalToChar(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToChar(1m) == f(1m));
            //Debug.Assert(ManualConverter.DecimalToChar(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToChar(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToChar(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToInt16()
        {
            var f = Factory.Create<decimal, short>();

            Debug.Assert(ManualConverter.DecimalToInt16(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToInt16(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToInt16(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToInt16(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToInt16(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToUInt16()
        {
            var f = Factory.Create<decimal, ushort>();

            Debug.Assert(ManualConverter.DecimalToUInt16(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToUInt16(1m) == f(1m));
            //Debug.Assert(ManualConverter.DecimalToUInt16(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToUInt16(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToUInt16(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToInt32()
        {
            var f = Factory.Create<decimal, int>();

            Debug.Assert(ManualConverter.DecimalToInt32(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToInt32(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToInt32(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToInt32(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToInt32(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToUInt32()
        {
            var f = Factory.Create<decimal, uint>();

            Debug.Assert(ManualConverter.DecimalToUInt32(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToUInt32(1m) == f(1m));
            //Debug.Assert(ManualConverter.DecimalToUInt32(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToUInt32(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToUInt32(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToInt64()
        {
            var f = Factory.Create<decimal, long>();

            Debug.Assert(ManualConverter.DecimalToInt64(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToInt64(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToInt64(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToInt64(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToInt64(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToUInt64()
        {
            var f = Factory.Create<decimal, ulong>();

            Debug.Assert(ManualConverter.DecimalToUInt64(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToUInt64(1m) == f(1m));
            //Debug.Assert(ManualConverter.DecimalToUInt64(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToUInt64(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToUInt64(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToSingle()
        {
            var f = Factory.Create<decimal, float>();

            Debug.Assert(ManualConverter.DecimalToSingle(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToSingle(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToSingle(-1m) == f(-1m));
            Debug.Assert(ManualConverter.DecimalToSingle(Decimal.MinValue) == f(Decimal.MinValue));
            Debug.Assert(ManualConverter.DecimalToSingle(Decimal.MinValue) == f(Decimal.MinValue));
        }


        public static void DecimalToDouble()
        {
            var f = Factory.Create<decimal, double>();

            Debug.Assert(ManualConverter.DecimalToDouble(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToDouble(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToDouble(-1m) == f(-1m));
            Debug.Assert(ManualConverter.DecimalToDouble(Decimal.MinValue) == f(Decimal.MinValue));
            Debug.Assert(ManualConverter.DecimalToDouble(Decimal.MinValue) == f(Decimal.MinValue));
        }

        // Nop decimal


        public static void DecimalToIntPtr()
        {
            var f = Factory.Create<decimal, IntPtr>();

            Debug.Assert(ManualConverter.DecimalToIntPtr(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToIntPtr(1m) == f(1m));
            Debug.Assert(ManualConverter.DecimalToIntPtr(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToIntPtr(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToIntPtr(Decimal.MaxValue) == f(Decimal.MaxValue));
        }


        public static void DecimalToUIntPtr()
        {
            var f = Factory.Create<decimal, UIntPtr>();

            Debug.Assert(ManualConverter.DecimalToUIntPtr(0m) == f(0m));
            Debug.Assert(ManualConverter.DecimalToUIntPtr(1m) == f(1m));
            //Debug.Assert(ManualConverter.DecimalToUIntPtr(-1m) == f(-1m));
            //Debug.Assert(ManualConverter.DecimalToUIntPtr(Decimal.MinValue) == f(Decimal.MinValue));
            //Debug.Assert(ManualConverter.DecimalToUIntPtr(Decimal.MaxValue) == f(Decimal.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // IntPtrTo
        //--------------------------------------------------------------------------------


        public static void IntPtrToByte()
        {
            var f = Factory.Create<IntPtr, byte>();

            Debug.Assert(ManualConverter.IntPtrToByte(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToByte((IntPtr)1) == f((IntPtr)1));
            //Debug.Assert(ManualConverter.IntPtrToByte(IntPtr.MinValue) == f(IntPtr.MinValue));
            //Debug.Assert(ManualConverter.IntPtrToByte(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToSByte()
        {
            var f = Factory.Create<IntPtr, sbyte>();

            Debug.Assert(ManualConverter.IntPtrToSByte(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToSByte((IntPtr)1) == f((IntPtr)1));
            //Debug.Assert(ManualConverter.IntPtrToSByte(IntPtr.MinValue) == f(IntPtr.MinValue));
            //Debug.Assert(ManualConverter.IntPtrToSByte(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToChar()
        {
            var f = Factory.Create<IntPtr, char>();

            Debug.Assert(ManualConverter.IntPtrToChar(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToChar((IntPtr)1) == f((IntPtr)1));
            //Debug.Assert(ManualConverter.IntPtrToChar(IntPtr.MinValue) == f(IntPtr.MinValue));
            //Debug.Assert(ManualConverter.IntPtrToChar(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToInt16()
        {
            var f = Factory.Create<IntPtr, short>();

            Debug.Assert(ManualConverter.IntPtrToInt16(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToInt16((IntPtr)1) == f((IntPtr)1));
            //Debug.Assert(ManualConverter.IntPtrToInt16(IntPtr.MinValue) == f(IntPtr.MinValue));
            //Debug.Assert(ManualConverter.IntPtrToInt16(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToUInt16()
        {
            var f = Factory.Create<IntPtr, ushort>();

            Debug.Assert(ManualConverter.IntPtrToUInt16(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToUInt16((IntPtr)1) == f((IntPtr)1));
            //Debug.Assert(ManualConverter.IntPtrToUInt16(IntPtr.MinValue) == f(IntPtr.MinValue));
            //Debug.Assert(ManualConverter.IntPtrToUInt16(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToInt32()
        {
            var f = Factory.Create<IntPtr, int>();

            Debug.Assert(ManualConverter.IntPtrToInt32(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToInt32((IntPtr)1) == f((IntPtr)1));
            //Debug.Assert(ManualConverter.IntPtrToInt32(IntPtr.MinValue) == f(IntPtr.MinValue));
            //Debug.Assert(ManualConverter.IntPtrToInt32(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToUInt32()
        {
            var f = Factory.Create<IntPtr, uint>();

            Debug.Assert(ManualConverter.IntPtrToUInt32(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToUInt32((IntPtr)1) == f((IntPtr)1));
            //Debug.Assert(ManualConverter.IntPtrToUInt32(IntPtr.MinValue) == f(IntPtr.MinValue));
            //Debug.Assert(ManualConverter.IntPtrToUInt32(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToInt64()
        {
            var f = Factory.Create<IntPtr, long>();

            Debug.Assert(ManualConverter.IntPtrToInt64(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToInt64((IntPtr)1) == f((IntPtr)1));
            Debug.Assert(ManualConverter.IntPtrToInt64(IntPtr.MinValue) == f(IntPtr.MinValue));
            Debug.Assert(ManualConverter.IntPtrToInt64(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToUInt64()
        {
            var f = Factory.Create<IntPtr, ulong>();

            Debug.Assert(ManualConverter.IntPtrToUInt64(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToUInt64((IntPtr)1) == f((IntPtr)1));
            Debug.Assert(ManualConverter.IntPtrToUInt64(IntPtr.MinValue) == f(IntPtr.MinValue));
            Debug.Assert(ManualConverter.IntPtrToUInt64(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }


        public static void IntPtrToSingle()
        {
            var f = Factory.Create<IntPtr, float>();

            Debug.Assert(ManualConverter.IntPtrToSingle(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToSingle((IntPtr)1) == f((IntPtr)1));
            Debug.Assert(ManualConverter.IntPtrToSingle(IntPtr.MinValue) == f(IntPtr.MinValue));
            Debug.Assert(ManualConverter.IntPtrToSingle(IntPtr.MinValue) == f(IntPtr.MinValue));
        }


        public static void IntPtrToDouble()
        {
            var f = Factory.Create<IntPtr, double>();

            Debug.Assert(ManualConverter.IntPtrToDouble(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToDouble((IntPtr)1) == f((IntPtr)1));
            Debug.Assert(ManualConverter.IntPtrToDouble(IntPtr.MinValue) == f(IntPtr.MinValue));
            Debug.Assert(ManualConverter.IntPtrToDouble(IntPtr.MinValue) == f(IntPtr.MinValue));
        }


        public static void IntPtrToDecimal()
        {
            var f = Factory.Create<IntPtr, decimal>();

            Debug.Assert(ManualConverter.IntPtrToDecimal(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToDecimal((IntPtr)1) == f((IntPtr)1));
            Debug.Assert(ManualConverter.IntPtrToDecimal(IntPtr.MinValue) == f(IntPtr.MinValue));
            Debug.Assert(ManualConverter.IntPtrToDecimal(IntPtr.MinValue) == f(IntPtr.MinValue));
        }

        // Nop IntPtr


        public static void IntPtrToUIntPtr()
        {
            var f = Factory.Create<IntPtr, UIntPtr>();

            Debug.Assert(ManualConverter.IntPtrToUIntPtr(IntPtr.Zero) == f(IntPtr.Zero));
            Debug.Assert(ManualConverter.IntPtrToUIntPtr((IntPtr)1) == f((IntPtr)1));
            Debug.Assert(ManualConverter.IntPtrToUIntPtr(IntPtr.MinValue) == f(IntPtr.MinValue));
            Debug.Assert(ManualConverter.IntPtrToUIntPtr(IntPtr.MaxValue) == f(IntPtr.MaxValue));
        }

        //--------------------------------------------------------------------------------
        // UIntPtrTo
        //--------------------------------------------------------------------------------


        public static void UIntPtrToByte()
        {
            var f = Factory.Create<UIntPtr, byte>();

            Debug.Assert(ManualConverter.UIntPtrToByte(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToByte((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToByte(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            //Debug.Assert(ManualConverter.UIntPtrToByte(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToSByte()
        {
            var f = Factory.Create<UIntPtr, sbyte>();

            Debug.Assert(ManualConverter.UIntPtrToSByte(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToSByte((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToSByte(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            //Debug.Assert(ManualConverter.UIntPtrToSByte(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToChar()
        {
            var f = Factory.Create<UIntPtr, char>();

            Debug.Assert(ManualConverter.UIntPtrToChar(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToChar((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToChar(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            //Debug.Assert(ManualConverter.UIntPtrToChar(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToInt16()
        {
            var f = Factory.Create<UIntPtr, short>();

            Debug.Assert(ManualConverter.UIntPtrToInt16(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToInt16((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToInt16(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            //Debug.Assert(ManualConverter.UIntPtrToInt16(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToUInt16()
        {
            var f = Factory.Create<UIntPtr, ushort>();

            Debug.Assert(ManualConverter.UIntPtrToUInt16(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToUInt16((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToUInt16(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            //Debug.Assert(ManualConverter.UIntPtrToUInt16(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToInt32()
        {
            var f = Factory.Create<UIntPtr, int>();

            Debug.Assert(ManualConverter.UIntPtrToInt32(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToInt32((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToInt32(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            //Debug.Assert(ManualConverter.UIntPtrToInt32(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToUInt32()
        {
            var f = Factory.Create<UIntPtr, uint>();

            Debug.Assert(ManualConverter.UIntPtrToUInt32(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToUInt32((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToUInt32(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            //Debug.Assert(ManualConverter.UIntPtrToUInt32(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToInt64()
        {
            var f = Factory.Create<UIntPtr, long>();

            Debug.Assert(ManualConverter.UIntPtrToInt64(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToInt64((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToInt64(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            Debug.Assert(ManualConverter.UIntPtrToInt64(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToUInt64()
        {
            var f = Factory.Create<UIntPtr, ulong>();

            Debug.Assert(ManualConverter.UIntPtrToUInt64(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToUInt64((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToUInt64(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            Debug.Assert(ManualConverter.UIntPtrToUInt64(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }


        public static void UIntPtrToSingle()
        {
            var f = Factory.Create<UIntPtr, float>();

            Debug.Assert(ManualConverter.UIntPtrToSingle(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToSingle((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToSingle(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            Debug.Assert(ManualConverter.UIntPtrToSingle(UIntPtr.MinValue) == f(UIntPtr.MinValue));
        }


        public static void UIntPtrToDouble()
        {
            var f = Factory.Create<UIntPtr, double>();

            Debug.Assert(ManualConverter.UIntPtrToDouble(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToDouble((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToDouble(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            Debug.Assert(ManualConverter.UIntPtrToDouble(UIntPtr.MinValue) == f(UIntPtr.MinValue));
        }


        public static void UIntPtrToDecimal()
        {
            var f = Factory.Create<UIntPtr, decimal>();

            Debug.Assert(ManualConverter.UIntPtrToDecimal(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToDecimal((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToDecimal(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            Debug.Assert(ManualConverter.UIntPtrToDecimal(UIntPtr.MinValue) == f(UIntPtr.MinValue));
        }


        public static void UIntPtrToIntPtr()
        {
            var f = Factory.Create<UIntPtr, IntPtr>();

            Debug.Assert(ManualConverter.UIntPtrToIntPtr(UIntPtr.Zero) == f(UIntPtr.Zero));
            Debug.Assert(ManualConverter.UIntPtrToIntPtr((UIntPtr)1) == f((UIntPtr)1));
            Debug.Assert(ManualConverter.UIntPtrToIntPtr(UIntPtr.MinValue) == f(UIntPtr.MinValue));
            Debug.Assert(ManualConverter.UIntPtrToIntPtr(UIntPtr.MaxValue) == f(UIntPtr.MaxValue));
        }
    }
}

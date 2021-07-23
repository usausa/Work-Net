namespace EmitConverterTest
{
    using System;

    public class ManualConverter
    {
        public static sbyte ByteToSByte(byte x) => (sbyte)x;
        public static char ByteToChar(byte x) => (char)x;
        public static decimal ByteToDecimal(byte x) => x;
        public static double ByteToDouble(byte x) => x;
        public static float ByteToSingle(byte x) => x;
        public static int ByteToInt32(byte x) => x;
        public static uint ByteToUInt32(byte x) => x;
        public static IntPtr ByteToIntPtr(byte x) => (IntPtr)x;
        public static UIntPtr ByteToUIntPtr(byte x) => (UIntPtr)x;
        public static long ByteToInt64(byte x) => x;
        public static ulong ByteToUInt64(byte x) => x;
        public static short ByteToInt16(byte x) => x;
        public static ushort ByteToUInt16(byte x) => x;

        public static byte SByteToByte(sbyte x) => (byte)x;
        public static char SByteToChar(sbyte x) => (char)x;
        public static decimal SByteToDecimal(sbyte x) => x;
        public static double SByteToDouble(sbyte x) => x;
        public static float SByteToSingle(sbyte x) => x;
        public static int SByteToInt32(sbyte x) => x;
        public static uint SByteToUInt32(sbyte x) => (uint)x;
        public static IntPtr SByteToIntPtr(sbyte x) => (IntPtr)x;
        public static UIntPtr SByteToUIntPtr(sbyte x) => (UIntPtr)x;
        public static long SByteToInt64(sbyte x) => x;
        public static ulong SByteToUInt64(sbyte x) => (ulong)x;
        public static short SByteToInt16(sbyte x) => x;
        public static ushort SByteToUInt16(sbyte x) => (ushort)x;

        public static byte CharToByte(char x) => (byte)x;
        public static sbyte CharToSByte(char x) => (sbyte)x;
        public static decimal CharToDecimal(char x) => x;
        public static double CharToDouble(char x) => x;
        public static float CharToSingle(char x) => x;
        public static int CharToInt32(char x) => x;
        public static uint CharToUInt32(char x) => x;
        public static IntPtr CharToIntPtr(char x) => (IntPtr)x;
        public static UIntPtr CharToUIntPtr(char x) => (UIntPtr)x;
        public static long CharToInt64(char x) => x;
        public static ulong CharToUInt64(char x) => x;
        public static short CharToInt16(char x) => (short)x;
        public static ushort CharToUInt16(char x) => x;

        public static byte Int16ToByte(short x) => (byte)x;
        public static sbyte Int16ToSByte(short x) => (sbyte)x;
        public static char Int16ToChar(short x) => (char)x;
        public static decimal Int16ToDecimal(short x) => x;
        public static double Int16ToDouble(short x) => x;
        public static float Int16ToSingle(short x) => x;
        public static int Int16ToInt32(short x) => x;
        public static uint Int16ToUInt32(short x) => (uint)x;
        public static IntPtr Int16ToIntPtr(short x) => (IntPtr)x;
        public static UIntPtr Int16ToUIntPtr(short x) => (UIntPtr)x;
        public static long Int16ToInt64(short x) => x;
        public static ulong Int16ToUInt64(short x) => (ulong)x;
        public static ushort Int16ToUInt16(short x) => (ushort)x;

        public static byte UInt16ToByte(ushort x) => (byte)x;
        public static sbyte UInt16ToSByte(ushort x) => (sbyte)x;
        public static char UInt16ToChar(ushort x) => (char)x;
        public static decimal UInt16ToDecimal(ushort x) => x;
        public static double UInt16ToDouble(ushort x) => x;
        public static float UInt16ToSingle(ushort x) => x;
        public static int UInt16ToInt32(ushort x) => x;
        public static uint UInt16ToUInt32(ushort x) => x;
        public static IntPtr UInt16ToIntPtr(ushort x) => (IntPtr)x;
        public static UIntPtr UInt16ToUIntPtr(ushort x) => (UIntPtr)x;
        public static long UInt16ToInt64(ushort x) => x;
        public static ulong UInt16ToUInt64(ushort x) => x;
        public static short UInt16ToInt16(ushort x) => (short)x;

        public static byte Int32ToByte(int x) => (byte)x;
        public static sbyte Int32ToSByte(int x) => (sbyte)x;
        public static char Int32ToChar(int x) => (char)x;
        public static decimal Int32ToDecimal(int x) => x;
        public static double Int32ToDouble(int x) => x;
        public static float Int32ToSingle(int x) => x;
        public static uint Int32ToUInt32(int x) => (uint)x;
        public static IntPtr Int32ToIntPtr(int x) => (IntPtr)x;
        public static UIntPtr Int32ToUIntPtr(int x) => (UIntPtr)x;
        public static long Int32ToInt64(int x) => x;
        public static ulong Int32ToUInt64(int x) => (ulong)x;
        public static short Int32ToInt16(int x) => (short)x;
        public static ushort Int32ToUInt16(int x) => (ushort)x;

        public static byte UInt32ToByte(uint x) => (byte)x;
        public static sbyte UInt32ToSByte(uint x) => (sbyte)x;
        public static char UInt32ToChar(uint x) => (char)x;
        public static decimal UInt32ToDecimal(uint x) => x;
        public static double UInt32ToDouble(uint x) => x;
        public static float UInt32ToSingle(uint x) => x;
        public static int UInt32ToInt32(uint x) => (int)x;
        public static IntPtr UInt32ToIntPtr(uint x) => (IntPtr)x;
        public static UIntPtr UInt32ToUIntPtr(uint x) => (UIntPtr)x;
        public static long UInt32ToInt64(uint x) => x;
        public static ulong UInt32ToUInt64(uint x) => x;
        public static short UInt32ToInt16(uint x) => (short)x;
        public static ushort UInt32ToUInt16(uint x) => (ushort)x;

        public static byte Int64ToByte(long x) => (byte)x;
        public static sbyte Int64ToSByte(long x) => (sbyte)x;
        public static char Int64ToChar(long x) => (char)x;
        public static decimal Int64ToDecimal(long x) => x;
        public static double Int64ToDouble(long x) => x;
        public static float Int64ToSingle(long x) => x;
        public static int Int64ToInt32(long x) => (int)x;
        public static uint Int64ToUInt32(long x) => (uint)x;
        public static IntPtr Int64ToIntPtr(long x) => (IntPtr)x;
        public static UIntPtr Int64ToUIntPtr(long x) => (UIntPtr)x;
        public static ulong Int64ToUInt64(long x) => (ulong)x;
        public static short Int64ToInt16(long x) => (short)x;
        public static ushort Int64ToUInt16(long x) => (ushort)x;

        public static byte UInt64ToByte(ulong x) => (byte)x;
        public static sbyte UInt64ToSByte(ulong x) => (sbyte)x;
        public static char UInt64ToChar(ulong x) => (char)x;
        public static decimal UInt64ToDecimal(ulong x) => x;
        public static double UInt64ToDouble(ulong x) => x;
        public static float UInt64ToSingle(ulong x) => x;
        public static int UInt64ToInt32(ulong x) => (int)x;
        public static uint UInt64ToUInt32(ulong x) => (uint)x;
        public static IntPtr UInt64ToIntPtr(ulong x) => (IntPtr)x;
        public static UIntPtr UInt64ToUIntPtr(ulong x) => (UIntPtr)x;
        public static long UInt64ToInt64(ulong x) => (long)x;
        public static short UInt64ToInt16(ulong x) => (short)x;
        public static ushort UInt64ToUInt16(ulong x) => (ushort)x;

        public static byte SingleToByte(float x) => (byte)x;
        public static sbyte SingleToSByte(float x) => (sbyte)x;
        public static char SingleToChar(float x) => (char)x;
        public static decimal SingleToDecimal(float x) => (decimal)x;
        public static double SingleToDouble(float x) => x;
        public static int SingleToInt32(float x) => (int)x;
        public static uint SingleToUInt32(float x) => (uint)x;
        public static IntPtr SingleToIntPtr(float x) => (IntPtr)x;
        public static UIntPtr SingleToUIntPtr(float x) => (UIntPtr)x;
        public static long SingleToInt64(float x) => (long)x;
        public static ulong SingleToUInt64(float x) => (ulong)x;
        public static short SingleToInt16(float x) => (short)x;
        public static ushort SingleToUInt16(float x) => (ushort)x;

        public static byte DoubleToByte(double x) => (byte)x;
        public static sbyte DoubleToSByte(double x) => (sbyte)x;
        public static char DoubleToChar(double x) => (char)x;
        public static decimal DoubleToDecimal(double x) => (decimal)x;
        public static float DoubleToSingle(double x) => (float)x;
        public static int DoubleToInt32(double x) => (int)x;
        public static uint DoubleToUInt32(double x) => (uint)x;
        public static IntPtr DoubleToIntPtr(double x) => (IntPtr)x;
        public static UIntPtr DoubleToUIntPtr(double x) => (UIntPtr)x;
        public static long DoubleToInt64(double x) => (long)x;
        public static ulong DoubleToUInt64(double x) => (ulong)x;
        public static short DoubleToInt16(double x) => (short)x;
        public static ushort DoubleToUInt16(double x) => (ushort)x;

        public static byte DecimalToByte(decimal x) => (byte)x;
        public static sbyte DecimalToSByte(decimal x) => (sbyte)x;
        public static char DecimalToChar(decimal x) => (char)x;
        public static double DecimalToDouble(decimal x) => (double)x;
        public static float DecimalToSingle(decimal x) => (float)x;
        public static int DecimalToInt32(decimal x) => (int)x;
        public static uint DecimalToUInt32(decimal x) => (uint)x;
        public static IntPtr DecimalToIntPtr(decimal x) => (IntPtr)x;
        public static UIntPtr DecimalToUIntPtr(decimal x) => (UIntPtr)x;
        public static long DecimalToInt64(decimal x) => (long)x;
        public static ulong DecimalToUInt64(decimal x) => (ulong)x;
        public static short DecimalToInt16(decimal x) => (short)x;
        public static ushort DecimalToUInt16(decimal x) => (ushort)x;

        public static byte IntPtrToByte(IntPtr x) => (byte)x;
        public static sbyte IntPtrToSByte(IntPtr x) => (sbyte)x;
        public static char IntPtrToChar(IntPtr x) => (char)x;
        public static decimal IntPtrToDecimal(IntPtr x) => (decimal)x;
        public static double IntPtrToDouble(IntPtr x) => (double)x;
        public static float IntPtrToSingle(IntPtr x) => (float)x;
        public static int IntPtrToInt32(IntPtr x) => (int)x;
        public static uint IntPtrToUInt32(IntPtr x) => (uint)x;
        public static UIntPtr IntPtrToUIntPtr(IntPtr x) => (UIntPtr)(long)x;
        public static long IntPtrToInt64(IntPtr x) => (long)x;
        public static ulong IntPtrToUInt64(IntPtr x) => (ulong)x;
        public static short IntPtrToInt16(IntPtr x) => (short)x;
        public static ushort IntPtrToUInt16(IntPtr x) => (ushort)x;

        public static byte UIntPtrToByte(UIntPtr x) => (byte)x;
        public static sbyte UIntPtrToSByte(UIntPtr x) => (sbyte)x;
        public static char UIntPtrToChar(UIntPtr x) => (char)x;
        public static decimal UIntPtrToDecimal(UIntPtr x) => (decimal)x;
        public static double UIntPtrToDouble(UIntPtr x) => (double)x;
        public static float UIntPtrToSingle(UIntPtr x) => (float)x;
        public static int UIntPtrToInt32(UIntPtr x) => (int)x;
        public static uint UIntPtrToUInt32(UIntPtr x) => (uint)x;
        public static IntPtr UIntPtrToIntPtr(UIntPtr x) => (IntPtr)(ulong)x;
        public static long UIntPtrToInt64(UIntPtr x) => (long)x;
        public static ulong UIntPtrToUInt64(UIntPtr x) => (ulong)x;
        public static short UIntPtrToInt16(UIntPtr x) => (short)x;
        public static ushort UIntPtrToUInt16(UIntPtr x) => (ushort)x;
    }
}

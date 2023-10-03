using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorkIL
{
    public static class ValueConvert
    {
        public static int IntEnum2Int(IntEnum value) => (int)value; // Nop

        public static int LongEnum2Int(LongEnum value) => (int)value; // conv.i4(Emitに同じ？)

        public static int NullableIntEnum2Int(IntEnum? value) => (int)value!; // Call getValue(Nop)
        public static int NullableIntEnum2Int2(IntEnum? value) => (int)value!.Value; // Call getValue(Nop)

        public static int NullableLongEnum2Int(LongEnum? value) => (int)value!; // Call getValue(Nop) + conv.i4(Emitに同じ？)
        public static int NullableLongEnum2Int2(LongEnum? value) => (int)value!.Value; // Call getValue(Nop) + conv.i4(Emitに同じ？)

        public static int NullableInt2Int(int? value) => value!.Value;
        public static int NullableInt2Int2(int? value) => (int)value!;
        public static int? NullableInt2NullableInt(int? value) => value;

        public static MyStruct StructToStruct(MyStruct value) => value;
        public static MyStruct NullableStructToStruct(MyStruct? value) => value!.Value;
        public static MyStruct? StructToNullableStruct(MyStruct value) => value; // ldarg.0, Newobj
        public static MyStruct? NullableStructToNullableStruct(MyStruct? value) => value;

        // Enum(uとかもありうる)

        public enum IntEnum
        {
            Zero
        }

        public enum ByteEnum : byte
        {
            Zero
        }

        public enum ShortEnum : short
        {
            Zero
        }

        public enum LongEnum : ulong
        {
            Zero
        }

        public struct MyStruct
        {
            public int Value;
        }
    }
}

using System;
using System.Diagnostics;
using System.Reflection.Emit;

namespace WorkConstSetter
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = new Data();

            var setInt = Factory.MakeSetter(nameof(Data.IntValue), 1);
            setInt(data);
            Debug.WriteLine(data.IntValue);
        }
    }

    public class Sample
    {
        public static int Value;

        public int Get() => Value;
    }


    public static class Factory
    {
        public static Action<Data> MakeSetter<T>(string name, T value)
        {
            var type = typeof(T);
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new[] { typeof(object), typeof(Data) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var pi = typeof(Data).GetProperty(name)!;

            // Stack data
            ilGenerator.Emit(OpCodes.Ldarg_1);

            // Set
            if (type == typeof(int))
            {
                ilGenerator.EmitConstInt(value!);
            }
            else
            {
                throw new NotSupportedException();
            }

            ilGenerator.Emit(OpCodes.Callvirt, pi.SetMethod!);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<Data>>();
        }
    }

    public static class ILGeneratorExtensions
    {
        public static bool CanEmitConst(this Type type)
        {
            // TODO
            return type == typeof(int);
        }

        public static void EmitConstInt(this ILGenerator ilGenerator, object value)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
        }
    }

    public class Data
    {
        public string? StringValue { get; set; }

        public bool BoolValue { get; set; }
        public byte ByteValue { get; set; }
        public sbyte SByteValue { get; set; }
        public char CharValue { get; set; }
        public short ShortValue { get; set; }
        public ushort UShortValue { get; set; }
        public int IntValue { get; set; }
        public uint UIntValue { get; set; }
        public long LongValue { get; set; }
        public ulong ULongValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        public IntPtr IntPtrValue { get; set; }
        public UIntPtr UIntPtrValue { get; set; }

        public MyEnum EnumValue { get; set; }
        public MyEnumShort EnumShortValue { get; set; }
        public MyEnumByte EnumByteValue { get; set; }

        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }

        public bool? NullableBoolValue { get; set; }
        public byte? NullableByteValue { get; set; }
        public sbyte? NullableSByteValue { get; set; }
        public char? NullableCharValue { get; set; }
        public short? NullableShortValue { get; set; }
        public ushort? NullableUShortValue { get; set; }
        public int? NullableIntValue { get; set; }
        public uint? NullableUIntValue { get; set; }
        public long? NullableLongValue { get; set; }
        public ulong? NullableULongValue { get; set; }
        public float? NullableFloatValue { get; set; }
        public double? NullableDoubleValue { get; set; }
        public decimal? NullableDecimalValue { get; set; }
        public IntPtr? NullableIntPtrValue { get; set; }
        public UIntPtr? NullableUIntPtrValue { get; set; }

        public MyEnum? NullableEnumValue { get; set; }
        public MyEnumShort? NullableEnumShortValue { get; set; }
        public MyEnumByte? NullableEnumByteValue { get; set; }

        public DateTime? NullableDateTimeValue { get; set; }
        public DateTimeOffset? NullableDateTimeOffsetValue { get; set; }
    }

    public enum MyEnum
    {
        Zero,
        One
    }

    public enum MyEnumShort : short
    {
        Zero,
        One
    }

    public enum MyEnumByte : byte
    {
        Zero,
        One
    }
}

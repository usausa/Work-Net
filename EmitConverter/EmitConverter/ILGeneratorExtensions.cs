namespace EmitConverter
{
    using System;
    using System.Reflection.Emit;

    public static class ILGeneratorExtensions
    {
        public static bool EmitPrimitiveConvert(this ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            if (sourceType == typeof(byte))
            {
                if (destinationType == typeof(byte))
                {
                    // Nop same
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(short))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(ushort))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(int))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(uint))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(long))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(ulong))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // implicit
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(sbyte))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    // Nop same
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(uint))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(long))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(ulong))
                {
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // implicit
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(char))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    // Nop same
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(int))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(uint))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(long))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(ulong))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(short))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    // Nop same
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(uint))
                {
                    // Nop
                }
                else if (destinationType == typeof(long))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(ulong))
                {
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(ushort))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    // Nop
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    // Nop same
                }
                else if (destinationType == typeof(int))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(uint))
                {
                    // Nop implicit
                }
                else if (destinationType == typeof(long))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(ulong))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(int))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    // Nop same
                }
                else if (destinationType == typeof(uint))
                {
                    // Nop
                }
                else if (destinationType == typeof(long))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(ulong))
                {
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(uint))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    // Nop
                }
                else if (destinationType == typeof(uint))
                {
                    // Nop same
                }
                else if (destinationType == typeof(long))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(ulong))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(long))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Conv_I4);
                }
                else if (destinationType == typeof(uint))
                {
                    ilGenerator.Emit(OpCodes.Conv_U4);
                }
                else if (destinationType == typeof(long))
                {
                    // Nop same
                }
                else if (destinationType == typeof(ulong))
                {
                    // Nop
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(ulong))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Conv_I4);
                }
                else if (destinationType == typeof(uint))
                {
                    ilGenerator.Emit(OpCodes.Conv_U4);
                }
                else if (destinationType == typeof(long))
                {
                    // Nop
                }
                else if (destinationType == typeof(ulong))
                {
                    // Nop same
                }
                else if (destinationType == typeof(float))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // implicit
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(float))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Conv_I4);
                }
                else if (destinationType == typeof(uint))
                {
                    ilGenerator.Emit(OpCodes.Conv_U4);
                }
                else if (destinationType == typeof(long))
                {
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(ulong))
                {
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(float))
                {
                    // Nop same
                }
                else if (destinationType == typeof(double))
                {
                    // Implicit
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(double))
            {
                if (destinationType == typeof(byte))
                {
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Conv_I4);
                }
                else if (destinationType == typeof(uint))
                {
                    ilGenerator.Emit(OpCodes.Conv_U4);
                }
                else if (destinationType == typeof(long))
                {
                    ilGenerator.Emit(OpCodes.Conv_I8);
                }
                else if (destinationType == typeof(ulong))
                {
                    ilGenerator.Emit(OpCodes.Conv_U8);
                }
                else if (destinationType == typeof(float))
                {
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // Nop same
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(decimal))
            {
                if (destinationType == typeof(byte))
                {
                    // TODO
                }
                else if (destinationType == typeof(sbyte))
                {
                    // TODO
                }
                else if (destinationType == typeof(char))
                {
                    // TODO
                }
                else if (destinationType == typeof(short))
                {
                    // TODO
                }
                else if (destinationType == typeof(ushort))
                {
                    // TODO
                }
                else if (destinationType == typeof(int))
                {
                    // TODO
                }
                else if (destinationType == typeof(uint))
                {
                    // TODO
                }
                else if (destinationType == typeof(long))
                {
                    // TODO
                }
                else if (destinationType == typeof(ulong))
                {
                    // TODO
                }
                else if (destinationType == typeof(float))
                {
                    // TODO
                }
                else if (destinationType == typeof(double))
                {
                    // TODO
                }
                else if (destinationType == typeof(decimal))
                {
                    // Nop same
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(IntPtr))
            {
                if (destinationType == typeof(byte))
                {
                    // TODO
                }
                else if (destinationType == typeof(sbyte))
                {
                    // TODO
                }
                else if (destinationType == typeof(char))
                {
                    // TODO
                }
                else if (destinationType == typeof(short))
                {
                    // TODO
                }
                else if (destinationType == typeof(ushort))
                {
                    // TODO
                }
                else if (destinationType == typeof(int))
                {
                    // TODO
                }
                else if (destinationType == typeof(uint))
                {
                    // TODO
                }
                else if (destinationType == typeof(long))
                {
                    // TODO
                }
                else if (destinationType == typeof(ulong))
                {
                    // TODO
                }
                else if (destinationType == typeof(float))
                {
                    // TODO
                }
                else if (destinationType == typeof(double))
                {
                    // TODO
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // Nop same
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // TODO
                }
                else
                {
                    return false;
                }
            }
            else if (sourceType == typeof(UIntPtr))
            {
                if (destinationType == typeof(byte))
                {
                    // TODO
                }
                else if (destinationType == typeof(sbyte))
                {
                    // TODO
                }
                else if (destinationType == typeof(char))
                {
                    // TODO
                }
                else if (destinationType == typeof(short))
                {
                    // TODO
                }
                else if (destinationType == typeof(ushort))
                {
                    // TODO
                }
                else if (destinationType == typeof(int))
                {
                    // TODO
                }
                else if (destinationType == typeof(uint))
                {
                    // TODO
                }
                else if (destinationType == typeof(long))
                {
                    // TODO
                }
                else if (destinationType == typeof(ulong))
                {
                    // TODO
                }
                else if (destinationType == typeof(float))
                {
                    // TODO
                }
                else if (destinationType == typeof(double))
                {
                    // TODO
                }
                else if (destinationType == typeof(decimal))
                {
                    // TODO
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // TODO
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // Nop same
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}

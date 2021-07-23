namespace EmitConverter
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ILGeneratorExtensions
    {
        private static MethodInfo ResolveImplicitMethod(Type targetType, Type parameterType, Type returnType)
        {
            return targetType.GetMethods()
                .First(x => x.IsPublic &&
                            x.IsStatic &&
                            x.Name == "op_Implicit" &&
                            x.ReturnParameter?.ParameterType == returnType &&
                            x.GetParameters().Length == 1 &&
                            x.GetParameters()[0].ParameterType == parameterType);
        }

        private static MethodInfo ResolveExplicitMethod(Type targetType, Type parameterType, Type returnType)
        {
            return targetType.GetMethods()
                .First(x => x.IsPublic &&
                            x.IsStatic &&
                            x.Name == "op_Explicit" &&
                            x.ReturnParameter?.ParameterType == returnType &&
                            x.GetParameters().Length == 1 &&
                            x.GetParameters()[0].ParameterType == parameterType);
        }

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
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(byte), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(int), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(uint), typeof(UIntPtr)));
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
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(sbyte), typeof(decimal)));

                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(int), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // implicit
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(char), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(int), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(uint), typeof(UIntPtr)));
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
                    // implicit
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(short), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(int), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // implicit
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(ushort), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(int), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(uint), typeof(UIntPtr)));
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
                    // implicit
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(int), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(int), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // implicit
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(uint), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(uint), typeof(UIntPtr)));
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
                    // implicit
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(long), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // implicit
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(ulong), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(float), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(double), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_I8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Conv_U8);
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(byte)));
                }
                else if (destinationType == typeof(sbyte))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(sbyte)));
                }
                else if (destinationType == typeof(char))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(char)));
                }
                else if (destinationType == typeof(short))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(short)));
                }
                else if (destinationType == typeof(ushort))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(ushort)));
                }
                else if (destinationType == typeof(int))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(int)));
                }
                else if (destinationType == typeof(uint))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(uint)));
                }
                else if (destinationType == typeof(long))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(long)));
                }
                else if (destinationType == typeof(ulong))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(ulong)));
                }
                else if (destinationType == typeof(float))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(float)));
                }
                else if (destinationType == typeof(double))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(double)));
                }
                else if (destinationType == typeof(decimal))
                {
                    // Nop same
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(long)));
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(decimal), typeof(decimal), typeof(ulong)));
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(ulong), typeof(UIntPtr)));
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
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(int)));
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(int)));
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(int)));
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(int)));
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(int)));
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(int)));
                }
                else if (destinationType == typeof(uint))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(int)));
                }
                else if (destinationType == typeof(long))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(long)));
                }
                else if (destinationType == typeof(ulong))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(long)));
                }
                else if (destinationType == typeof(float))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(long)));
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(long)));
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(long)));
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(long), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // Nop same
                }
                else if (destinationType == typeof(UIntPtr))
                {
                    // Special
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(IntPtr), typeof(long)));
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
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
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(uint)));
                    ilGenerator.Emit(OpCodes.Conv_U1);
                }
                else if (destinationType == typeof(sbyte))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(uint)));
                    ilGenerator.Emit(OpCodes.Conv_I1);
                }
                else if (destinationType == typeof(char))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(uint)));
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(short))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(uint)));
                    ilGenerator.Emit(OpCodes.Conv_I2);
                }
                else if (destinationType == typeof(ushort))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(uint)));
                    ilGenerator.Emit(OpCodes.Conv_U2);
                }
                else if (destinationType == typeof(int))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(uint)));
                }
                else if (destinationType == typeof(uint))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(uint)));
                }
                else if (destinationType == typeof(long))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(ulong)));
                }
                else if (destinationType == typeof(ulong))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(ulong)));
                }
                else if (destinationType == typeof(float))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(ulong)));
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                    ilGenerator.Emit(OpCodes.Conv_R4);
                }
                else if (destinationType == typeof(double))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(ulong)));
                    ilGenerator.Emit(OpCodes.Conv_R_Un);
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }
                else if (destinationType == typeof(decimal))
                {
                    // explicit
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(ulong)));
                    ilGenerator.Emit(OpCodes.Call, ResolveImplicitMethod(typeof(decimal), typeof(ulong), typeof(decimal)));
                }
                else if (destinationType == typeof(IntPtr))
                {
                    // Special
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(UIntPtr), typeof(UIntPtr), typeof(ulong)));
                    ilGenerator.Emit(OpCodes.Call, ResolveExplicitMethod(typeof(IntPtr), typeof(long), typeof(IntPtr)));
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

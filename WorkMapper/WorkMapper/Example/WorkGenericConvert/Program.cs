namespace WorkGenericConvert
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart;
    using Smart.Reflection.Emit;

    public static class Program
    {
        public static void Main()
        {
            TestBasic.Test();
            TestNullable.Test();
            TestEnum.Test();
            TestOperator.Test();
        }
    }

    public static class Factory
    {
        public static Func<TSource, TDestination> Create<TSource, TDestination>()
        {
            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);

            var dynamicMethod = new DynamicMethod(string.Empty, destinationType, new[] { typeof(object), sourceType }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Prepare stack
            ilGenerator.Emit(OpCodes.Ldarg_1);

            // 分岐
            if (sourceType.IsClass)
            {
                // Class
                if (!destinationType.IsAssignableFrom(sourceType))
                {
                    if (!EmitConvertOperationForClass(ilGenerator, sourceType, destinationType))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            else if (sourceType.IsNullableType())
            {
                // Nullable
                if (destinationType != sourceType)
                {
                    var local = ilGenerator.DeclareLocal(sourceType);
                    ilGenerator.EmitStloc(local);

                    if (EmitConvertOperationForNullable(ilGenerator, sourceType, destinationType, local))
                    {
                    }
                    else
                    {
                        ilGenerator.EmitLdloca(local);
                        ilGenerator.Emit(OpCodes.Call, sourceType.GetProperty("Value")!.GetMethod!);
                        var underlyingType = Nullable.GetUnderlyingType(sourceType);
                        if (!EmitConvertOperationForNullableValue(ilGenerator, underlyingType, destinationType) &&
                            !EmitConvertPrimitive(ilGenerator, underlyingType, destinationType))
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
            }
            else
            {
                // ValueType
                if (destinationType != sourceType)
                {
                    if (!EmitConvertOperationValueType(ilGenerator, sourceType, destinationType) &&
                        !EmitConvertPrimitive(ilGenerator, sourceType, destinationType))
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<TSource, TDestination>>();
        }

        private static bool EmitConvertOperationForClass(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                Debug.WriteLine("*1A");
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    Debug.WriteLine("*1B");
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationForNullable(ILGenerator ilGenerator, Type sourceType, Type destinationType, LocalBuilder local)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, false);
            if (opMethod is not null)
            {
                Debug.WriteLine("*1A");
                ilGenerator.Emit(OpCodes.Ldloc, local);
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, false);
                if (opMethod is not null)
                {
                    Debug.WriteLine("*1B");
                    ilGenerator.Emit(OpCodes.Ldloc, local);
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationForNullableValue(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                Debug.WriteLine("*1A");
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    Debug.WriteLine("*1B");
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationValueType(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Match operator
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                Debug.WriteLine("*1A");
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    Debug.WriteLine("*1B");
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            var nullableSourceType = typeof(Nullable<>).MakeGenericType(sourceType);
            opMethod = FindConversionOperator(nullableSourceType, destinationType, true);
            if (opMethod is not null)
            {
                Debug.WriteLine("*1C");

                ilGenerator.Emit(OpCodes.Newobj, nullableSourceType.GetConstructor(new[] { sourceType })!);
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }


            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(nullableSourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    Debug.WriteLine("*1D");
                    ilGenerator.Emit(OpCodes.Newobj, nullableSourceType.GetConstructor(new[] { sourceType })!);
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertPrimitive(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // Try primitive covert
            var baseSourceType = sourceType.IsEnum ? Enum.GetUnderlyingType(sourceType) : sourceType;
            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            var baseDestinationType = underlyingDestinationType ?? destinationType;
            baseDestinationType = baseDestinationType.IsEnum ? Enum.GetUnderlyingType(baseDestinationType) : baseDestinationType;

            if ((baseDestinationType != baseSourceType) &&
                !ilGenerator.EmitPrimitiveConvert(baseSourceType, baseDestinationType))
            {
                return false;
            }

            // If destination is nullable, convert to nullable
            if (underlyingDestinationType is not null)
            {
                ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
            }

            Debug.WriteLine("*2");
            return true;
        }

        private static MethodInfo FindConversionOperator(Type sourceType, Type destinationType, bool useSourceMethod)
        {
            if (useSourceMethod)
            {
                var sourceTypeMethod = sourceType.GetMethods().FirstOrDefault(mi =>
                    mi.IsPublic && mi.IsStatic && mi.Name == "op_Implicit" && mi.ReturnType == destinationType);
                if (sourceTypeMethod is not null)
                {
                    return sourceTypeMethod;
                }
            }

            var method = destinationType.GetMethods().FirstOrDefault(mi =>
                mi.IsPublic && mi.IsStatic && mi.Name == "op_Implicit" && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == sourceType);
            if (method is not null)
            {
                return method;
            }

            if (useSourceMethod)
            {
                var sourceTypeMethod = sourceType.GetMethods().FirstOrDefault(mi =>
                    mi.IsPublic && mi.IsStatic && mi.Name == "op_Explicit" && mi.ReturnType == destinationType);
                if (sourceTypeMethod is not null)
                {
                    return sourceTypeMethod;
                }
            }

            return destinationType.GetMethods().FirstOrDefault(mi =>
                mi.IsPublic && mi.IsStatic && mi.Name == "op_Explicit" && mi.GetParameters().Length == 1 && mi.GetParameters()[0].ParameterType == sourceType);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using Smart;
using Smart.Reflection;

namespace WorkEnumerableConverter
{
    class Program
    {
        static void Main()
        {
            //var factory = new Factory();
        }
    }

    public class Factory
    {
        private int typeNo;

        private AssemblyBuilder? assemblyBuilder;

        private ModuleBuilder? moduleBuilder;

        private ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder is null)
                {
                    assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("WorkAssembly"),
                        AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(
                        "WorkModule");
                }

                return moduleBuilder;
            }
        }

        public Func<TS, TD> Create<TS, TD>(object? converter = null)
        {
            var context = TypeResolver.ResolveContext(typeof(TS), typeof(TD));
            if ((context.Source == SourceType.None) || (context.Destination == DestinationType.None))
            {
                throw new InvalidOperationException();
            }

            // Holder
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            if (converter is not null)
            {
                typeBuilder.DefineField("converter", converter.GetType(), FieldAttributes.Public);
            }

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var instance = Activator.CreateInstance(holderType)!;

            if (converter is not null)
            {
                holderType.GetField("converter")!.SetValue(instance, converter);
            }

            // Method
            var dynamicMethod = new DynamicMethod(
                "Converter",
                typeof(TD),
                new[] { holderType, typeof(TS) },
                true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // ----------------------------------------

            var sourceLocal = ilGenerator.DeclareLocal(typeof(TS));
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stloc, sourceLocal);

            switch (context.Source)
            {
                case SourceType.Array:
                    EmitArrayLoop(ilGenerator, context, sourceLocal);
                    break;
                case SourceType.List:
                    EmitListLoop(ilGenerator, context, sourceLocal);
                    break;
                case SourceType.IList:
                    EmitIListLoop(ilGenerator, context, sourceLocal);
                    break;
                case SourceType.IEnumerable:
                    EmitIEnumerableLoop(ilGenerator, context, sourceLocal);
                    break;
            }

            //  Return
            ilGenerator.Emit(OpCodes.Ret);

            // ----------------------------------------

            return (Func<TS, TD>)dynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(typeof(TS), typeof(TD)), instance);
        }

        // TODO test+
        // TODO Convert ***
        // TODO 随時Context部品化

        //--------------------------------------------------------------------------------
        // Array
        //--------------------------------------------------------------------------------

        private static void EmitArrayLoop(ILGenerator ilGenerator, ContainerConvertContext context, LocalBuilder sourceLocal)
        {
            var destinationLocal = ilGenerator.DeclareLocal(context.DestinationType);
            var indexLocal = ilGenerator.DeclareLocal(typeof(int));

            var conditionLabel = ilGenerator.DefineLabel();
            var loopLabel = ilGenerator.DefineLabel();

            // Result
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
                    ilGenerator.Emit(OpCodes.Ldlen);
                    ilGenerator.Emit(OpCodes.Conv_I4);
                    ilGenerator.Emit(OpCodes.Newarr, context.DestinationElementType);
                    ilGenerator.Emit(OpCodes.Stloc, destinationLocal);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
                    ilGenerator.Emit(OpCodes.Ldlen);
                    ilGenerator.Emit(OpCodes.Conv_I4);
                    ilGenerator.Emit(OpCodes.Newobj, context.DestinationType.GetConstructor(new[] { typeof(int) })!);
                    ilGenerator.Emit(OpCodes.Stloc, destinationLocal);
                    break;
            }

            // Loop
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);
            ilGenerator.Emit(OpCodes.Br_S, conditionLabel);

            // Loop start
            ilGenerator.MarkLabel(loopLabel);

            // Prepare result
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
                    ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
                    break;
            }

            // Get element
            ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldelem, context.SourceElementType);

            // Convert
            EmitConvert(ilGenerator, context);

            // Set element
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Stelem, context.DestinationElementType);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Callvirt, context.DestinationType.GetMethod("Add", new[] { context.DestinationElementType })!);
                    break;
            }

            // Increment
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);

            // Condition
            ilGenerator.MarkLabel(conditionLabel);

            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
            ilGenerator.Emit(OpCodes.Ldlen);
            ilGenerator.Emit(OpCodes.Conv_I4);
            ilGenerator.Emit(OpCodes.Blt_S, loopLabel);

            // Return
            ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
        }

        //--------------------------------------------------------------------------------
        // List
        //--------------------------------------------------------------------------------

        private static void EmitListLoop(ILGenerator ilGenerator, ContainerConvertContext context, LocalBuilder sourceLocal)
        {
            var destinationLocal = ilGenerator.DeclareLocal(context.DestinationType);
            var indexLocal = ilGenerator.DeclareLocal(typeof(int));

            var conditionLabel = ilGenerator.DefineLabel();
            var loopLabel = ilGenerator.DefineLabel();

            var sourceCountMethod = context.SourceType.GetProperty("Count")!.GetMethod!;
            var sourceItemMethod = context.SourceType.GetProperty("Item")!.GetMethod!;

            // Result
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
                    ilGenerator.Emit(OpCodes.Callvirt, sourceCountMethod);
                    ilGenerator.Emit(OpCodes.Newarr, context.DestinationElementType);
                    ilGenerator.Emit(OpCodes.Stloc, destinationLocal);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
                    ilGenerator.Emit(OpCodes.Callvirt, sourceCountMethod);
                    ilGenerator.Emit(OpCodes.Newobj, context.DestinationType.GetConstructor(new[] { typeof(int) })!);
                    ilGenerator.Emit(OpCodes.Stloc, destinationLocal);
                    break;
            }

            // Loop
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);
            ilGenerator.Emit(OpCodes.Br_S, conditionLabel);

            // Loop start
            ilGenerator.MarkLabel(loopLabel);

            // Prepare result
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
                    ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
                    break;
            }

            // Get element
            ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Callvirt, sourceItemMethod);

            // Convert
            EmitConvert(ilGenerator, context);

            // Set element
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Stelem, context.DestinationElementType);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Callvirt, context.DestinationType.GetMethod("Add", new[] { context.DestinationElementType })!);
                    break;
            }

            // Increment
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);

            // Condition
            ilGenerator.MarkLabel(conditionLabel);

            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
            ilGenerator.Emit(OpCodes.Callvirt, sourceCountMethod);
            ilGenerator.Emit(OpCodes.Blt_S, loopLabel);

            // Return
            ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
        }

        //--------------------------------------------------------------------------------
        // IList
        //--------------------------------------------------------------------------------

        private static void EmitIListLoop(ILGenerator ilGenerator, ContainerConvertContext context, LocalBuilder sourceLocal)
        {
            var destinationLocal = ilGenerator.DeclareLocal(context.DestinationType);
            var indexLocal = ilGenerator.DeclareLocal(typeof(int));
            var countLocal = ilGenerator.DeclareLocal(typeof(int));

            var conditionLabel = ilGenerator.DefineLabel();
            var loopLabel = ilGenerator.DefineLabel();

            var sourceCountMethod = context.SourceType.GetProperty("Count")!.GetMethod!;
            var sourceItemMethod = context.SourceType.GetProperty("Item")!.GetMethod!;

            // Count
            if (context.SourceType.IsClass)
            {
                ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
                ilGenerator.Emit(OpCodes.Callvirt, sourceCountMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldloca, sourceLocal);
                ilGenerator.Emit(OpCodes.Call, sourceCountMethod);
            }
            ilGenerator.Emit(OpCodes.Stloc, countLocal);

            // Result
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Ldloc, countLocal);
                    ilGenerator.Emit(OpCodes.Newarr, context.DestinationElementType);
                    ilGenerator.Emit(OpCodes.Stloc, destinationLocal);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Ldloc, countLocal);
                    ilGenerator.Emit(OpCodes.Newobj, context.DestinationType.GetConstructor(new[] { typeof(int) })!);
                    ilGenerator.Emit(OpCodes.Stloc, destinationLocal);
                    break;
            }

            // Loop
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);
            ilGenerator.Emit(OpCodes.Br_S, conditionLabel);

            // Loop start
            ilGenerator.MarkLabel(loopLabel);

            // Prepare result
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
                    ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
                    break;
            }

            // Get element
            if (context.SourceType.IsClass)
            {
                ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
                ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
                ilGenerator.Emit(OpCodes.Callvirt, sourceItemMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldloca, sourceLocal);
                ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
                ilGenerator.Emit(OpCodes.Call, sourceItemMethod);
            }

            // Convert
            EmitConvert(ilGenerator, context);

            // Set element
            switch (context.Destination)
            {
                case DestinationType.Array:
                    ilGenerator.Emit(OpCodes.Stelem, context.DestinationElementType);
                    break;
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Callvirt, context.DestinationType.GetMethod("Add", new[] { context.DestinationElementType })!);
                    break;
            }

            // Increment
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Add);
            ilGenerator.Emit(OpCodes.Stloc, indexLocal);

            // Condition
            ilGenerator.MarkLabel(conditionLabel);

            ilGenerator.Emit(OpCodes.Ldloc, indexLocal);
            ilGenerator.Emit(OpCodes.Ldloc, countLocal);
            ilGenerator.Emit(OpCodes.Blt_S, loopLabel);

            // Return
            ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
        }

        //--------------------------------------------------------------------------------
        // IEnumerable
        //--------------------------------------------------------------------------------

        private static void EmitIEnumerableLoop(ILGenerator ilGenerator, ContainerConvertContext context, LocalBuilder sourceLocal)
        {
            var destinationType = context.Destination == DestinationType.Array ? typeof(List<>).MakeGenericType(context.DestinationElementType) : context.DestinationType;
            var destinationLocal = ilGenerator.DeclareLocal(destinationType);
            var enumeratorType = typeof(IEnumerator<>).MakeGenericType(context.SourceElementType);
            var enumeratorLocal = ilGenerator.DeclareLocal(enumeratorType);

            var conditionLabel = ilGenerator.DefineLabel();
            var loopLabel = ilGenerator.DefineLabel();
            var endFinallyLabel = ilGenerator.DefineLabel();

            var sourceGetEnumeratorMethod = context.SourceType.GetMethod("GetEnumerator", Type.EmptyTypes)!;
            var enumeratorCurrentMethod = enumeratorType.GetProperty("Current")!.GetMethod!;
            var enumeratorMoveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
            var disposeMethod = typeof(IDisposable).GetMethod("Dispose")!;

            // Result
            switch (context.Destination)
            {
                case DestinationType.Array:
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(Type.EmptyTypes)!);
                    ilGenerator.Emit(OpCodes.Stloc, destinationLocal);
                    break;
            }

            // Enumerator
            if (context.SourceType.IsClass)
            {
                ilGenerator.Emit(OpCodes.Ldloc, sourceLocal);
                ilGenerator.Emit(OpCodes.Callvirt, sourceGetEnumeratorMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldloca, sourceLocal);
                ilGenerator.Emit(OpCodes.Call, sourceGetEnumeratorMethod);
            }
            ilGenerator.Emit(OpCodes.Stloc, enumeratorLocal);

            // Try begin
            var tryLabel = ilGenerator.BeginExceptionBlock();

            // Loop
            ilGenerator.Emit(OpCodes.Br_S, conditionLabel);

            // Loop start
            ilGenerator.MarkLabel(loopLabel);

            // Prepare result
            switch (context.Destination)
            {
                case DestinationType.Array:
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
                    break;
            }

            // Get current
            ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
            ilGenerator.Emit(OpCodes.Callvirt, enumeratorCurrentMethod);

            // Convert
            EmitConvert(ilGenerator, context);

            // Set element
            switch (context.Destination)
            {
                case DestinationType.Array:
                case DestinationType.ListAssignable:
                    ilGenerator.Emit(OpCodes.Callvirt, destinationType.GetMethod("Add", new[] { context.DestinationElementType })!);
                    break;
            }

            // Condition
            ilGenerator.MarkLabel(conditionLabel);
            ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
            ilGenerator.Emit(OpCodes.Callvirt, enumeratorMoveNextMethod);
            ilGenerator.Emit(OpCodes.Brtrue_S, loopLabel);

            // Try end
            ilGenerator.Emit(OpCodes.Leave_S, tryLabel);

            // Finally begin
            ilGenerator.BeginFinallyBlock();

            // Dispose
            ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
            ilGenerator.Emit(OpCodes.Brfalse_S, endFinallyLabel);
            ilGenerator.Emit(OpCodes.Ldloc, enumeratorLocal);
            ilGenerator.Emit(OpCodes.Callvirt, disposeMethod);

            // Finally end
            ilGenerator.MarkLabel(endFinallyLabel);
            ilGenerator.Emit(OpCodes.Endfinally);
            ilGenerator.EndExceptionBlock();

            // Return
            ilGenerator.Emit(OpCodes.Ldloc, destinationLocal);
            if (context.Destination == DestinationType.Array)
            {
                ilGenerator.Emit(OpCodes.Callvirt, destinationType.GetMethod("ToArray")!);
            }
        }

        //--------------------------------------------------------------------------------
        // Converter
        //--------------------------------------------------------------------------------

        private static void EmitConvert(ILGenerator ilGenerator, ContainerConvertContext context)
        {
            if (context.SourceElementType == context.DestinationElementType)
            {
                return;
            }

            if (context.SourceElementType.IsClass)
            {
                // TODO 変換
                // class (null or), Nullable (null or), valueType-call-only
            }
            else if (context.SourceType.IsNullableType())
            {
                // TODO 変換
                // class (null or), Nullable (null or), valueType-call-only
            }
            else
            {
                // TODO 変換
                // class (null or), Nullable (null or), valueType-call-only
            }

        }

        //--------------------------------------------------------------------------------
        // TODO
        //--------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------
        // Convert helper
        //--------------------------------------------------------------------------------

        private static bool EmitConvertOperationForClass(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // TS to TD : Func<TS, TD>
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                // TS to TD? : Func<TS, TD>
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationForNullable(ILGenerator ilGenerator, Type sourceType, Type destinationType, LocalBuilder local)
        {
            // TS? to TD : Func<TS?, TD>
            var opMethod = FindConversionOperator(sourceType, destinationType, false);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Ldloc, local);
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                // TS? to TD? : Func<TS?, TD>
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, false);
                if (opMethod is not null)
                {
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
            // TS? to TD : Func<TS, TD>
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                // TS? to TD? : Func<TS, TD>
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            return false;
        }

        private static bool EmitConvertOperationValueType(ILGenerator ilGenerator, Type sourceType, Type destinationType)
        {
            // TS to TD : Func<TS, TD>
            var opMethod = FindConversionOperator(sourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            // TS to TD? : Func<TS, TD>
            var underlyingDestinationType = Nullable.GetUnderlyingType(destinationType);
            if (underlyingDestinationType is not null)
            {
                opMethod = FindConversionOperator(sourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
                    ilGenerator.Emit(OpCodes.Call, opMethod);
                    ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
                    return true;
                }
            }

            // TS to TD : Func<TS?, TD>
            var nullableSourceType = typeof(Nullable<>).MakeGenericType(sourceType);
            opMethod = FindConversionOperator(nullableSourceType, destinationType, true);
            if (opMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Newobj, nullableSourceType.GetConstructor(new[] { sourceType })!);
                ilGenerator.Emit(OpCodes.Call, opMethod);
                return true;
            }

            if (underlyingDestinationType is not null)
            {
                // TS to TD? : Func<TS?, TD>
                opMethod = FindConversionOperator(nullableSourceType, underlyingDestinationType, true);
                if (opMethod is not null)
                {
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

            if (baseDestinationType != baseSourceType)
            {
                var method = PrimitiveConvert.GetMethod(baseSourceType, baseDestinationType);
                if (method is null)
                {
                    return false;
                }

                ilGenerator.Emit(OpCodes.Call, method);
            }

            // If destination is nullable, convert to nullable
            if (underlyingDestinationType is not null)
            {
                ilGenerator.Emit(OpCodes.Newobj, destinationType.GetConstructor(new[] { underlyingDestinationType })!);
            }

            return true;
        }

        //--------------------------------------------------------------------------------
        // Helper
        //--------------------------------------------------------------------------------

        private static MethodInfo? FindConversionOperator(Type sourceType, Type destinationType, bool useSourceMethod)
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

    //--------------------------------------------------------------------------------
    // Misc
    //--------------------------------------------------------------------------------

    public class ContainerConvertContext
    {
        public SourceType Source { get; }
        public Type SourceElementType { get; }
        public Type SourceType { get; }
        public DestinationType Destination { get; }
        public Type DestinationType { get; }
        public Type DestinationElementType { get; }

        public ContainerConvertContext(SourceType source, Type sourceType, Type sourceElementType, DestinationType destination, Type destinationType, Type destinationElementType)
        {
            Source = source;
            SourceType = sourceType;
            SourceElementType = sourceElementType;
            Destination = destination;
            DestinationType = destinationType;
            DestinationElementType = destinationElementType;
        }
    }

    //--------------------------------------------------------------------------------

    public enum SourceType
    {
        None,
        Array,
        List,
        IList,
        IEnumerable
    }

    public enum DestinationType
    {
        None,
        Array,
        ListAssignable
    }

    //--------------------------------------------------------------------------------

    public static class TypeResolver
    {
        public static ContainerConvertContext ResolveContext(Type sourceType, Type destinationType)
        {
            var (source, sourceElementType) = TypeResolver.ResolveSourceType(sourceType);
            var (destination, destinationElementType) = TypeResolver.ResolveDestinationType(destinationType);
            return new(source, sourceType, sourceElementType!, destination, destinationType, destinationElementType!);
        }

        public static (SourceType, Type?) ResolveSourceType(Type type)
        {
            if (type.IsArray)
            {
                return (SourceType.Array, type.GetElementType());
            }

            Type? t = type;
            do
            {
                if (t.IsGenericType && (t.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    return (SourceType.List, t.GetGenericArguments()[0]);
                }

                t = t.BaseType;
            } while (t is not null);

            var listType = type.GetInterfaces().Prepend(type).FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
            if (listType is not null)
            {
                return (SourceType.IList, listType.GetGenericArguments()[0]);
            }

            var enumerableType = type.GetInterfaces().Prepend(type).FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerableType is not null)
            {
                return (SourceType.IEnumerable, enumerableType.GetGenericArguments()[0]);
            }

            return (SourceType.None, null);
        }

        public static (DestinationType, Type?) ResolveDestinationType(Type type)
        {
            if (type.IsArray)
            {
                return (DestinationType.Array, type.GetElementType());
            }

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    return (DestinationType.ListAssignable, type.GetGenericArguments()[0]);
                }

                if (type.IsAssignableFrom(typeof(List<>).MakeGenericType(type.GetGenericArguments()[0])))
                {
                    return (DestinationType.ListAssignable, type.GetGenericArguments()[0]);
                }
            }

            return (DestinationType.None, null);
        }
    }

    public enum ConverterType
    {
        None,
        FuncSource,
        FuncSourceContext,
        Interface,
        InterfaceType
    }

    public sealed class ConverterEntry
    {
        public ConverterType Type { get; }

        public Type SourceType { get; }

        public Type DestinationType { get; }

        public object Value { get; }

        public ConverterEntry(ConverterType type, Type sourceType, Type destinationType, object value)
        {
            Type = type;
            SourceType = sourceType;
            DestinationType = destinationType;
            Value = value;
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using Smart.Reflection.Emit;

namespace ConstBenchmark
{
    public static class Program
    {
        public static void Main()
        {
            //var data = new Data();
            //var action = new Factory().CreateStaticNullableIntFieldSetter(10000);
            //action(data);
            BenchmarkRunner.Run<MapperBenchmark>();
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
            AddColumn(
                StatisticColumn.Mean,
                StatisticColumn.Min,
                StatisticColumn.Max,
                StatisticColumn.P90,
                StatisticColumn.Error,
                StatisticColumn.StdDev);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class MapperBenchmark
    {
        private const int N = 1000;

        [AllowNull]
        private Action<Data> direct1Action;
        [AllowNull]
        private Action<Data> field1Action;
        [AllowNull]
        private Action<Data> staticField1Action;

        [AllowNull]
        private Action<Data> direct10000Action;
        [AllowNull]
        private Action<Data> field10000Action;
        [AllowNull]
        private Action<Data> staticField10000Action;

        [AllowNull]
        private Action<Data> nullableDirectAction;
        [AllowNull]
        private Action<Data> nullableFieldAction;
        [AllowNull]
        private Action<Data> staticNullableFieldAction;

        [AllowNull]
        private Action<Data> nullableDirect1Action;
        [AllowNull]
        private Action<Data> nullableField1Action;
        [AllowNull]
        private Action<Data> staticNullableField1Action;

        [AllowNull]
        private Action<Data> nullableDirect10000Action;
        [AllowNull]
        private Action<Data> nullableField10000Action;
        [AllowNull]
        private Action<Data> staticNullableField10000Action;

        [GlobalSetup]
        public void Setup()
        {
            var factory = new Factory();

            direct1Action = factory.CreateIntDirectSetter(1);
            field1Action = factory.CreateIntFieldSetter(1);
            staticField1Action = factory.CreateStaticIntFieldSetter(1);

            direct10000Action = factory.CreateIntDirectSetter(10000);
            field10000Action = factory.CreateIntFieldSetter(10000);
            staticField10000Action = factory.CreateStaticIntFieldSetter(10000);

            nullableDirectAction = factory.CreateNullableIntDirectSetter(null);
            nullableFieldAction = factory.CreateNullableIntFieldSetter(null);
            staticNullableFieldAction = factory.CreateStaticNullableIntFieldSetter(null);

            nullableDirect1Action = factory.CreateNullableIntDirectSetter(1);
            nullableField1Action = factory.CreateNullableIntFieldSetter(1);
            staticNullableField1Action = factory.CreateStaticNullableIntFieldSetter(1);

            nullableDirect10000Action = factory.CreateNullableIntDirectSetter(10000);
            nullableField10000Action = factory.CreateNullableIntFieldSetter(10000);
            staticNullableField10000Action = factory.CreateStaticNullableIntFieldSetter(10000);
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void Direct1()
        {
            var data = new Data();
            var action = direct1Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void Field1()
        {
            var data = new Data();
            var action = field1Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticField1()
        {
            var data = new Data();
            var action = staticField1Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void Direct10000()
        {
            var data = new Data();
            var action = direct10000Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void Field10000()
        {
            var data = new Data();
            var action = field10000Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticField10000()
        {
            var data = new Data();
            var action = staticField10000Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void NullableDirectNull()
        {
            var data = new Data();
            var action = nullableDirectAction;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void NullableFieldNull()
        {
            var data = new Data();
            var action = nullableFieldAction;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticNullableFieldNull()
        {
            var data = new Data();
            var action = staticNullableFieldAction;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void NullableDirect1()
        {
            var data = new Data();
            var action = nullableDirect1Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void NullableField1()
        {
            var data = new Data();
            var action = nullableField1Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticNullableField1()
        {
            var data = new Data();
            var action = staticNullableField1Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void NullableDirect10000()
        {
            var data = new Data();
            var action = nullableDirect10000Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void NullableField10000()
        {
            var data = new Data();
            var action = nullableField10000Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }

        [Benchmark(OperationsPerInvoke = N)]
        public void StaticNullableField10000()
        {
            var data = new Data();
            var action = staticNullableField10000Action;
            for (var i = 0; i < N; i++)
            {
                action(data);
            }
        }
    }

    public class Factory
    {
        private int no;

        private AssemblyBuilder? assemblyBuilder;

        private ModuleBuilder? moduleBuilder;

        private ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder is null)
                {
                    assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("FactoryAssembly"),
                        AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(
                        "FactoryModule");
                }

                return moduleBuilder;
            }
        }

        public Action<Data> CreateIntFieldSetter(int value)
        {
            var type = typeof(Data);
            var pi = type.GetProperty(nameof(Data.Value))!;

            // Holder
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder{no}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            no++;

            typeBuilder.DefineField("constValue", pi.PropertyType, FieldAttributes.Public);

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            var field = holderType.GetField("constValue")!;
            field.SetValue(holder, value);

            // Method
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new[] { holderType, type }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);
            ilGenerator.Emit(OpCodes.Callvirt, pi.SetMethod!);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<Data>>(holder);
        }

        public Action<Data> CreateStaticIntFieldSetter(int value)
        {
            var type = typeof(Data);
            var pi = type.GetProperty(nameof(Data.Value))!;

            // Holder
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder{no}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            no++;

            typeBuilder.DefineField("constValue", pi.PropertyType, FieldAttributes.Public | FieldAttributes.Static);

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            var field = holderType.GetField("constValue")!;
            field.SetValue(null, value);

            // Method
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new[] { holderType, type }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldsfld, field);
            ilGenerator.Emit(OpCodes.Callvirt, pi.SetMethod!);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<Data>>(holder);
        }

        public Action<Data> CreateNullableIntFieldSetter(int? value)
        {
            var type = typeof(Data);
            var pi = type.GetProperty(nameof(Data.NullableValue))!;

            // Holder
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder{no}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            no++;

            typeBuilder.DefineField("constValue", pi.PropertyType, FieldAttributes.Public);

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            var field = holderType.GetField("constValue")!;
            field.SetValue(holder, value);

            // Method
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new[] { holderType, type }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);
            ilGenerator.Emit(OpCodes.Callvirt, pi.SetMethod!);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<Data>>(holder);
        }

        public Action<Data> CreateStaticNullableIntFieldSetter(int? value)
        {
            var type = typeof(Data);
            var pi = type.GetProperty(nameof(Data.NullableValue))!;

            // Holder
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder{no}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            no++;

            typeBuilder.DefineField("constValue", pi.PropertyType, FieldAttributes.Public | FieldAttributes.Static);

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            var field = holderType.GetField("constValue")!;
            field.SetValue(holder, value);

            // Method
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new[] { holderType, type }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldsfld, field);
            ilGenerator.Emit(OpCodes.Callvirt, pi.SetMethod!);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<Data>>(holder);
        }

        public Action<Data> CreateIntDirectSetter(int value)
        {
            var type = typeof(Data);
            var pi = type.GetProperty(nameof(Data.Value))!;

            // Holder
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder{no}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            no++;

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            // Method
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new[] { holderType, type }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.EmitConstI4(value);
            ilGenerator.Emit(OpCodes.Callvirt, pi.SetMethod!);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<Data>>(holder);
        }

        public Action<Data> CreateNullableIntDirectSetter(int? value)
        {
            var type = typeof(Data);
            var pi = type.GetProperty(nameof(Data.NullableValue))!;

            // Holder
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder{no}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            no++;

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            // Method
            var dynamicMethod = new DynamicMethod(string.Empty, typeof(void), new[] { holderType, type }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var local = ilGenerator.DeclareLocal(pi.PropertyType);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            if (value.HasValue)
            {
                ilGenerator.EmitConstI4(value.Value);
                ilGenerator.EmitValueToNullableType(pi.PropertyType);
            }
            else
            {
                ilGenerator.EmitStackDefaultValue(pi.PropertyType, local);
            }

            ilGenerator.Emit(OpCodes.Callvirt, pi.SetMethod!);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Action<Data>>(holder);
        }
    }

    public static class ILGeneratorExtensions
    {
        public static void EmitConstI4(this ILGenerator ilGenerator, int value)
        {
            if (value == 0)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
            else if (value == 1)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            else if (value == 2)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_2);
            }
            else if (value == 3)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_3);
            }
            else if (value == 4)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_4);
            }
            else if (value == 5)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_5);
            }
            else if (value == 6)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_6);
            }
            else if (value == 7)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_7);
            }
            else if (value == 8)
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_8);
            }
            else if ((value <= 127) && (value >= -128))
            {
                ilGenerator.Emit(OpCodes.Ldc_I4_S, value);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldc_I4, value);
            }
        }

        public static void EmitValueToNullableType(this ILGenerator ilGenerator, Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type)!;
            var nullableCtor = type.GetConstructor(new[] { underlyingType })!;
            ilGenerator.Emit(OpCodes.Newobj, nullableCtor);
        }

        public static void EmitStackDefaultValue(this ILGenerator ilGenerator, Type type, LocalBuilder local)
        {
            if (type.IsValueType)
            {
                ilGenerator.EmitLdloca(local);
                ilGenerator.Emit(OpCodes.Initobj, type);
                ilGenerator.EmitLdloc(local);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
        }    }

    public class Data
    {
        public int Value { get; set; }

        public int? NullableValue { get; set; }
    }
}

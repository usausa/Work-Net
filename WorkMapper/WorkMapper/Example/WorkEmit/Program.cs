using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace WorkEmit
{
    using System;
    using System.Runtime.CompilerServices;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Columns;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    public static class Program
    {
        public static void Main()
        {
            //var factory = new Factory();
            //var mapper = factory.Create(typeof(Data));
            //var typedMapper = (EmitMapper<Data>)mapper;
            //var data = new Data();
            //typedMapper.MapAction(data);
            //var data1 = typedMapper.MapFunc();
            //var data2 = typedMapper.MapFunc2();

            BenchmarkRunner.Run<Benchmark>();
        }
    }

    public class EmitMapper<T>
    {
        [AllowNull]
        public Action<T> MapAction;

        [AllowNull]
        public Func<T> MapFunc;

        [AllowNull]
        public Func<T> MapFunc2;

        // TODO object version ?
    }

    public sealed class Factory
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
                        new AssemblyName("WorkEmitAssembly"),
                        AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(
                        "WorkEmitModule");
                }

                return moduleBuilder;
            }
        }

        public object Create(Type type)
        {
            // Mapper
            var mapper = CreateMapper(type);

            // Holder
            var holder = CreateHolder();
            var mapAction = CreateActionMethod(type, holder);
            var mapFunc = CreateFuncMethod(type, holder);
            var mapFunc2 = CreateFunc2Method(type, holder, mapAction);

            var a = mapAction.CreateDelegate(typeof(Action<>).MakeGenericType(type), holder);
            var f = mapFunc.CreateDelegate(typeof(Func<>).MakeGenericType(type), holder);
            var f2 = mapFunc2.CreateDelegate(typeof(Func<>).MakeGenericType(type), holder);

            mapper.GetType().GetField("MapAction")!.SetValue(mapper, a);
            mapper.GetType().GetField("MapFunc")!.SetValue(mapper, f);
            mapper.GetType().GetField("MapFunc2")!.SetValue(mapper, f2);

            return mapper;
        }

        private DynamicMethod CreateActionMethod(Type type, object holder)
        {
            var property = type.GetProperty("Value")!;

            // Func
            var dynamicMethod = new DynamicMethod("MapAction", typeof(void), new[] { holder.GetType(), type }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Set 1
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Callvirt, property.SetMethod!);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        private DynamicMethod CreateFuncMethod(Type type, object holder)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes)!;
            var property = type.GetProperty("Value")!;

            // Func
            var dynamicMethod = new DynamicMethod("MapFunc", type, new[] { holder.GetType() }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Class new
            ilGenerator.Emit(OpCodes.Newobj, ctor);

            // Set 1
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Callvirt, property.SetMethod!);

            // Return
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        private DynamicMethod CreateFunc2Method(Type type, object holder, DynamicMethod mapAction)
        {
            var ctor = type.GetConstructor(Type.EmptyTypes)!;

            // Func
            var dynamicMethod = new DynamicMethod("MapFunc2", type, new[] { holder.GetType() }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            var ctorLocal = ilGenerator.DeclareLocal(type);

            // Class new
            ilGenerator.Emit(OpCodes.Newobj, ctor);

            ilGenerator.Emit(OpCodes.Stloc_S, ctorLocal);

            // Call map
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldloc_S, ctorLocal);
            ilGenerator.Emit(OpCodes.Call, mapAction);

            // Return
            ilGenerator.Emit(OpCodes.Ldloc_S, ctorLocal);
            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod;
        }

        private object CreateMapper(Type type)
        {
            var mapperType = typeof(EmitMapper<>).MakeGenericType(type);
            return Activator.CreateInstance(mapperType)!;
        }

        private object CreateHolder()
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            // [MEMO] Define field

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            // [MEMO] Set field

            return holder;
        }
    }

    public class Data
    {
        public int Value { get; set; }
    }

    //--------------------------------------------------------------------------------

    public sealed class ManualMapper
    {
        public Action<Data> MapAction;

        public Func<Data> MapFunc;

        public ManualMapper()
        {
            MapAction = Map;
            MapFunc = Map;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Map(Data data)
        {
            data.Value = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Data Map()
        {
            var data = new Data();
            data.Value = 1;
            return data;
        }
    }

    public sealed class ManualMapper2
    {
        public Action<Data> MapAction;

        public Func<Data> MapFunc;

        public ManualMapper2()
        {
            MapAction = Map;
            MapFunc = Map;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Map(Data data)
        {
            data.Value = 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Data Map()
        {
            var data = new Data();
            Map(data);
            return data;
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
    public class Benchmark
    {
        private const int N = 1000;

        private readonly ManualMapper manualMapper = new();
        private readonly ManualMapper2 manualMapper2 = new();
        private readonly EmitMapper<Data> emitMapper = (EmitMapper<Data>)new Factory().Create(typeof(Data));

        [Benchmark(OperationsPerInvoke = N)]
        public Data? ManualAction()
        {
            var mapper = manualMapper;
            Data? data = null;
            for (var i = 0; i < N; i++)
            {
                data = new Data();
                mapper.MapAction(data);
            }

            return data;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public Data? ManualFunc()
        {
            var mapper = manualMapper;
            Data? data = null;
            for (var i = 0; i < N; i++)
            {
                data = mapper.MapFunc();
            }

            return data;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public Data? ManualAction2()
        {
            var mapper = manualMapper2;
            Data? data = null;
            for (var i = 0; i < N; i++)
            {
                data = new Data();
                mapper.MapAction(data);
            }

            return data;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public Data? ManualFunc2()
        {
            var mapper = manualMapper2;
            Data? data = null;
            for (var i = 0; i < N; i++)
            {
                data = mapper.MapFunc();
            }

            return data;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public Data? EmitAction()
        {
            var mapper = emitMapper;
            Data? data = null;
            for (var i = 0; i < N; i++)
            {
                data = new Data();
                mapper.MapAction(data);
            }

            return data;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public Data? EmitFunc()
        {
            var mapper = emitMapper;
            Data? data = null;
            for (var i = 0; i < N; i++)
            {
                data = mapper.MapFunc();
            }

            return data;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public Data? EmitFunc2()
        {
            var mapper = emitMapper;
            Data? data = null;
            for (var i = 0; i < N; i++)
            {
                data = mapper.MapFunc2();
            }

            return data;
        }
    }
}

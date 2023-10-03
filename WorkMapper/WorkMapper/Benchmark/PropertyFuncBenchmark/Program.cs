using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace PropertyFuncBenchmark
{
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

        private Func<Data, int> byProperty;

        private Func<Data, int> byFunc;

        [GlobalSetup]
        public void Setup()
        {
            var factory = new Factory();
            byProperty = factory.CreateByProperty(x => x.Value);
            byFunc = factory.CreateByFunc(x => x.Value);
        }

        [Benchmark]
        public int ByProperty()
        {
            var f = byProperty;
            var data = new Data();
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }

        [Benchmark]
        public int ByFunc()
        {
            var f = byFunc;
            var data = new Data();
            var ret = 0;
            for (var i = 0; i < N; i++)
            {
                ret = f(data);
            }

            return ret;
        }
    }

    public class Data
    {
        public int Value { get; set; }
    }


    public class Factory
    {
        public Func<Data, int> CreateByProperty(Expression<Func<Data, int>> expression)
        {
            var pi = (PropertyInfo)((MemberExpression)expression.Body).Member;

            var holder = CreateHolder();

            var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), new[] { holder.GetType(), typeof(Data) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Callvirt, pi.GetMethod!);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data, int>>();
        }

        public Func<Data, int> CreateByFunc(Expression<Func<Data, int>> expression)
        {
            var func = expression.Compile();

            var holder = CreateHolder();
            var field = holder.GetType().GetField("func")!;
            field.SetValue(holder, func);

            var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), new[] { holder.GetType(), typeof(Data) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, field);

            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(Func<Data, int>).GetMethod("Invoke")!);

            ilGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<Func<Data, int>>(holder);
        }

        private object CreateHolder()
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            typeBuilder.DefineField("func", typeof(Func<Data, int>), FieldAttributes.Public);

            var typeInfo = typeBuilder.CreateTypeInfo()!;
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType)!;

            return holder;
        }

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
    }
}

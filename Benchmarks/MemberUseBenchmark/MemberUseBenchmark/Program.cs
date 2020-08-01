using System;

namespace MemberUseBenchmark
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<MemberBenchmark>();
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.LongRun);
            //AddJob(Job.MediumRun);
            //AddJob(Job.ShortRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class MemberBenchmark
    {
        private Func<int> instanceDelegate;

        private Func<int> staticDelegate;

        [GlobalSetup]
        public void Setup()
        {
            var id = new InstanceDelegate();
            id.Converter = Delegate;
            instanceDelegate = id.Execute;

            StaticDelegate.Converter = Delegate;
            staticDelegate = StaticDelegate.Execute;
        }

        private int Delegate() => 1;

        [Benchmark]
        public int ByInstance() => instanceDelegate();

        [Benchmark]
        public int ByStatic() => staticDelegate();
    }

    public class InstanceDelegate
    {
        public Func<int> Converter;

        public int Execute() => Converter();
    }

    public static class StaticDelegate
    {
        public static Func<int> Converter;

        public static int Execute() => Converter();
    }
}

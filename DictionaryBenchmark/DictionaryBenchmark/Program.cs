using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Smart.Collections.Concurrent;

namespace DictionaryBenchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.Default, MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private readonly ThreadsafeTypeHashArrayMap<object> hashArrayMap = new ThreadsafeTypeHashArrayMap<object>();

        private readonly ThreadsafeTypeHashArrayMapOld<object> hashArrayMapOld = new ThreadsafeTypeHashArrayMapOld<object>();

        private readonly Type key = typeof(Class0);

        [GlobalSetup]
        public void Setup()
        {
            foreach (var type in Classes.Types)
            {
                hashArrayMap.AddIfNotExist(type, type);
                hashArrayMapOld.AddIfNotExist(type, type);
            }
        }

        [Benchmark]
        public object ThreadsafeTypeHashArrayMap()
        {
            return hashArrayMap.TryGetValue(key, out var obj) ? obj : null;
        }

        [Benchmark]
        public object ThreadsafeTypeHashArrayMapOld()
        {
            return hashArrayMapOld.TryGetValue(key, out var obj) ? obj : null;
        }
    }
}

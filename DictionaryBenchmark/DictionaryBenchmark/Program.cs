using System;
using System.Diagnostics;
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
            //// TODO
            //var hashArrayMap = new ThreadsafeTypeHashArrayMap<object>();
            //foreach (var type in Classes.Types)
            //{
            //    if (type == typeof(Class12) || type == typeof(Class13))
            //    {
            //        Debug.WriteLine("--");
            //    }

            //    Debug.Assert(hashArrayMap.TryGetValue(type, out _) == false);
            //    hashArrayMap.AddIfNotExist(type, new object());

            //    //Debug.WriteLine("--");
            //    //hashArrayMap.Dump();

            //    foreach (var type2 in Classes.Types)
            //    {
            //        if (!hashArrayMap.TryGetValue(type2, out _))
            //        {
            //            Debug.WriteLine("--");
            //            hashArrayMap.Dump();
            //            Debug.WriteLine(type2.Name);
            //        }

            //        if (type == type2)
            //        {
            //            break;
            //        }
            //    }

            //    //Debug.WriteLine("--");
            //    //hashArrayMap.Dump();
            //}

            //hashArrayMap.Dump();

            //foreach (var type in Classes.Types)
            //{
            //    if (!hashArrayMap.TryGetValue(type, out _))
            //    {
            //        Debug.Assert(hashArrayMap.TryGetValue(type, out _));
            //    }
            //}

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

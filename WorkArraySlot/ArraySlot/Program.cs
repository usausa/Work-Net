namespace ArraySlot
{
    using System;
    using System.Collections.Generic;

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
            BenchmarkRunner.Run<Benchmark>();
        }

        public static int Calc(int n)
        {
            return ((n >> 7) << 7) + 128;
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
            AddDiagnoser(MemoryDiagnoser.Default);
            AddJob(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private readonly Container container = new Container();

        [GlobalSetup]
        public void GlobalSetup()
        {
            container.InContainerScope(typeof(Class00), () => new Class00());
            container.InContainerScope(typeof(Class01), () => new Class01());
            container.InContainerScope(typeof(Class02), () => new Class02());
            container.InContainerScope(typeof(Class03), () => new Class03());
            container.InContainerScope(typeof(Class04), () => new Class04());
            container.InContainerScope(typeof(Class05), () => new Class05());
            container.InContainerScopeOld(typeof(Class00), () => new Class00());
            container.InContainerScopeOld(typeof(Class01), () => new Class01());
            container.InContainerScopeOld(typeof(Class02), () => new Class02());
            container.InContainerScopeOld(typeof(Class03), () => new Class03());
            container.InContainerScopeOld(typeof(Class04), () => new Class04());
            container.InContainerScopeOld(typeof(Class05), () => new Class05());
        }

        [Benchmark]
        public void ScopeNew()
        {
            for (var i = 0; i < 100; i++)
            {
                container.InitializeSlot();
                container.Get(typeof(Class00));
                container.Get(typeof(Class01));
                container.Get(typeof(Class02));
                container.Get(typeof(Class03));
                container.Get(typeof(Class00));
                container.Get(typeof(Class01));
                container.Get(typeof(Class02));
                container.Get(typeof(Class03));
                container.ClearSlot();
            }
        }

        [Benchmark]
        public void ScopeOld()
        {
            for (var i = 0; i < 100; i++)
            {
                container.InitializeDictionary();
                container.GetOld(typeof(Class00));
                container.GetOld(typeof(Class01));
                container.GetOld(typeof(Class02));
                container.GetOld(typeof(Class03));
                container.GetOld(typeof(Class00));
                container.GetOld(typeof(Class01));
                container.GetOld(typeof(Class02));
                container.GetOld(typeof(Class03));
                container.ClearDictionary();
            }
        }
    }

    public class Class00 { }
    public class Class01 { }
    public class Class02 { }
    public class Class03 { }
    public class Class04 { }
    public class Class05 { }


    public sealed class Container
    {
        private readonly Dictionary<Type, Func<Container, object>> cache = new Dictionary<Type, Func<Container, object>>();

        private readonly Dictionary<Type, Func<Container, Type, object>> cacheOld = new Dictionary<Type, Func<Container, Type, object>>();

        private readonly SlotIndexManager component = new SlotIndexManager();

        private readonly TlsSingleContainerSlotPool pool = new TlsSingleContainerSlotPool();

        public ContainerSlot Slot { get; set; }

        public Dictionary<Type, object> Dictionary { get; set; }

        public void InitializeSlot()
        {
            Slot = pool.Rent();
        }

        public void ClearSlot()
        {
            pool.Return(Slot);
            Slot = null;
        }

        public void InitializeDictionary()
        {
            Dictionary = new Dictionary<Type, object>();
        }

        public void ClearDictionary()
        {
            foreach (var pair in Dictionary)
            {
                (pair.Value as IDisposable)?.Dispose();
            }

            Dictionary = null;
        }

        public void InContainerScope(Type type, Func<object> factory)
        {
            var scope = new ContainerScope(component);
            cache[type] = scope.Create(factory);
        }

        public void InContainerScopeOld(Type type, Func<object> factory)
        {
            var scope = new ContainerScopeOld();
            cacheOld[type] = scope.Create(factory);
        }

        public object Get(Type type) => cache[type](this);

        public object GetOld(Type type) => cacheOld[type](this, type);
    }

    public sealed class ContainerScope
    {
        private readonly int index;

        public ContainerScope(SlotIndexManager component)
        {
            index = component.Acquire();
        }

        public Func<Container, object> Create(Func<object> factory)
        {
            return c => c.Slot.GetOrCreate(index, factory);
        }
    }

    public sealed class ContainerScopeOld
    {
        public Func<Container, Type, object> Create(Func<object> factory)
        {
            return (c, t) =>
            {
                var d = c.Dictionary;
                lock (d)
                {
                    if (!d.TryGetValue(t, out var value))
                    {
                        value = factory;
                        d[t] = value;
                    }

                    return value;
                }
            };
        }
    }

    public sealed class ContainerSlot
    {
        private readonly object sync = new object();

        private object[] entries = new object[8];

        // TODO 分解？ Barrier
        public object GetOrCreate(int index, Func<object> factory)
        {
            lock (sync)
            {
                if (index < entries.Length)
                {
                    var obj = entries[index];
                    if (obj is null)
                    {
                        obj = factory();
                        entries[index] = obj;
                    }

                    return obj;
                }
                else
                {
                    Grow(index);

                    var obj = factory();
                    entries[index] = obj;

                    return obj;
                }
            }
        }

        private void Grow(int index)
        {
            var newEntries = new object[((index >> 5) << 5) + 8];
            Array.Copy(entries, 0, newEntries, 0, entries.Length);
            entries = newEntries;
        }

        public void Clear()
        {
            lock (sync)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    (entries[i] as IDisposable)?.Dispose();
                }

                Array.Clear(entries, 0, entries.Length);
            }
        }
    }

    // TODO other implement

    public sealed class TlsSingleContainerSlotPool
    {
        [ThreadStatic]
        private static ContainerSlot pool;

        public ContainerSlot Rent()
        {
            if (pool is null)
            {
                return new ContainerSlot();
            }

            var rent = pool;
            pool = null;
            return rent;
        }

        public void Return(ContainerSlot slot)
        {
            slot.Clear();
            pool ??= slot;
        }
    }

    public sealed class SlotIndexManager
    {
        private readonly object sync = new object();

        private int index;

        public int Acquire()
        {
            lock (sync)
            {
                return index++;
            }
        }
    }
}

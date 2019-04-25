namespace Smart.Benchmark
{
    using System;
    using System.Reflection;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    using Smart.ComponentModel;

    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }

    public sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.Default, MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.LongRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private static readonly ConstructorInfo Ci0 = typeof(Data0).GetConstructors()[0];
        private static readonly ConstructorInfo Ci1 = typeof(Data1).GetConstructors()[0];
        private static readonly ConstructorInfo Ci2 = typeof(Data2).GetConstructors()[0];

        private static readonly PropertyInfo Pii = typeof(Data).GetProperty(nameof(Data.IntValue));
        private static readonly PropertyInfo Pis = typeof(Data).GetProperty(nameof(Data.StringValue));
        private static readonly PropertyInfo PiHi = typeof(Data).GetProperty(nameof(Data.NotificationIntValue));
        private static readonly PropertyInfo PiHs = typeof(Data).GetProperty(nameof(Data.NotificationStringValue));

        private Func<object> factory0New;
        private Func<object> factory0Old;
        private Func<object, object> factory1New;
        private Func<object, object> factory1Old;
        private Func<object, object, object> factory2New;
        private Func<object, object, object> factory2Old;
        private Func<object[], object> factory2BNew;
        private Func<object[], object> factory2BOld;

        private Func<Data0> typedFactory0New;
        private Func<Data0> typedFactory0Old;
        private Func<string, Data1> typedFactory1New;
        private Func<string, Data1> typedFactory1Old;
        private Func<string, int, Data2> typedFactory2New;
        private Func<string, int, Data2> typedFactory2Old;

        private Func<object, object> getterIntOld;
        private Func<object, object> getterIntNew;
        private Func<object, object> getterStringOld;
        private Func<object, object> getterStringNew;
        private Func<object, object> getterHolderIntOld;
        private Func<object, object> getterHolderIntNew;
        private Func<object, object> getterHolderStringOld;
        private Func<object, object> getterHolderStringNew;

        private Func<Data, int> typedGetterIntOld;
        private Func<Data, int> typedGetterIntNew;
        private Func<Data, string> typedGetterStringOld;
        private Func<Data, string> typedGetterStringNew;
        private Func<Data, int> typedGetterHolderIntOld;
        private Func<Data, int> typedGetterHolderIntNew;
        private Func<Data, string> typedGetterHolderStringOld;
        private Func<Data, string> typedGetterHolderStringNew;

        private Action<object, object> setterIntOld;
        private Action<object, object> setterIntNew;
        private Action<object, object> setterStringOld;
        private Action<object, object> setterStringNew;
        private Action<object, object> setterHolderIntOld;
        private Action<object, object> setterHolderIntNew;
        private Action<object, object> setterHolderStringOld;
        private Action<object, object> setterHolderStringNew;

        private Action<Data, int> typedSetterIntOld;
        private Action<Data, int> typedSetterIntNew;
        private Action<Data, string> typedSetterStringOld;
        private Action<Data, string> typedSetterStringNew;
        private Action<Data, int> typedSetterHolderIntOld;
        private Action<Data, int> typedSetterHolderIntNew;
        private Action<Data, string> typedSetterHolderStringOld;
        private Action<Data, string> typedSetterHolderStringNew;

        private Data data;

        [GlobalSetup]
        public void Setup()
        {
            factory0New = Smart.Reflection.DynamicDelegateFactory.Default.CreateFactory0(Ci0);
            factory1New = Smart.Reflection.DynamicDelegateFactory.Default.CreateFactory1(Ci1);
            factory2New = Smart.Reflection.DynamicDelegateFactory.Default.CreateFactory2(Ci2);
            factory2BNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateFactory(Ci2);

            typedFactory0New = Smart.Reflection.DynamicDelegateFactory.Default.CreateFactory<Data0>();
            typedFactory1New = Smart.Reflection.DynamicDelegateFactory.Default.CreateFactory<string, Data1>();
            typedFactory2New = Smart.Reflection.DynamicDelegateFactory.Default.CreateFactory<string, int, Data2>();

            getterIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter(Pii);
            getterStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter(Pis);
            getterHolderIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter(PiHi);
            getterHolderStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter(PiHs);

            typedGetterIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter<Data, int>(Pii);
            typedGetterStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter<Data, string>(Pis);
            typedGetterHolderIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter<Data, int>(PiHi);
            typedGetterHolderStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateGetter<Data, string>(PiHs);

            setterIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter(Pii);
            setterStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter(Pis);
            setterHolderIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter(PiHi);
            setterHolderStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter(PiHs);

            typedSetterIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter<Data, int>(Pii);
            typedSetterStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter<Data, string>(Pis);
            typedSetterHolderIntNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter<Data, int>(PiHi);
            typedSetterHolderStringNew = Smart.Reflection.DynamicDelegateFactory.Default.CreateSetter<Data, string>(PiHs);

            factory0Old = Smart.Reflection2.DynamicDelegateFactory.Default.CreateFactory0(Ci0);
            factory1Old = Smart.Reflection2.DynamicDelegateFactory.Default.CreateFactory1(Ci1);
            factory2Old = Smart.Reflection2.DynamicDelegateFactory.Default.CreateFactory2(Ci2);
            factory2BOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateFactory(Ci2);

            typedFactory0Old = Smart.Reflection2.DynamicDelegateFactory.Default.CreateFactory<Data0>();
            typedFactory1Old = Smart.Reflection2.DynamicDelegateFactory.Default.CreateFactory<string, Data1>();
            typedFactory2Old = Smart.Reflection2.DynamicDelegateFactory.Default.CreateFactory<string, int, Data2>();

            getterIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter(Pii);
            getterStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter(Pis);
            getterHolderIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter(PiHi);
            getterHolderStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter(PiHs);

            typedGetterIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter<Data, int>(Pii);
            typedGetterStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter<Data, string>(Pis);
            typedGetterHolderIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter<Data, int>(PiHi);
            typedGetterHolderStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateGetter<Data, string>(PiHs);

            setterIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter(Pii);
            setterStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter(Pis);
            setterHolderIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter(PiHi);
            setterHolderStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter(PiHs);

            typedSetterIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter<Data, int>(Pii);
            typedSetterStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter<Data, string>(Pis);
            typedSetterHolderIntOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter<Data, int>(PiHi);
            typedSetterHolderStringOld = Smart.Reflection2.DynamicDelegateFactory.Default.CreateSetter<Data, string>(PiHs);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            data = new Data();
        }

        // Factory

        [Benchmark]
        public object Factory0New() => factory0New();
        [Benchmark]
        public object Factory1New() => factory1New(string.Empty);
        [Benchmark]
        public object Factory2New() => factory2New(string.Empty, 0);
        [Benchmark]
        public object Factory2BNew() => factory2BNew(new object[] { string.Empty, 0 });

        [Benchmark]
        public object Factory0Old() => factory0Old();
        [Benchmark]
        public object Factory1Old() => factory1Old(string.Empty);
        [Benchmark]
        public object Factory2Old() => factory2Old(string.Empty, 0);
        [Benchmark]
        public object Factory2BOld() => factory2BOld(new object[] { string.Empty, 0 });

        // TypedFactory

        [Benchmark]
        public Data0 TypedFactory0New() => typedFactory0New();
        [Benchmark]
        public Data1 TypedFactory1New() => typedFactory1New(string.Empty);
        [Benchmark]
        public Data2 TypedFactory2New() => typedFactory2New(string.Empty, 0);

        [Benchmark]
        public Data0 TypedFactory0Old() => typedFactory0Old();
        [Benchmark]
        public Data1 TypedFactory1Old() => typedFactory1Old(string.Empty);
        [Benchmark]
        public Data2 TypedFactory2Old() => typedFactory2Old(string.Empty, 0);

        // Getter

        [Benchmark]
        public object GetterIntNew() => getterIntNew(data);
        [Benchmark]
        public object GetterStringNew() => getterStringNew(data);
        [Benchmark]
        public object GetterHolderIntNew() => getterHolderIntNew(data);
        [Benchmark]
        public object GetterHolderStringNew() => getterHolderStringNew(data);

        [Benchmark]
        public object GetterIntOld() => getterIntOld(data);
        [Benchmark]
        public object GetterStringOld() => getterStringOld(data);
        [Benchmark]
        public object GetterHolderIntOld() => getterHolderIntOld(data);
        [Benchmark]
        public object GetterHolderStringOld() => getterHolderStringOld(data);

        // TypedGetter

        [Benchmark]
        public int TypedGetterIntNew() => typedGetterIntNew(data);
        [Benchmark]
        public string TypedGetterStringNew() => typedGetterStringNew(data);
        [Benchmark]
        public int TypedGetterHolderIntNew() => typedGetterHolderIntNew(data);
        [Benchmark]
        public string TypedGetterHolderStringNew() => typedGetterHolderStringNew(data);

        [Benchmark]
        public int TypedGetterIntOld() => typedGetterIntOld(data);
        [Benchmark]
        public string TypedGetterStringOld() => typedGetterStringOld(data);
        [Benchmark]
        public int TypedGetterHolderIntOld() => typedGetterHolderIntOld(data);
        [Benchmark]
        public string TypedGetterHolderStringOld() => typedGetterHolderStringOld(data);

        // Setter

        [Benchmark]
        public void SetterIntNew() => setterIntNew(data, 0);
        [Benchmark]
        public void SetterStringNew() => setterStringNew(data, string.Empty);
        [Benchmark]
        public void SetterHolderIntNew() => setterHolderIntNew(data, 0);
        [Benchmark]
        public void SetterHolderStringNew() => setterHolderStringNew(data, string.Empty);

        [Benchmark]
        public void SetterIntOld() => setterIntOld(data, 0);
        [Benchmark]
        public void SetterStringOld() => setterStringOld(data, string.Empty);
        [Benchmark]
        public void SetterHolderIntOld() => setterHolderIntOld(data, 0);
        [Benchmark]
        public void SetterHolderStringOld() => setterHolderStringOld(data, string.Empty);

        // TypedSetter

        [Benchmark]
        public void TypedSetterIntNew() => typedSetterIntNew(data, 0);
        [Benchmark]
        public void TypedSetterStringNew() => typedSetterStringNew(data, string.Empty);
        [Benchmark]
        public void TypedSetterHolderIntNew() => typedSetterHolderIntNew(data, 0);
        [Benchmark]
        public void TypedSetterHolderStringNew() => typedSetterHolderStringNew(data, string.Empty);

        [Benchmark]
        public void TypedSetterIntOld() => typedSetterIntOld(data, 0);
        [Benchmark]
        public void TypedSetterStringOld() => typedSetterStringOld(data, string.Empty);
        [Benchmark]
        public void TypedSetterHolderIntOld() => typedSetterHolderIntOld(data, 0);
        [Benchmark]
        public void TypedSetterHolderStringOld() => typedSetterHolderStringOld(data, string.Empty);
    }

    public class Data
    {
        public int IntValue { get; set; }

        public string StringValue { get; set; }

        public IValueHolder<string> NotificationStringValue { get; } = new NotificationValue<string>();

        public IValueHolder<int> NotificationIntValue { get; } = new NotificationValue<int>();
    }

    public class Data0
    {
    }

    public class Data1
    {
        public string Param1 { get; }

        public Data1(string arg1)
        {
            Param1 = arg1;
        }
    }

    public class Data2
    {
        public string Param1 { get; }

        public int Param2 { get; }

        public Data2(string arg1, int arg2)
        {
            Param1 = arg1;
            Param2 = arg2;
        }
    }
}

using System;

using WorkMapper;

namespace Benchmark
{
    using AutoMapper;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Columns;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    using Nelibur.ObjectMapper;

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

        private readonly SimpleSource simpleSource = new();

        private readonly MixedSource mixedSource = new();

        private IMapper autoMapper;

        private readonly ActionMapperFactory instantActionMapperFactory = new();    // Boxed

        private readonly ActionMapperFactory rawActionMapperFactory = new();

        private IActionMapper<SimpleSource, SimpleDestination> instantSimpleMapper;

        private IActionMapper<SimpleSource, SimpleDestination> rawSimpleMapper;

        private WorkMapper.Mapper smartMapper;

        private Func<SimpleSource, SimpleDestination> smartSimpleMapper;

        [GlobalSetup]
        public void Setup()
        {
            // AutoMapper
            var autoMapperConfig = new MapperConfiguration(c =>
            {
                c.CreateMap<SimpleSource, SimpleDestination>();
                c.CreateMap<MixedSource, MixedDestination>();
            });
            autoMapper = autoMapperConfig.CreateMapper();

            // TinyMapper
            TinyMapper.Bind<SimpleSource, SimpleDestination>();
            TinyMapper.Bind<MixedSource, MixedDestination>();

            // Action based
            instantSimpleMapper = InstantMapperFactory.Create<SimpleSource, SimpleDestination>();
            instantActionMapperFactory.AddMapper(typeof(SimpleSource), typeof(SimpleDestination), instantSimpleMapper);
            instantActionMapperFactory.AddMapper(typeof(MixedSource), typeof(MixedDestination), InstantMapperFactory.Create<MixedSource, MixedDestination>());

            rawSimpleMapper = RawMapperFactory.CreateSimpleMapper();
            rawActionMapperFactory.AddMapper(typeof(SimpleSource), typeof(SimpleDestination), rawSimpleMapper);
            rawActionMapperFactory.AddMapper(typeof(MixedSource), typeof(MixedDestination), RawMapperFactory.CreateMixedMapper());

            // Smart
            var smartConfig = new MapperConfig()
                .AddDefaultMapper();
            smartConfig.CreateMap<SimpleSource, SimpleDestination>();
            smartConfig.CreateMap<MixedSource, MixedDestination>();
            smartMapper = smartConfig.ToMapper();

            smartSimpleMapper = smartMapper.GetMapperFunc<SimpleSource, SimpleDestination>();
        }

        //--------------------------------------------------------------------------------
        // Simple
        //--------------------------------------------------------------------------------

        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleAutoMapper()
        {
            var m = autoMapper;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<SimpleDestination>(simpleSource);
            }
            return ret;
        }


        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleAutoMapper2()
        {
            var m = autoMapper;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<SimpleSource, SimpleDestination>(simpleSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleTinyMapper()
        {
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = TinyMapper.Map<SimpleDestination>(simpleSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleInstantMapper()
        {
            var m = instantActionMapperFactory;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<SimpleDestination>(simpleSource);
            }
            return ret;
        }

        // Near Tiny
        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleRawMapper()
        {
            var m = rawActionMapperFactory;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<SimpleDestination>(simpleSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleSmartMapper()
        {
            var m = smartMapper;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<SimpleSource, SimpleDestination>(simpleSource);
            }
            return ret;
        }

        //--------------------------------------------------------------------------------
        // Without lookup
        //--------------------------------------------------------------------------------

        // Slow (object based/boxed & delegate getter/setter)
        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleInstantMapperWoLookup()
        {
            var m = instantSimpleMapper;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map(simpleSource);
            }
            return ret;
        }

        // Fast (No loop, No boxed)
        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleRawMapperWoLookup()
        {
            var m = rawSimpleMapper;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map(simpleSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleSmartMapperWoLookup()
        {
            var m = smartSimpleMapper;
            var ret = default(SimpleDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m(simpleSource);
            }
            return ret;
        }

        // Max
        [Benchmark(OperationsPerInvoke = N)]
        public SimpleDestination SimpleHand()
        {
            var ret = default(SimpleDestination);

            // Without Lookup
            for (var i = 0; i < N; i++)
            {
                ret = new SimpleDestination
                {
                    Value1 = simpleSource.Value1,
                    Value2 = simpleSource.Value2,
                    Value3 = simpleSource.Value3,
                    Value4 = simpleSource.Value4,
                    Value5 = simpleSource.Value5,
                    Value6 = simpleSource.Value6,
                    Value7 = simpleSource.Value7,
                    Value8 = simpleSource.Value8
                };
            }

            return ret;
        }

        //--------------------------------------------------------------------------------
        // Mixed
        //--------------------------------------------------------------------------------

        [Benchmark(OperationsPerInvoke = N)]
        public MixedDestination MixedAutoMapper()
        {
            var m = autoMapper;
            var ret = default(MixedDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<MixedDestination>(mixedSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public MixedDestination MixedTinyMapper()
        {
            var ret = default(MixedDestination);
            for (var i = 0; i < N; i++)
            {
                ret = TinyMapper.Map<MixedDestination>(mixedSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public MixedDestination MixedInstantMapper()
        {
            var m = instantActionMapperFactory;
            var ret = default(MixedDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<MixedDestination>(mixedSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public MixedDestination MixedRawMapper()
        {
            var m = rawActionMapperFactory;
            var ret = default(MixedDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<MixedDestination>(mixedSource);
            }
            return ret;
        }

        [Benchmark(OperationsPerInvoke = N)]
        public MixedDestination MixedSmartMapper()
        {
            var m = smartMapper;
            var ret = default(MixedDestination);
            for (var i = 0; i < N; i++)
            {
                ret = m.Map<MixedSource, MixedDestination>(mixedSource);
            }
            return ret;
        }

        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        // TODO 2
        // TODO +interface 1
    }
}

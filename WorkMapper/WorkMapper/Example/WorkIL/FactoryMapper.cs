namespace WorkIL
{
    using System;

    using WorkMapper;
    using WorkMapper.Functions;

    public sealed class FactoryMapper
    {
        public Destination MapFunc(Source source)
        {
            var destination = new Destination();
            destination.Value = source.Value;
            destination.ClassValue = source.ClassValue;
            destination.StructValue = source.StructValue;
            return destination;
        }

        public void MapAction(Source source, Destination destination)
        {
            destination.Value = source.Value;
            destination.ClassValue = source.ClassValue;
            destination.StructValue = source.StructValue;
        }
    }

    public sealed class FactoryMapper1
    {
        public INestedMapper mapper;

        public IServiceProvider factory;

        public Action<Source, Destination> beforeMap1;
        public Action<Source, Destination, ResolutionContext> beforeMap2;
        public IMappingAction<Source, Destination> beforeMap3;

        public Action<Source, Destination> afterMap1;
        public Action<Source, Destination, ResolutionContext> afterMap2;
        public IMappingAction<Source, Destination> afterMap3;

        public Destination Map(Source source, string parameter)
        {
            var context = new ResolutionContext(parameter, mapper);
            var destination = (Destination)factory.GetService(typeof(Destination))!;

            beforeMap1(source, destination);
            beforeMap2(source, destination, context);
            beforeMap3.Process(source, destination, context);

            // TODO

            afterMap1(source, destination);
            afterMap2(source, destination, context);
            afterMap3.Process(source, destination, context);

            return destination;
        }
    }

    public sealed class FactoryMapper2
    {
        public Func<Destination> factory;

        public Destination Map(Source source)
        {
            var data = factory();
            return data;
        }
    }

    public sealed class FactoryMapper3
    {
        public Func<Source, Destination> factory;

        public Destination Map(Source source)
        {
            var data = factory(source);
            return data;
        }
    }

    public sealed class FactoryMapper4
    {
        public Func<ResolutionContext, Destination> factory;

        public INestedMapper mapper;

        public Destination Map(Source source)
        {
            var context = new ResolutionContext(null, mapper);
            var data = factory(context);
            return data;
        }
    }

    public sealed class FactoryMapper5
    {
        public Func<Source, ResolutionContext, Destination> factory;

        public INestedMapper mapper;

        public Destination Map(Source source)
        {
            var context = new ResolutionContext(null, mapper);
            var data = factory(source, context);
            return data;
        }
    }

    public sealed class FactoryMapper6
    {
        public IObjectFactory<Source, Destination> factory;

        public INestedMapper mapper;

        public Destination Map(Source source)
        {
            var context = new ResolutionContext(null, mapper);
            var data = factory.Create(source, context);
            return data;
        }
    }
}

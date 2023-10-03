namespace WorkIL
{
    using System;

    using WorkMapper;
    using WorkMapper.Functions;

    public sealed class StructMapper
    {
        public StructDestination MapFunc(StructSource source)
        {
            var destination = new StructDestination();
            destination.Value = source.Value;
            destination.Value2 = source.Value2;
            destination.StructValue = source.StructValue;
            return destination;
        }

        public void MapAction(StructSource source, StructDestination destination)
        {
            destination.Value = source.Value;
            destination.Value2 = source.Value2;
            destination.StructValue = source.StructValue;
        }
    }

    public sealed class StructConditionMapper
    {
        public Func<StructSource, bool> condition;

        public StructDestination MapFunc(StructSource source)
        {
            var destination = new StructDestination();
            if (condition(source))
            {
                destination.Value = source.Value;
            }
            destination.Value2 = source.Value2;
            return destination;
        }

        public void MapAction(StructSource source, StructDestination destination)
        {
            destination.Value = source.Value;
            if (condition(source))
            {
                destination.Value2 = source.Value2;
            }
        }

        public StructDestination MapFunc2(StructSource source)
        {
            var destination = new StructDestination();
            if (condition(source))
            {
                destination.Value = source.Value;
            }
            if (condition(source))
            {
                destination.Value2 = source.Value2;
            }
            return destination;
        }
    }

    public sealed class StructMapeer1
    {
        public INestedMapper mapper;

        public IServiceProvider factory;

        public Action<StructSource, StructDestination> beforeMap1;

        public Action<StructSource, StructDestination> afterMap1;

        public StructDestination Map(StructSource source)
        {
            var destination = (StructDestination)factory.GetService(typeof(StructDestination))!;

            beforeMap1(source, destination);

            // TODO

            afterMap1(source, destination);

            return destination;
        }
    }

    public sealed class StructMapeer2
    {
        public IObjectFactory<StructSource, StructDestination> factory;

        public INestedMapper mapper;

        public StructDestination Map(StructSource source, string parameter)
        {
            var context = new ResolutionContext(parameter, mapper);
            var data = factory.Create(source, context);
            return data;
        }
    }
}

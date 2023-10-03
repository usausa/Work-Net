using System;

using WorkMapper;
using WorkMapper.Functions;

namespace WorkIL
{
    public sealed class ConditionMapper1
    {
        public Func<Source, bool> condition;

        public void Map(Source source, Destination destination)
        {
            if (condition(source))
            {
                destination.Value = source.Value;
            }
        }
    }

    public sealed class ConditionMapper2
    {
        public Func<Source, ResolutionContext, bool> condition;

        public INestedMapper mapper;

        public void Map(Source source, Destination destination)
        {
            var context = new ResolutionContext(null, mapper);

            if (condition(source, context))
            {
                destination.Value = source.Value;
            }
        }
    }

    public sealed class ConditionMapper3
    {
        public Func<Source, Destination, ResolutionContext, bool> condition;

        public INestedMapper mapper;

        public void Map(Source source, Destination destination)
        {
            var context = new ResolutionContext(null, mapper);

            if (condition(source, destination, context))
            {
                destination.Value = source.Value;
            }
        }
    }

    public sealed class ConditionMapper4
    {
        public IMemberCondition<Source, Destination> condition;

        public INestedMapper mapper;

        public void Map(Source source, Destination destination)
        {
            var context = new ResolutionContext(null, mapper);

            if (condition.Eval(source, destination, context))
            {
                destination.Value = source.Value;
            }
        }
    }
}

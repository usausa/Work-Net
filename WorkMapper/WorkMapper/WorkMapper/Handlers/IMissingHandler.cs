using System;

using WorkMapper.Options;

namespace WorkMapper.Handlers
{
    public interface IMissingHandler
    {
        int Priority { get; }

        MappingOption? Handle(Type sourceType, Type destinationType);
    }
}

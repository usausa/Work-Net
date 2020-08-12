namespace Smart.Resolver.Processors
{
    using System;

    public interface IProcessor
    {
        int Order { get; }

        Action<object> CreateProcessor(IResolver resolver, Type type);
    }
}

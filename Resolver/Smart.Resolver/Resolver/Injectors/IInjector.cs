namespace Smart.Resolver.Injectors
{
    using System;

    using Smart.Resolver.Bindings;

    public interface IInjector
    {
        Action<object> CreateInjector(IResolver resolver, Type type, IBinding binding);
    }
}

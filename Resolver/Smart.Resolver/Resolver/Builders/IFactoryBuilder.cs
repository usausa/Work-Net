namespace Smart.Resolver.Builders
{
    using System;
    using System.Reflection;

    public interface IFactoryBuilder
    {
        Func<object> CreateFactory(ConstructorInfo ci, Func<object>[] factories, Action<object>[] actions);

        Func<object> CreateArrayFactory(Type type, Func<object>[] factories);
    }
}

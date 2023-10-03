namespace WorkMapper.Components
{
    using System;

    public interface IConverterResolver
    {
        Func<TSource, TDestination> Resolve<TSource, TDestination>();
    }
}

using System;

namespace WorkMapper.Components
{
    public interface IFactoryResolver
    {
        Func<T> Resolve<T>();
    }
}

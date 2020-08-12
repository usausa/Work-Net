namespace Smart.Resolver.Scopes
{
    using System;

    using Smart.ComponentModel;
    using Smart.Resolver.Bindings;

    public sealed class ContainerScope : IScope
    {
        public IScope Copy(IComponentContainer components)
        {
            return this;
        }

        public Func<object> Create(IResolver resolver, IBinding binding, Func<object> factory)
        {
            if (resolver is IContainer container)
            {
                return () => container.Create(binding, factory);
            }
            else
            {
                return factory;
            }
        }
    }
}

namespace Smart.Resolver.Scopes
{
    using System;
    using Smart.ComponentModel;

    public sealed class ThreadLocalScope : IScope
    {
        public ThreadLocalScope(IComponentContainer components)
        {
            // TODO
        }

        public IScope Copy(IComponentContainer components)
        {
            return new SingletonScope(components);
        }

        public Func<object> Create(Func<object> factory)
        {
            throw new NotImplementedException();
        }
    }
}

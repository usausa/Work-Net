namespace Smart.Resolver.Scopes
{
    using System;

    using Smart.ComponentModel;
    using Smart.Resolver.Components;

    public sealed class SingletonScope : IScope, IDisposable
    {
        private object value;

        private Func<object> objectFactory;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public SingletonScope(IComponentContainer container)
        {
            container.Get<DisposableStorage>().Add(this);
        }

        public void Dispose()
        {
            (value as IDisposable)?.Dispose();
        }

        public IScope Copy(IComponentContainer components)
        {
            return new SingletonScope(components);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<object> Create(Func<object> factory)
        {
            if (objectFactory is null)
            {
                value = factory();
                objectFactory = () => value;
            }

            return objectFactory;
        }
    }
}

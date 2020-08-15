namespace Smart.Resolver.Contexts
{
    using System;

    public interface IContext : IDisposable
    {
        void Switch();
    }
}

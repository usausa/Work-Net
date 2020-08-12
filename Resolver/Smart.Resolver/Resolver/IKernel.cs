namespace Smart.Resolver
{
    using System;

    using Smart.Resolver.Constraints;

    public interface IKernel : IResolver
    {
        bool TryResolveFactory(Type type, IConstraint constraint, out Func<object> factory);

        bool TryResolveFactories(Type type, IConstraint constraint, out Func<object>[] factories);
    }
}

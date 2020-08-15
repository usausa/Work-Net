namespace Smart.Resolver
{
    using System;
    using System.Runtime.CompilerServices;

    using Smart.Resolver.Constraints;
    using Smart.Resolver.Contexts;
    using Smart.Resolver.Scopes;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
    public static class SmartResolverExtensions
    {
        // Child
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IResolver CreateChildResolver(this SmartResolver resolver) =>
            resolver.CreateChildResolver(new ThreadLocalChildContext());

        // CanGet

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanGet<T>(this SmartResolver resolver, string name) =>
            resolver.CanGet<T>(new NameConstraint(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanGet(this SmartResolver resolver, Type type, string name) =>
            resolver.CanGet(type, new NameConstraint(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanGet<T>(this IResolver resolver, string name) =>
            resolver.CanGet<T>(new NameConstraint(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanGet(this IResolver resolver, Type type, string name) =>
            resolver.CanGet(type, new NameConstraint(name));

        // TryGet

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<T>(this SmartResolver resolver, string name, out T obj) =>
            resolver.TryGet(new NameConstraint(name), out obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(this SmartResolver resolver, Type type, string name, out object obj) =>
            resolver.TryGet(type, new NameConstraint(name), out obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<T>(this IResolver resolver, string name, out T obj) =>
            resolver.TryGet(new NameConstraint(name), out obj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet(this IResolver resolver, Type type, string name, out object obj) =>
            resolver.TryGet(type, new NameConstraint(name), out obj);

        // Get

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(this SmartResolver resolver, string name) =>
            resolver.Get<T>(new NameConstraint(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Get(this SmartResolver resolver, Type type, string name) =>
            resolver.Get(type, new NameConstraint(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(this IResolver resolver, string name) =>
            resolver.Get<T>(new NameConstraint(name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Get(this IResolver resolver, Type type, string name) =>
            resolver.Get(type, new NameConstraint(name));
    }
}
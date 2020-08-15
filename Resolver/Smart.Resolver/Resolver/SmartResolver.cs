namespace Smart.Resolver
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    using Smart.Collections.Concurrent;
    using Smart.ComponentModel;
    using Smart.Resolver.Bindings;
    using Smart.Resolver.Constraints;
    using Smart.Resolver.Contexts;
    using Smart.Resolver.Handlers;
    using Smart.Resolver.Injectors;
    using Smart.Resolver.Providers;

    public sealed class SmartResolver : IKernel
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Performance")]
        private sealed class FactoryEntry
        {
            public readonly bool CanGet;

            public readonly Func<object> Single;

            public readonly Func<object>[] Multiple;

            public FactoryEntry(bool canGet, Func<object> single, Func<object>[] multiple)
            {
                CanGet = canGet;
                Single = single;
                Multiple = multiple;
            }
        }

        private readonly ThreadsafeTypeHashArrayMap<FactoryEntry> factoriesCache = new ThreadsafeTypeHashArrayMap<FactoryEntry>(128);

        private readonly TypeConstraintHashArray<FactoryEntry> factoriesCacheWithConstraint = new TypeConstraintHashArray<FactoryEntry>();

        private readonly ThreadsafeTypeHashArrayMap<Action<object>[]> injectorsCache = new ThreadsafeTypeHashArrayMap<Action<object>[]>();

        private readonly object sync = new object();

        private readonly BindingTable table = new BindingTable();

        private readonly IInjector[] injectors;

        private readonly IMissingHandler[] handlers;

        private int disposed;

        public IComponentContainer Components { get; }

        public SmartResolver(IResolverConfig config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            Components = config.CreateComponentContainer();

            injectors = Components.GetAll<IInjector>().ToArray();
            handlers = Components.GetAll<IMissingHandler>().ToArray();

            foreach (var group in config.CreateBindings(Components).GroupBy(b => b.Type))
            {
                table.Add(group.Key, group.ToArray());
            }

            table.Add(typeof(IResolver), new IBinding[] { new Binding(typeof(IResolver), new ConstantProvider(this), null, null, null, null) });
            table.Add(typeof(SmartResolver), new IBinding[] { new Binding(typeof(SmartResolver), new ConstantProvider(this), null, null, null, null) });
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1)
            {
                return;
            }

            Components.Dispose();
        }

        // ------------------------------------------------------------
        // ObjectFactory
        // ------------------------------------------------------------

        bool IKernel.TryResolveFactory(Type type, IConstraint constraint, out Func<object> factory)
        {
            var entry = constraint is null ? FindFactoryEntry(type) : FindFactoryEntry(type, constraint);
            factory = entry.Single;
            return entry.CanGet;
        }

        bool IKernel.TryResolveFactories(Type type, IConstraint constraint, out Func<object>[] factories)
        {
            var entry = constraint is null ? FindFactoryEntry(type) : FindFactoryEntry(type, constraint);
            factories = entry.Multiple;
            return entry.CanGet;
        }

        // ------------------------------------------------------------
        // Resolver
        // ------------------------------------------------------------

        // CanGet

        public bool CanGet<T>() =>
            FindFactoryEntry(typeof(T)).CanGet;

        public bool CanGet<T>(IConstraint constraint) =>
            FindFactoryEntry(typeof(T), constraint).CanGet;

        public bool CanGet(Type type) =>
            FindFactoryEntry(type).CanGet;

        public bool CanGet(Type type, IConstraint constraint) =>
            FindFactoryEntry(type, constraint).CanGet;

        // TryGet

        public bool TryGet<T>(out T obj)
        {
            var entry = FindFactoryEntry(typeof(T));
            obj = entry.CanGet ? (T)entry.Single() : default;
            return entry.CanGet;
        }

        public bool TryGet<T>(IConstraint constraint, out T obj)
        {
            var entry = FindFactoryEntry(typeof(T), constraint);
            obj = entry.CanGet ? (T)entry.Single() : default;
            return entry.CanGet;
        }

        public bool TryGet(Type type, out object obj)
        {
            var entry = FindFactoryEntry(type);
            obj = entry.CanGet ? entry.Single() : default;
            return entry.CanGet;
        }

        public bool TryGet(Type type, IConstraint constraint, out object obj)
        {
            var entry = FindFactoryEntry(type, constraint);
            obj = entry.CanGet ? entry.Single() : default;
            return entry.CanGet;
        }

        // Get

        public T Get<T>() =>
            (T)FindFactoryEntry(typeof(T)).Single();

        public T Get<T>(IConstraint constraint) =>
            (T)FindFactoryEntry(typeof(T), constraint).Single();

        public object Get(Type type) =>
            FindFactoryEntry(type).Single();

        public object Get(Type type, IConstraint constraint) =>
            FindFactoryEntry(type, constraint).Single();

        // GetAll

        public IEnumerable<T> GetAll<T>() =>
            FindFactoryEntry(typeof(T)).Multiple.Select(x => (T)x());

        public IEnumerable<T> GetAll<T>(IConstraint constraint) =>
            FindFactoryEntry(typeof(T), constraint).Multiple.Select(x => (T)x());

        public IEnumerable<object> GetAll(Type type) =>
            FindFactoryEntry(type).Multiple.Select(x => x());

        public IEnumerable<object> GetAll(Type type, IConstraint constraint) =>
            FindFactoryEntry(type, constraint).Multiple.Select(x => x());

        // ------------------------------------------------------------
        // Binding
        // ------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FactoryEntry FindFactoryEntry(Type type)
        {
            if (!factoriesCache.TryGetValue(type, out var entry))
            {
                entry = factoriesCache.AddIfNotExist(type, t => CreateFactoryEntry(t, null));
            }

            return entry;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FactoryEntry FindFactoryEntry(Type type, IConstraint constraint)
        {
            if (!factoriesCacheWithConstraint.TryGetValue(type, constraint, out var entry))
            {
                entry = factoriesCacheWithConstraint.AddIfNotExist(type, constraint, CreateFactoryEntry);
            }

            return entry;
        }

        private FactoryEntry CreateFactoryEntry(Type type, IConstraint constraint)
        {
            lock (sync)
            {
                var bindings = table.Get(type) ?? handlers.SelectMany(h => h.Handle(Components, table, type));
                if (constraint != null)
                {
                    bindings = bindings.Where(b => constraint.Match(b.Metadata));
                }

                var factories = bindings
                    .Select(b =>
                    {
                        var factory = b.Provider.CreateFactory(this, b);
                        return b.Scope is null ? factory : b.Scope.Create(factory);
                    })
                    .ToArray();

                return new FactoryEntry(
                    factories.Length > 0,
                    factories.Length > 0 ? factories[factories.Length - 1] : () => null,
                    factories);
            }
        }

        // ------------------------------------------------------------
        // Inject
        // ------------------------------------------------------------

        public void Inject(object instance)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            var actions = FindInjectors(instance.GetType());
            for (var i = 0; i < actions.Length; i++)
            {
                actions[i](instance);
            }
        }

        private Action<object>[] FindInjectors(Type type)
        {
            if (!injectorsCache.TryGetValue(type, out var actions))
            {
                actions = injectorsCache.AddIfNotExist(type, CreateInjectors);
            }

            return actions;
        }

        private Action<object>[] CreateInjectors(Type type)
        {
            var binding = new Binding(type);
            return injectors
                .Select(x => x.CreateInjector(this, type, binding))
                .Where(x => x != null)
                .ToArray();
        }

        // ------------------------------------------------------------
        // Scope
        // ------------------------------------------------------------

        public IResolver CreateChildResolver(IContext context)
        {
            return new ChildResolver(this, context);
        }

        private sealed class ChildResolver : IResolver
        {
            private readonly SmartResolver resolver;

            private readonly IContext context;

            public ChildResolver(SmartResolver resolver, IContext context)
            {
                this.resolver = resolver;
                this.context = context;
            }

            public void Dispose()
            {
                context.Dispose();
            }

            // CanGet

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanGet<T>()
            {
                context.Switch();
                return resolver.FindFactoryEntry(typeof(T)).CanGet;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanGet<T>(IConstraint constraint)
            {
                context.Switch();
                return resolver.FindFactoryEntry(typeof(T), constraint).CanGet;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanGet(Type type)
            {
                context.Switch();
                return resolver.FindFactoryEntry(type).CanGet;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool CanGet(Type type, IConstraint constraint)
            {
                context.Switch();
                return resolver.FindFactoryEntry(type, constraint).CanGet;
            }

            // TryGet

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet<T>(out T obj)
            {
                context.Switch();
                var entry = resolver.FindFactoryEntry(typeof(T));
                obj = entry.CanGet ? (T)entry.Single() : default;
                return entry.CanGet;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet<T>(IConstraint constraint, out T obj)
            {
                context.Switch();
                var entry = resolver.FindFactoryEntry(typeof(T), constraint);
                obj = entry.CanGet ? (T)entry.Single() : default;
                return entry.CanGet;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(Type type, out object obj)
            {
                context.Switch();
                var entry = resolver.FindFactoryEntry(type);
                obj = entry.CanGet ? entry.Single() : default;
                return entry.CanGet;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TryGet(Type type, IConstraint constraint, out object obj)
            {
                context.Switch();
                var entry = resolver.FindFactoryEntry(type, constraint);
                obj = entry.CanGet ? entry.Single() : default;
                return entry.CanGet;
            }

            // Get

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T Get<T>()
            {
                context.Switch();
                return (T)resolver.FindFactoryEntry(typeof(T)).Single();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T Get<T>(IConstraint constraint)
            {
                context.Switch();
                return (T)resolver.FindFactoryEntry(typeof(T), constraint).Single();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public object Get(Type type)
            {
                context.Switch();
                return resolver.FindFactoryEntry(type).Single();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public object Get(Type type, IConstraint constraint)
            {
                context.Switch();
                return resolver.FindFactoryEntry(type, constraint).Single();
            }

            // GetAll

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerable<T> GetAll<T>()
            {
                context.Switch();
                return resolver.FindFactoryEntry(typeof(T)).Multiple.Select(x => (T)x());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerable<T> GetAll<T>(IConstraint constraint)
            {
                context.Switch();
                return resolver.FindFactoryEntry(typeof(T), constraint).Multiple.Select(x => (T)x());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerable<object> GetAll(Type type)
            {
                context.Switch();
                return resolver.FindFactoryEntry(type).Multiple.Select(x => x());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerable<object> GetAll(Type type, IConstraint constraint)
            {
                context.Switch();
                return resolver.FindFactoryEntry(type, constraint).Multiple.Select(x => x());
            }

            public void Inject(object instance)
            {
                if (instance is null)
                {
                    throw new ArgumentNullException(nameof(instance));
                }

                context.Switch();
                var actions = resolver.FindInjectors(instance.GetType());
                for (var i = 0; i < actions.Length; i++)
                {
                    actions[i](instance);
                }
            }
        }

        // ------------------------------------------------------------
        // Diagnostics
        // ------------------------------------------------------------

        public DiagnosticsInfo Diagnostics
        {
            get
            {
                var factoryDiagnostics = factoriesCache.Diagnostics;
                var constraintFactoryDiagnostics = factoriesCacheWithConstraint.Diagnostics;
                var injectorDiagnostics = injectorsCache.Diagnostics;

                return new DiagnosticsInfo(
                    factoryDiagnostics.Count,
                    factoryDiagnostics.Width,
                    factoryDiagnostics.Depth,
                    constraintFactoryDiagnostics.Count,
                    constraintFactoryDiagnostics.Width,
                    constraintFactoryDiagnostics.Depth,
                    injectorDiagnostics.Count,
                    injectorDiagnostics.Width,
                    injectorDiagnostics.Depth);
            }
        }

        public sealed class DiagnosticsInfo
        {
            public int FactoryCacheCount { get; }

            public int FactoryCacheWidth { get; }

            public int FactoryCacheDepth { get; }

            public int ConstraintFactoryCacheCount { get; }

            public int ConstraintFactoryCacheWidth { get; }

            public int ConstraintFactoryCacheDepth { get; }

            public int InjectorCacheCount { get; }

            public int InjectorCacheWidth { get; }

            public int InjectorCacheDepth { get; }

            public DiagnosticsInfo(
                int factoryCacheCount,
                int factoryCacheWidth,
                int factoryCacheDepth,
                int constraintFactoryCacheCount,
                int constraintFactoryCacheWidth,
                int constraintFactoryCacheDepth,
                int injectorCacheCount,
                int injectorCacheWidth,
                int injectorCacheDepth)
            {
                FactoryCacheCount = factoryCacheCount;
                FactoryCacheWidth = factoryCacheWidth;
                FactoryCacheDepth = factoryCacheDepth;
                ConstraintFactoryCacheCount = constraintFactoryCacheCount;
                ConstraintFactoryCacheWidth = constraintFactoryCacheWidth;
                ConstraintFactoryCacheDepth = constraintFactoryCacheDepth;
                InjectorCacheCount = injectorCacheCount;
                InjectorCacheWidth = injectorCacheWidth;
                InjectorCacheDepth = injectorCacheDepth;
            }
        }
    }
}

namespace Smart.Resolver.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    using Smart.ComponentModel;
    using Smart.Resolver.Attributes;
    using Smart.Resolver.Bindings;
    using Smart.Resolver.Builders;
    using Smart.Resolver.Constraints;
    using Smart.Resolver.Helpers;
    using Smart.Resolver.Injectors;
    using Smart.Resolver.Processors;

    public sealed class StandardProvider : IProvider
    {
        private readonly IInjector[] injectors;

        private readonly IProcessor[] processors;

        private readonly IFactoryBuilder builder;

        public Type TargetType { get; }

        public StandardProvider(Type type, IComponentContainer components)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (components is null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            TargetType = type;
            injectors = components.GetAll<IInjector>().ToArray();
            processors = components.GetAll<IProcessor>().ToArray();
            builder = components.Get<IFactoryBuilder>();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<object> CreateFactory(IKernel kernel, IBinding binding)
        {
            var constructors = CreateConstructorMetadata();
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.InvariantCulture, "No constructor available. type = {0}", TargetType.Name));
            }

            foreach (var constructor in constructors)
            {
                var match = true;
                var argumentFactories = new List<Func<object>>(constructor.Parameters.Length);

                foreach (var parameter in constructor.Parameters)
                {
                    var pi = parameter.Parameter;

                    // Constructor argument
                    var argument = binding.ConstructorArguments.GetParameter(pi.Name);
                    if (argument != null)
                    {
                        argumentFactories.Add(() => argument.Resolve(kernel));
                        continue;
                    }

                    // Resolve
                    if (kernel.TryResolveFactory(pi.ParameterType, parameter.Constraint, out var factory))
                    {
                        argumentFactories.Add(factory);
                        continue;
                    }

                    // Multiple
                    if ((parameter.ElementType != null) &&
                        kernel.TryResolveFactories(parameter.ElementType, parameter.Constraint, out var factories))
                    {
                        var arrayFactory = builder.CreateArrayFactory(parameter.ElementType, factories);
                        argumentFactories.Add(arrayFactory);
                        continue;
                    }

                    // DefaultValue
                    if (pi.HasDefaultValue)
                    {
                        argumentFactories.Add(() => pi.DefaultValue);
                        continue;
                    }

                    match = false;
                    break;
                }

                if (match)
                {
                    var actions = CreateActions(kernel, binding);
                    return builder.CreateFactory(constructor.Constructor, argumentFactories.ToArray(), actions);
                }
            }

            throw new InvalidOperationException(
            String.Format(CultureInfo.InvariantCulture, "Constructor parameter unresolved. type = {0}", TargetType.Name));
        }

        // ------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------

        private ConstructorMetadata[] CreateConstructorMetadata()
        {
            return TargetType.GetConstructors()
                .Where(c => !c.IsStatic)
                .OrderByDescending(c => c.IsInjectDefined() ? 1 : 0)
                .ThenByDescending(c => c.GetParameters().Length)
                .ThenByDescending(c => c.GetParameters().Count(p => p.HasDefaultValue))
                .Select(c => new ConstructorMetadata(c))
                .ToArray();
        }

        private Action<object>[] CreateActions(IResolver resolver, IBinding binding)
        {
            var targetInjectors = injectors
                .Select(x => x.CreateInjector(resolver, TargetType, binding))
                .Where(x => x != null);
            var targetProcessors = processors
                .OrderByDescending(x => x.Order)
                .Select(x => x.CreateProcessor(resolver, TargetType))
                .Where(x => x != null);
            return targetInjectors.Concat(targetProcessors).ToArray();
        }

        // ------------------------------------------------------------
        // Metadata
        // ------------------------------------------------------------

        private sealed class ConstructorMetadata
        {
            public ConstructorInfo Constructor { get; }

            public ParameterMetadata[] Parameters { get; }

            public ConstructorMetadata(ConstructorInfo ci)
            {
                Constructor = ci;
                Parameters = ci.GetParameters()
                    .Select(x => new ParameterMetadata(x))
                    .ToArray();
            }
        }

        private sealed class ParameterMetadata
        {
            public ParameterInfo Parameter { get; }

            public Type ElementType { get; }

            public IConstraint Constraint { get; }

            public ParameterMetadata(ParameterInfo pi)
            {
                Parameter = pi;
                ElementType = TypeHelper.GetEnumerableElementType(pi.ParameterType);
                Constraint = ConstraintBuilder.Build(pi.GetCustomAttributes<ConstraintAttribute>());
            }
        }
    }
}

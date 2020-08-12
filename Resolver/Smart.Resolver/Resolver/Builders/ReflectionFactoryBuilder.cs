namespace Smart.Resolver.Builders
{
    using System;
    using System.Reflection;

    public sealed class ReflectionFactoryBuilder : IFactoryBuilder
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<object> CreateFactory(ConstructorInfo ci, Func<object>[] factories, Action<object>[] actions)
        {
            if (ci.GetParameters().Length == 0)
            {
                return actions.Length == 0
                    ? BuildActivatorFactory(ci.DeclaringType)
                    : BuildActivatorWithActionsFactory(ci.DeclaringType, actions);
            }

            return actions.Length == 0
                ? BuildConstructorFactory(ci, factories)
                : BuildConstructorWithActionsFactory(ci, factories, actions);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        private static Func<object> BuildActivatorFactory(Type type)
        {
            return () => Activator.CreateInstance(type);
        }

        private static Func<object> BuildActivatorWithActionsFactory(Type type, Action<object>[] actions)
        {
            return () =>
            {
                var obj = Activator.CreateInstance(type);

                for (var i = 0; i < actions.Length; i++)
                {
                    actions[i](obj);
                }

                return obj;
            };
        }

        private static Func<object> BuildConstructorFactory(ConstructorInfo ci, Func<object>[] factories)
        {
            return () =>
            {
                var args = new object[factories.Length];
                for (var i = 0; i < factories.Length; i++)
                {
                    args[i] = factories[i]();
                }

                return ci.Invoke(args);
            };
        }

        private static Func<object> BuildConstructorWithActionsFactory(ConstructorInfo ci, Func<object>[] factories, Action<object>[] actions)
        {
            return () =>
            {
                var args = new object[factories.Length];
                for (var i = 0; i < factories.Length; i++)
                {
                    args[i] = factories[i]();
                }

                var obj = ci.Invoke(args);

                for (var i = 0; i < actions.Length; i++)
                {
                    actions[i](obj);
                }

                return obj;
            };
        }

        public Func<object> CreateArrayFactory(Type type, Func<object>[] factories)
        {
            return () =>
            {
                var array = Array.CreateInstance(type, factories.Length);
                for (var i = 0; i < factories.Length; i++)
                {
                    array.SetValue(factories[i](), i);
                }

                return array;
            };
        }
    }
}

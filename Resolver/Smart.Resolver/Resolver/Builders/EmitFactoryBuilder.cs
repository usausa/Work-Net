namespace Smart.Resolver.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Reflection.Emit;

    public sealed class EmitFactoryBuilder : IFactoryBuilder
    {
        private static readonly Action<object>[] EmptyActions = Array.Empty<Action<object>>();

        private static readonly HolderBuilder DefaultHolderBuilder = new HolderBuilder();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<object> CreateFactory(ConstructorInfo ci, Func<object>[] factories, Action<object>[] actions)
        {
            var holder = DefaultHolderBuilder.CreateHolder(factories, actions);
            var holderType = holder?.GetType() ?? typeof(object);

            var returnType = ci.DeclaringType.IsValueType ? typeof(object) : ci.DeclaringType;

            var dynamicMethod = new DynamicMethod(string.Empty, returnType, new[] { holderType }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            for (var i = 0; i < factories.Length; i++)
            {
                var invokeMethod = factories[i].GetType().GetMethod("Invoke");
                if ((invokeMethod == null) ||
                    (invokeMethod.GetParameters().Length != 0))
                {
                    throw new ArgumentException($"Invalid factory[{i}]");
                }

                var field = holderType.GetField($"factory{i}");

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, field);
                ilGenerator.Emit(OpCodes.Call, invokeMethod);
                ilGenerator.EmitTypeConversion(ci.GetParameters()[i].ParameterType);
            }

            ilGenerator.Emit(OpCodes.Newobj, ci);

            if (actions.Length > 0)
            {
                ilGenerator.DeclareLocal(ci.DeclaringType);

                ilGenerator.Emit(OpCodes.Stloc_0);

                for (var i = 0; i < actions.Length; i++)
                {
                    var invokeMethod = actions[i].GetType().GetMethod("Invoke");
                    if ((invokeMethod == null) ||
                        (invokeMethod.GetParameters().Length != 1))
                    {
                        throw new ArgumentException($"Invalid actions[{i}]");
                    }

                    var field = holderType.GetField($"action{i}");

                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldfld, field);
                    ilGenerator.Emit(OpCodes.Ldloc_0);
                    ilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
                }

                ilGenerator.Emit(OpCodes.Ldloc_0);
            }

            if (ci.DeclaringType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, ci.DeclaringType);
            }

            ilGenerator.Emit(OpCodes.Ret);

            var funcType = typeof(Func<>).MakeGenericType(returnType);
            return (Func<object>)dynamicMethod.CreateDelegate(funcType, holder);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<object> CreateArrayFactory(Type type, Func<object>[] factories)
        {
            var holder = DefaultHolderBuilder.CreateHolder(factories, EmptyActions);
            var holderType = holder?.GetType() ?? typeof(object);

            var arrayType = type.MakeArrayType();

            var dynamicMethod = new DynamicMethod(string.Empty, arrayType, new[] { holderType }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            ilGenerator.EmitLdcI4(factories.Length);
            ilGenerator.Emit(OpCodes.Newarr, type);

            for (var i = 0; i < factories.Length; i++)
            {
                var field = holderType.GetField($"factory{i}");

                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.EmitLdcI4(i);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, field);
                var invokeMethod = factories[i].GetType().GetMethod("Invoke", Type.EmptyTypes);
                ilGenerator.Emit(OpCodes.Call, invokeMethod);

                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }

            ilGenerator.Emit(OpCodes.Ret);

            var funcType = typeof(Func<>).MakeGenericType(arrayType);
            return (Func<object>)dynamicMethod.CreateDelegate(funcType, holder);
        }

        private sealed class HolderBuilder
        {
            private readonly Dictionary<Tuple<int, int>, Type> cache = new Dictionary<Tuple<int, int>, Type>();

            private AssemblyBuilder assemblyBuilder;

            private ModuleBuilder moduleBuilder;

            private ModuleBuilder ModuleBuilder
            {
                get
                {
                    if (moduleBuilder == null)
                    {
                        assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                            new AssemblyName("EmitBuilderAssembly"),
                            AssemblyBuilderAccess.Run);
                        moduleBuilder = assemblyBuilder.DefineDynamicModule(
                            "EmitBuilderModule");
                    }

                    return moduleBuilder;
                }
            }

            public object CreateHolder(Func<object>[] factories, Action<object>[] actions)
            {
                if ((factories.Length == 0) && (actions.Length == 0))
                {
                    return null;
                }

                var key = Tuple.Create(factories.Length, actions.Length);
                Type type;
                lock (cache)
                {
                    if (!cache.TryGetValue(key, out type))
                    {
                        type = CreateType(factories.Length, actions.Length);
                        cache[key] = type;
                    }
                }

                var holder = Activator.CreateInstance(type);

                for (var i = 0; i < factories.Length; i++)
                {
                    var field = type.GetField($"factory{i}");
                    field.SetValue(holder, factories[i]);
                }

                for (var i = 0; i < actions.Length; i++)
                {
                    var field = type.GetField($"action{i}");
                    field.SetValue(holder, actions[i]);
                }

                return holder;
            }

            private Type CreateType(int factoryCount, int actionCount)
            {
                // Define type
                var typeBuilder = ModuleBuilder.DefineType(
                    $"Holder_{factoryCount}_{actionCount}",
                    TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);

                // Define factory fields
                for (var i = 0; i < factoryCount; i++)
                {
                    typeBuilder.DefineField(
                        $"factory{i}",
                        typeof(Func<object>),
                        FieldAttributes.Public);
                }

                // Define action fields
                for (var i = 0; i < actionCount; i++)
                {
                    typeBuilder.DefineField(
                        $"action{i}",
                        typeof(Action<object>),
                        FieldAttributes.Public);
                }

                var typeInfo = typeBuilder.CreateTypeInfo();
                return typeInfo.AsType();
            }
        }
    }
}

namespace ConsoleApp
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Reflection.Emit;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var factory = CreateFactory0<Class0>(typeof(Class0).GetConstructors().First());
            var obj = factory();

            var factory1 = CreateFactory1<int, Class1>(typeof(Class1).GetConstructors().First());
            var obj1 = factory1(1);

            var factory2 = CreateFactory2<int, string, Class2>(typeof(Class2).GetConstructors().First());
            var obj2 = factory2(1, "a");

            var factory2B = CreateFactory2<object, object, Class2>(typeof(Class2).GetConstructors().First());
            var obj2B = factory2B(1, "a");
        }

        public static Func<T> CreateFactory0<T>(ConstructorInfo ci)
        {
            return (Func<T>)CreateFactoryInternal(
                ci,
                typeof(T),
                new[] { typeof(object) },
                typeof(Func<T>));
        }

        public static Func<TP1, T> CreateFactory1<TP1, T>(ConstructorInfo ci)
        {
            return (Func<TP1, T>)CreateFactoryInternal(
                ci,
                typeof(T),
                new[] { typeof(object), typeof(TP1) },
                typeof(Func<TP1, T>));
        }

        public static Func<TP1, TP2, T> CreateFactory2<TP1, TP2, T>(ConstructorInfo ci)
        {
            return (Func<TP1, TP2, T>)CreateFactoryInternal(
                ci,
                typeof(T),
                new[] { typeof(object), typeof(TP1), typeof(TP2) },
                typeof(Func<TP1, TP2, T>));
        }

        private static Delegate CreateFactoryInternal(ConstructorInfo ci, Type resutnType, Type[] parameterTypes, Type delegateType)
        {
            if (ci.GetParameters().Length != parameterTypes.Length - 1)
            {
                throw new ArgumentException($"Constructor parameter length is invalid. length={ci.GetParameters().Length}", nameof(ci));
            }

            for (var i = 0; i < ci.GetParameters().Length; i++)
            {
                if (!ci.GetParameters()[i].ParameterType.IsAssignableFrom(parameterTypes[i + 1]))
                {
                    throw new ArgumentException("Constructor parameter unmatched generic type.");
                }
            }

            var dynamic = new DynamicMethod(string.Empty, resutnType, parameterTypes, true);
            var il = dynamic.GetILGenerator();

            for (var i = 0; i < ci.GetParameters().Length; i++)
            {
                il.EmitLdarg(i + 1);
                //il.EmitTypeConversion(ci.GetParameters()[i].ParameterType);
            }

            il.Emit(OpCodes.Newobj, ci);
            il.Emit(OpCodes.Ret);

            return dynamic.CreateDelegate(delegateType, null);
        }
    }
}

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
        }

        public static Func<T> CreateFactory0<T>(ConstructorInfo ci)
        {
            if (ci == null)
            {
                throw new ArgumentNullException(nameof(ci));
            }

            return (Func<T>)CreateFactoryInternal(
                ci,
                typeof(T),
                new[] { typeof(object) },
                typeof(Func<T>));
        }

        private static Delegate CreateFactoryInternal(ConstructorInfo ci, Type resutnType, Type[] parameterTypes, Type delegateType)
        {
            if (ci.GetParameters().Length != parameterTypes.Length - 1)
            {
                throw new ArgumentException($"Constructor parameter length is invalid. length={ci.GetParameters().Length}", nameof(ci));
            }

            // TODO assingble ?

            var dynamic = new DynamicMethod(string.Empty, resutnType, parameterTypes, true);
            var il = dynamic.GetILGenerator();

            for (var i = 0; i < ci.GetParameters().Length; i++)
            {
                il.EmitLdarg(i + 1);
                // TODO need ?
                il.EmitTypeConversion(ci.GetParameters()[i].ParameterType);
            }

            il.Emit(OpCodes.Newobj, ci);
            il.Emit(OpCodes.Ret);

            return dynamic.CreateDelegate(delegateType, null);
        }
    }
}

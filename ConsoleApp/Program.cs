namespace ConsoleApp
{
    using System;
    using System.Diagnostics;
    using Smart.Refrection;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var activator = CreateNewConstraintActivator(typeof(Hoge));
            var obj = activator.Create();
            Debug.WriteLine(obj);
        }

        private static readonly Type ActivatorType = typeof(NewConstraintActivator<>);

        public static IActivator CreateNewConstraintActivator(Type type)
        {
            var activatorType = ActivatorType.MakeGenericType(type);
            return (IActivator)Activator.CreateInstance(activatorType);
        }
    }

    public class NewConstraintActivator<T> : IActivator
        where T : new()
    {
        public object Create()
        {
            return NewConstraintFactory<T>.Create();
        }
    }

    public interface IActivator
    {
        object Create();
    }

    public class Hoge
    {
    }
}

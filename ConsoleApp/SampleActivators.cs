namespace ConsoleApp
{
    using System.Reflection;

    using Smart.Reflection;

    public sealed class Sample0Activator : IActivator
    {
        public ConstructorInfo Source { get; }

        public Sample0Activator(ConstructorInfo source)
        {
            Source = source;
        }

        public object Create(params object[] arguments)
        {
            return new Class0();
        }
    }

    public sealed class Sample1Activator : IActivator
    {
        public ConstructorInfo Source { get; }

        public Sample1Activator(ConstructorInfo source)
        {
            Source = source;
        }

        public object Create(params object[] arguments)
        {
            return new Class1((int)arguments[0]);
        }
    }
}

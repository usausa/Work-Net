namespace ConsoleApp
{
    using Smart.Reflection;

    public static class Program
    {
        public static void Main(string[] args)
        {
            var factory = DelegateFactory.Default.CreateFactory<Class0>();
            var obj = factory();

            var factory1 = DelegateFactory.Default.CreateFactory<int, Class1>();
            var obj1 = factory1(1);

            var factory2 = DelegateFactory.Default.CreateFactory<int, string, Class2>();
            var obj2 = factory2(1, "a");

            var factory2B = DelegateFactory.Default.CreateFactory<object, object, Class2>();
            var obj2B = factory2B(1, "a");
        }
    }
}

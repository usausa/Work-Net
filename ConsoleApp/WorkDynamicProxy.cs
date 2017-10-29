namespace ConsoleApp
{
    using System;
    using Castle.DynamicProxy;

    public static class WorkDynamicProxy
    {
        public static void Test()
        {
            var generator = new ProxyGenerator();
            var target = generator.CreateClassProxy<DebutTarget>(new DebugIntercepter());
            var targetType = target.GetType();
            Console.WriteLine(targetType.Assembly.GetName());
            target.Work();
        }
    }

    public class DebugIntercepter : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Console.WriteLine("-Before-");
            invocation.Proceed();
            Console.WriteLine("-After-");
        }
    }

    public class DebutTarget
    {
        public virtual void Work()
        {
            Console.WriteLine("Work");
        }
    }
}

namespace ConsoleApp
{
    using System;
    using Castle.DynamicProxy;

    public static class WorkDynamicProxy
    {
        public static void Test()
        {
            var generator = new ProxyGenerator();
            var target = generator.CreateClassProxy<DebugTarget>(new DebugIntercepter());
            var targetB = generator.CreateClassProxy<DebugTarget>(new DebugIntercepter());
            var target2 = generator.CreateClassProxy<DebugTarget>(new Debug2Intercepter());
            var targetType = target.GetType();
            var targetTypeB = targetB.GetType();
            var targetType2 = target2.GetType();
            Console.WriteLine(targetType.FullName);
            Console.WriteLine(targetType.Assembly.GetName());
            Console.WriteLine(targetTypeB.FullName);
            Console.WriteLine(targetTypeB.Assembly.GetName());
            Console.WriteLine(targetType2.FullName);
            Console.WriteLine(targetType2.Assembly.GetName());

            Console.WriteLine(targetType == targetTypeB);
            Console.WriteLine(targetType == targetType2);

            target.Work();
            targetB.Work();
            target2.Work();
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

    public class Debug2Intercepter : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            Console.WriteLine("-Before2-");
            invocation.Proceed();
            Console.WriteLine("-After2-");
        }
    }

    public class DebugTarget
    {
        public virtual void Work()
        {
            Console.WriteLine("Work");
        }
    }
}

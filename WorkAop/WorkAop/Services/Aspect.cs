namespace WorkAop.Services;

using System.Diagnostics;
using System.Xml.Linq;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute
{
    public Type TargetInterface { get; }

    public ServiceAttribute(Type targetInterface)
    {
        TargetInterface = targetInterface;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class ServiceRegistryAttribute : Attribute
{
}

public sealed class Activity : IDisposable
{
    private readonly string name;

    public Activity(string name)
    {
        this.name = name;
    }

    public void Dispose()
    {
        Debug.WriteLine($"**** Dispose {name}");
    }
}

#pragma warning disable CA1815
public readonly struct ServiceAspectToken
{
    public readonly long Start;

    public readonly IDisposable? Activity;

    public ServiceAspectToken(long start, IDisposable? activity)
    {

    }
}
#pragma warning restore CA1815

public sealed class ServiceAspect<T>
{
    public void Start(string name)
    {
        Debug.WriteLine($"*Start {name}");
    }

    public void Start<T1>(string name, T1 p1)
    {
        Debug.WriteLine($"*Start {name} : {p1}");
    }

    public void Start<T1, T2>(string name, T1 p1, T2 p2)
    {
        Debug.WriteLine($"*Start {name} : {p1}, {p2}");
    }

    public void Start<T1, T2, T3>(string name, T1 p1, T2 p2, T3 p3)
    {
        Debug.WriteLine($"* Start{name} : {p1}, {p2}, {p3}");
    }

    // TODO ...

    public void Finish()
    {
        Debug.WriteLine("*Finish");
    }

    public void Finish<TRet>(TRet result)
    {
        Debug.WriteLine($"*Finish : {result}");
    }

    public void Exception(Exception ex)
    {
        Debug.WriteLine($"*Finish : {ex}");
    }
}

//public static class ServiceExtensions
//{
//    public static IServiceCollection AddExtendedServices(this IServiceCollection services)
//    {
//        // TODO
//        services.AddSingleton<TestService>();
//        services.AddSingleton<TestServiceProxy>();
//        services.AddSingleton<ITestService>(static p => p.GetRequiredService<TestServiceProxy>());

//        return services;
//    }
//}

//// TODO
//#pragma warning disable CA1727
//#pragma warning disable CA1848
//public sealed class TestServiceProxy : ITestService
//{
//    private readonly ILogger<TestService> log;

//    private readonly TestService service;

//    public TestServiceProxy(ILogger<TestService> log, TestService service)
//    {
//        this.log = log;
//        this.service = service;
//    }

//    public int Calc(int x, int y)
//    {
//        // TODO parameter
//        log.LogInformation("Service start. {x} {y}", x, y);

//        var ret = default(int);
//        try
//        {
//            ret = service.Calc(x, y);
//            return ret;
//        }
//        finally
//        {
//            // TODO parameter, time
//            log.LogInformation("Service start. {ret}", ret);
//        }
//    }
//}

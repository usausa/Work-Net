namespace WorkAop.Services;

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

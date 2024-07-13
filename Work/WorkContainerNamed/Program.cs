namespace WorkContainerNamed;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

public static class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IService, Service1>(nameof(Service1));
        services.AddKeyedSingleton<IService, Service2>(nameof(Service2));

        var provider = services.BuildServiceProvider();

        var service1 = provider.GetRequiredKeyedService<IService>(nameof(Service1));
        var service2 = provider.GetRequiredKeyedService<IService>(nameof(Service2));

        service1.Execute();
        service2.Execute();

        // IKeyedServiceProviderを実装しているか
        // public sealed class ServiceProvider : IServiceProvider, IKeyedServiceProvider, IDisposable, IAsyncDisposable
    }
}

public interface IService
{
    void Execute();
}

public class Service1 : IService
{
    public void Execute()
    {
        Debug.WriteLine("Service1");
    }
}

public class Service2 : IService
{
    public void Execute()
    {
        Debug.WriteLine("Service2");
    }
}

using Microsoft.Extensions.DependencyInjection;
using WorkCliHost.Core;

namespace WorkCliHost.Samples;

/// <summary>
/// Example: Using a custom DI container with ConfigureContainer.
/// This demonstrates how to replace the default ServiceProvider with a custom one.
/// </summary>
public static class CustomContainerExample
{
    public static async Task<int> RunWithCustomContainerAsync(string[] args)
    {
        var builder = CliHost.CreateBuilder(args);

        // Example: Configure a custom container
        // In a real scenario, you would use a third-party DI container like Autofac:
        // builder.ConfigureContainer(new AutofacServiceProviderFactory(), container =>
        // {
        //     container.RegisterType<MyService>().As<IMyService>();
        // });

        // For demonstration, we use a custom factory that wraps the default ServiceProvider
        builder.ConfigureContainer(new CustomServiceProviderFactory(), container =>
        {
            // Configure the custom container
            Console.WriteLine("Configuring custom container...");
            
            // Register services in the custom container
            container.AddSingleton<ICustomService, CustomService>();
        });

        builder.ConfigureCommands(commands =>
        {
            commands.AddCommand<CustomContainerCommand>();
        });

        var host = builder.Build();
        return await host.RunAsync();
    }
}

// Custom service interface
public interface ICustomService
{
    string GetMessage();
}

// Custom service implementation
public sealed class CustomService : ICustomService
{
    public string GetMessage() => "Hello from custom container!";
}

// Command that uses the custom service
[CliCommand("custom", Description = "Command using custom container")]
public sealed class CustomContainerCommand : ICommandDefinition
{
    private readonly ICustomService _customService;

    public CustomContainerCommand(ICustomService customService)
    {
        _customService = customService;
    }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        Console.WriteLine(_customService.GetMessage());
        return ValueTask.CompletedTask;
    }
}

// Custom ServiceProviderFactory (for demonstration)
public sealed class CustomServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        Console.WriteLine("CustomServiceProviderFactory: CreateBuilder called");
        return services;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        Console.WriteLine("CustomServiceProviderFactory: CreateServiceProvider called");
        return containerBuilder.BuildServiceProvider();
    }
}

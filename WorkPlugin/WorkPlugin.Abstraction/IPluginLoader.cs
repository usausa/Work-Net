using Microsoft.Extensions.DependencyInjection;

namespace WorkPlugin.Abstraction;

public interface IPluginLoader
{
    void Configure(IServiceCollection services);
}

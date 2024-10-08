namespace WorkMultiRuntime;

using Microsoft.Extensions.DependencyInjection;

public interface IPluginInitializer
{
    public void Setup(IServiceCollection services);
}

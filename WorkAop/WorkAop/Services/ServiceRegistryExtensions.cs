namespace WorkAop.Services;

public static partial class ServiceRegistryExtensions
{
    [ServiceRegistry]
    public static partial IServiceCollection AddExtendedServices(this IServiceCollection services);
}

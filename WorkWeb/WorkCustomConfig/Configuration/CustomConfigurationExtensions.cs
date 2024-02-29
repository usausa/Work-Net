namespace WorkCustomConfig.Configuration;

public static class CustomConfigurationExtensions
{
    public static IConfigurationBuilder AddCustomConfiguration(this IConfigurationBuilder builder, Action<CustomConfigurationOption> configure)
    {
        var option = new CustomConfigurationOption();
        configure(option);
        return builder.Add(new CustomConfigurationSource(option));
    }

    public static IServiceCollection AddCustomConfigurationOperator(this IServiceCollection services)
    {
        services.AddSingleton(p => (IConfigurationOperator)((IConfigurationRoot)p.GetRequiredService<IConfiguration>()).Providers.First(x => x is IConfigurationOperator));
        return services;
    }
}

namespace WorkCron;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScheduler<T>(this IServiceCollection services, Action<SchedulerConfig<T>> options)
        where T : SchedulerService
    {
        var config = new SchedulerConfig<T>();
        options.Invoke(config);
        services.AddSingleton(config);
        services.AddHostedService<T>();
        return services;
    }
}

namespace WorkExporterCustom.Metrics;

using OpenTelemetry.Metrics;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddApplicationInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddMeter(ApplicationMetrics.MeterName);
        return builder.AddInstrumentation(s => s.GetRequiredService<ApplicationMetrics>());
    }

    public static IServiceCollection AddApplicationInstrumentation(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationMetrics>();
        return services;
    }
}

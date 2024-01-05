namespace WorkBasicWeb;

using OpenTelemetry.Metrics;

using System.Diagnostics.Metrics;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddApiInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddMeter(ApiMetrics.MeterName);
        return builder;
    }

    public static IServiceCollection AddApiMetrics(this IServiceCollection services)
    {
        services.AddSingleton<ApiMetrics>();
        return services;
    }
}

public sealed class ApiMetrics
{
    internal static readonly string MeterName = "API";

    private readonly Counter<long> testExecuteCounter;

    public ApiMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName, typeof(ApiMetrics).Assembly.GetName().Version!.ToString());

        testExecuteCounter = meter.CreateCounter<long>("api.test.execute", description: "Count of api call");
    }

    public void IncrementTestExecute() => testExecuteCounter.Add(1);
}


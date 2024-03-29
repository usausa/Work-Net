namespace WorkBasicWeb;

using OpenTelemetry.Metrics;

using System.Diagnostics.Metrics;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddApiInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddMeter(ApiInstrument.MeterName);
        return builder;
    }

    public static IServiceCollection AddApiInstrument(this IServiceCollection services)
    {
        services.AddSingleton<ApiInstrument>();
        return services;
    }
}

public sealed class ApiInstrument
{
    internal const string MeterName = "API";

    private readonly Counter<long> testExecuteCounter;

    public ApiInstrument(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName, typeof(ApiInstrument).Assembly.GetName().Version!.ToString());

        testExecuteCounter = meter.CreateCounter<long>("api.test.execute", description: "Count of api call");
    }

    public void IncrementTestExecute() => testExecuteCounter.Add(1);
}


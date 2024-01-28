namespace WorkMetricsTag;

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

    private readonly Counter<long> counter;

    public ApiInstrument(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName, typeof(ApiInstrument).Assembly.GetName().Version!.ToString());

        counter = meter.CreateCounter<long>("api.test");
    }

    public void IncrementTest(string name) => counter.Add(1, new KeyValuePair<string, object?>[] { new("drive", name) });
}


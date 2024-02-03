namespace WorkMetricsCallback;

using OpenTelemetry.Metrics;

using System.Diagnostics.Metrics;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddCustomInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddMeter(CustomInstrument.MeterName);
        return builder;
    }

    //public static IServiceCollection AddCustomInstrument(this IServiceCollection services)
    //{
    //    services.AddSingleton<CustomInstrument>();
    //    return services;
    //}
}

public sealed class CustomInstrument
{
    internal const string MeterName = "Custom";

    private readonly Counter<long> counter;

    public CustomInstrument(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName, typeof(CustomInstrument).Assembly.GetName().Version!.ToString());

        counter = meter.CreateCounter<long>("custom.test");
    }

    public void IncrementTest(string name) => counter.Add(1, new KeyValuePair<string, object?>[] { new("drive", name) });
}


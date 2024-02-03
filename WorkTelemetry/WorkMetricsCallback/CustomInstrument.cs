namespace WorkMetricsCallback;

using OpenTelemetry.Metrics;

using System.Diagnostics.Metrics;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddCustomInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddInstrumentation(p => new CustomInstrument(p.GetRequiredService<IMeterFactory>()));
        builder.AddMeter(CustomInstrument.MeterName);
        return builder;
    }
}

public sealed class CustomInstrument
{
    internal const string MeterName = "Custom";

    public CustomInstrument(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName, typeof(CustomInstrument).Assembly.GetName().Version!.ToString());

        meter.CreateObservableCounter("custom.test", ObserveValue);
    }

    private static long ObserveValue()
    {
        return DateTime.Now.Ticks;
    }
}


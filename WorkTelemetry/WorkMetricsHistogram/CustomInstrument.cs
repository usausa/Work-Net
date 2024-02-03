namespace WorkMetricsHistogram;

using OpenTelemetry.Metrics;

using System.Diagnostics.Metrics;

internal static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddApiInstrumentation(this MeterProviderBuilder builder)
    {
        builder.AddInstrumentation<ApiInstrument>();
        builder.AddMeter(ApiInstrument.MeterName);
        builder.AddView(
            "api.test",
            new ExplicitBucketHistogramConfiguration { Boundaries = [1, 2, 5, 10, 15, 30] });


        return builder;
    }
}

public sealed class ApiInstrument
{
    internal const string MeterName = "API";

    private readonly Histogram<double> histogram;

    public ApiInstrument(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName, typeof(ApiInstrument).Assembly.GetName().Version!.ToString());

        histogram = meter.CreateHistogram<double>("api.test");
    }

    public void RecordTest(double value) => histogram.Record(value);
}


namespace WorkTraceZipkin;

using System.Diagnostics;

using OpenTelemetry.Trace;

internal static class MeterProviderBuilderExtensions
{
    public static TracerProviderBuilder AddApiInstrumentation(this TracerProviderBuilder builder)
    {
        builder.AddSource(ApiInstrument.ActivityName);
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
    internal const string ActivityName = "API";

    public ActivitySource ActivitySource { get; }

    public ApiInstrument()
    {
        ActivitySource = new ActivitySource(ActivityName, typeof(ApiInstrument).Assembly.GetName().Version!.ToString());
    }
}


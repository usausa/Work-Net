namespace WorkExporterCustom.Metrics;

using System;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Reflection;

public sealed class ApplicationMetrics
{
    public const string InstrumentName = "Application";

    internal static readonly AssemblyName AssemblyName = typeof(ApplicationMetrics).Assembly.GetName();
    internal static readonly string MeterName = AssemblyName.Name!;

    private static readonly Meter MeterInstance = new(MeterName, AssemblyName.Version!.ToString());

    private readonly Counter<long> counter;

    public ApplicationMetrics()
    {
        MeterInstance.CreateObservableCounter("application.uptime", ObserveApplicationUptime);

        counter = MeterInstance.CreateCounter<long>("application.counter");
    }

    private static long ObserveApplicationUptime() =>
        (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;

    public void IncrementCounter(string name) => counter.Add(1, new KeyValuePair<string, object?>[] { new("name", name) });
}

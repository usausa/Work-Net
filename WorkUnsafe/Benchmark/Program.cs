namespace Benchmark;

using System.Reflection;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);

        AddDiagnoser(
            new DisassemblyDiagnoser(
                new DisassemblyDiagnoserConfig(
                    maxDepth: 3,
                    printSource: true,
                    printInstructionAddresses: true,
                    exportCombinedDisassemblyReport: true,
                    exportDiff: true)),
            MemoryDiagnoser.Default);
    }
}

namespace WorkCryptBenchmark;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using System.Security.Cryptography;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<Benchmark>();
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
        AddDiagnoser(MemoryDiagnoser.Default, new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
    }
}

[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    private const int N = 1000;

    private static readonly byte[] KeyAndIv = "slv3tuzx"u8.ToArray();

    private static readonly DES PooledDes = DES.Create();

    [Benchmark]
    public void OldStyle()
    {
        for (var i = 0; i < N; i++)
        {
            OldStyleInternal();
        }
    }

    private void OldStyleInternal()
    {
        var data = ArrayPool<byte>.Shared.Rent(504);
        data.AsSpan().Clear();
        var encrypted = ArrayPool<byte>.Shared.Rent(512);

        using var des = DES.Create();
        des.Key = KeyAndIv;
        des.IV = KeyAndIv;  // IVも同じ鍵を使用
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.None;  // 手動でパディング済み

        using var encryptor = des.CreateEncryptor();
        var len = encryptor.TransformBlock(data, 0, 504, encrypted, 0);

        ArrayPool<byte>.Shared.Return(encrypted);
        ArrayPool<byte>.Shared.Return(data);
    }

    [Benchmark]
    public void NewStyle()
    {
        for (var i = 0; i < N; i++)
        {
            NewStyleInternal();
        }
    }

    private void NewStyleInternal()
    {
        Span<byte> input = stackalloc byte[504];
        Span<byte> output = stackalloc byte[512];

        using var des = DES.Create();
        var len = des.EncryptCbc(input, KeyAndIv, output, PaddingMode.None);
    }

    [Benchmark]
    public void NewStylePooled()
    {
        for (var i = 0; i < N; i++)
        {
            NewStylePooledInternal();
        }
    }

    private void NewStylePooledInternal()
    {
        Span<byte> input = stackalloc byte[504];
        Span<byte> output = stackalloc byte[512];

        var len = PooledDes.EncryptCbc(input, KeyAndIv, output, PaddingMode.None);
    }
}

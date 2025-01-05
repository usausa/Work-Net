namespace SampleDependencyGenerator;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

// https://github.com/jimmy-mll/DependencyInjection.SourceGenerators
// 2024/04 3k
// 対象クラスにAttribute指定
// 拡張メソッド生成
// Key対応
internal class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddServicesFromSampleDependencyGeneratorAssembly();
        var provider = services.BuildServiceProvider();

        var calc = provider.GetRequiredService<ICalc>();
        Debug.WriteLine(calc.Eval(1, 2));
    }
}

internal interface ICalc
{
    int Eval(int x, int y);
}

[Singleton]
internal sealed class Calc : ICalc
{
    public int Eval(int x, int y) => x + y;
}

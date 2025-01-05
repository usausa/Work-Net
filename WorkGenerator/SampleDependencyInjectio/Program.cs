namespace SampleDependencyInjectio;

using Injectio.Attributes;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

// https://github.com/loresoft/Injectio
// 2024/11 91k
// Attribute分離
// 対象クラスにAttribute指定、色々+
// 拡張メソッド生成
// Key対応
// 静的メソッド呼び出し
internal class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AddSampleDependencyInjectio();
        var provider = services.BuildServiceProvider();

        var calc = provider.GetRequiredService<ICalc>();
        Debug.WriteLine(calc.Eval(1, 2));
    }

    [RegisterServices]
    public static void Register(IServiceCollection services)
    {
    }
}

internal interface ICalc
{
    int Eval(int x, int y);
}

//[RegisterSingleton<ICalc>(ServiceKey = "Test")]
[RegisterSingleton<ICalc>]
internal sealed class Calc : ICalc
{
    public int Eval(int x, int y) => x + y;
}

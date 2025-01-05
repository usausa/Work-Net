namespace SampleDependencyAutoRegister;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

// https://github.com/patrickklaeren/AutoRegisterInject
// 2024/04 47k
// 対象クラスにAttribute指定
// 拡張メソッド生成
// Key対応
internal class Program
{
    public static void Main()
    {
        var services = new ServiceCollection();
        services.AutoRegister();
        //services.AutoRegisterFromSampleDependencyAutoRegister();
        var provider = services.BuildServiceProvider();

        var calc = provider.GetRequiredService<ICalc>();
        Debug.WriteLine(calc.Eval(1, 2));
    }
}

internal interface ICalc
{
    int Eval(int x, int y);
}

[RegisterSingleton(typeof(ICalc))]
internal sealed class Calc : ICalc
{
    public int Eval(int x, int y) => x + y;
}

using System.Diagnostics;

using Jab;

namespace SampleDependencyJab;

// https://github.com/pakrym/jab
// 2023/12 86k
// 非IServiceCollection
internal class Program
{
    public static void Main()
    {
        var provider = new ServiceProvider();

        var calc = provider.GetService<ICalc>();
        Debug.WriteLine(calc.Eval(1, 2));
    }
}

[ServiceProvider]
[Singleton(typeof(ICalc), typeof(Calc))]
internal sealed partial class ServiceProvider
{
}

internal interface ICalc
{
    int Eval(int x, int y);
}

internal sealed class Calc : ICalc
{
    public int Eval(int x, int y) => x + y;
}

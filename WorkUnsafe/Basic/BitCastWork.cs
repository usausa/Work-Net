namespace Basic;

using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class BitCastWork
{
    public static void Run()
    {
        var intValue = 123456789;
        var floatValue1 = Unsafe.As<int, float>(ref intValue);
        var floatValue2 = Unsafe.BitCast<int, float>(intValue);
        var intValue1 = Unsafe.As<float, int>(ref floatValue1);
        var intValue2 = Unsafe.BitCast<float, int>(floatValue2);
        Debug.WriteLine(intValue1);
        Debug.WriteLine(intValue2);
    }
}

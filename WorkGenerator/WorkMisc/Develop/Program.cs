namespace Develop;

using WorkMisc;

internal static class Program
{
    public static void Main()
    {
    }
}

#pragma warning disable CA1822
[CustomClass]
internal sealed class Target
{
    [CustomMethod]
    public void Method1()
    {
    }

    [CustomMethod]
    public void Method2()
    {
    }
}

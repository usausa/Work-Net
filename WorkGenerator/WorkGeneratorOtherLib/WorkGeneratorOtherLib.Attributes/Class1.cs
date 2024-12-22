namespace WorkGeneratorOtherLib.Attributes;

using WorkGeneratorOtherLib.Common;

public static class Class1
{
#pragma warning disable CA1822
    public static string Resolve() => Shared.Hello("test");
#pragma warning restore CA1822
}

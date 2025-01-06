namespace WorkScan.SourceGenerator.Example;

using Microsoft.Extensions.DependencyInjection;
using WorkScan.SourceGenerator.Attributes;

internal static class Program
{
    public static void Main()
    {
    }

    [ComponentSource("Service")]
    public static void AddServices(IServiceCollection services)
    {
    }
}

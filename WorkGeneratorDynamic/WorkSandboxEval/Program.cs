namespace WorkSandboxEval;

using System.Diagnostics;

using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;

internal static class Program
{
    public static void Main()
    {
        // https://github.com/dotnet/msbuild/discussions/10256
        var watch = Stopwatch.StartNew();
        // Initialize MSBuild
        MSBuildLocator.RegisterDefaults();
        Console.WriteLine(watch.ElapsedMilliseconds);

        watch.Restart();
        Test();
        Console.WriteLine(watch.ElapsedMilliseconds);
    }

    private static void Test()
    {
        var path = @"..\..\..\..\WorkGeneratorDynamic.Example\WorkGeneratorDynamic.Example.csproj";
        Debug.Write(Path.GetFullPath(path));
        var project = new Project(path);

        Debug.WriteLine(project.GetPropertyValue("AssemblyName"));
        foreach (var item in project.Items)
        {
            Debug.WriteLine($"Item: {item.ItemType} {item.EvaluatedInclude}");
        }
    }
}

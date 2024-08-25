namespace WorkGeneratorDynamic;

using System.Diagnostics;

using Microsoft.Build.Evaluation;
//using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1035
[Generator]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // TODO
        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider.Combine(context.CompilationProvider), (spc, source) =>
        {
            var (options, compilation) = source;

            // TODO
            //MSBuildLocator.RegisterDefaults();

            // Project
            var projectPath = compilation.SyntaxTrees.First().FilePath;
            var projectDirectory = Path.GetDirectoryName(projectPath)!;
            var csprojPath = Directory.GetFiles(projectDirectory, "*.csproj").First();

            var project = new Project(csprojPath);

            Debug.WriteLine(project.GetPropertyValue("AssemblyName"));
            foreach (var item in project.Items)
            {
                Debug.WriteLine($"Item: {item.ItemType} {item.EvaluatedInclude}");
            }

            // Info
            foreach (var key in options.GlobalOptions.Keys)
            {
                var value = options.GlobalOptions.TryGetValue(key, out var v) ? v : string.Empty;
                Debug.WriteLine($"{key} {value}");
            }

            options.GlobalOptions.TryGetValue("build_property.OutputPath", out var outputPath);
            Debug.WriteLine(outputPath);
            Debug.WriteLine(compilation.AssemblyName);
        });
    }
}

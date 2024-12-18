namespace WorkGeneratorOtherLib;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis;

[Generator]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.MetadataReferencesProvider
            .Collect()
            .Select((x, _) => x);

        context.RegisterSourceOutput(provider, Execute);
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<MetadataReference> references)
    {
        //foreach (var reference in references)
        //{
        //    Debug.WriteLine(reference.Display);
        //}

        var target = references.FirstOrDefault(x => x.Display?.EndsWith("WorkGeneratorOtherLib.Attributes.dll", StringComparison.OrdinalIgnoreCase) ?? false);
        if (target is not null)
        {
            // TODO
            var assembly = Assembly.LoadFile(target.Display!);
            foreach (var type in assembly.ExportedTypes)
            {
                Debug.WriteLine(type.FullName);
            }
        }
    }
}

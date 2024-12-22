namespace WorkGeneratorOtherLib;

using System.Diagnostics;
using System.Reflection;

using Microsoft.CodeAnalysis;
[Generator]
public sealed class Generator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    // TODO ref https://github.com/fiseni/SmartAnnotations
#pragma warning disable RS1035
    public void Execute(GeneratorExecutionContext context)
    {
        foreach (var reference in context.Compilation.References)
        {
            Debug.WriteLine(reference.Display);
        }

        var target = context.Compilation.References.FirstOrDefault(x => x.Display?.EndsWith("WorkGeneratorOtherLib.Attributes.dll", StringComparison.OrdinalIgnoreCase) ?? false);
        if (target is not null)
        {
            // TODO
            var assembly = Assembly.LoadFile(target.Display!);
            foreach (var type in assembly.ExportedTypes)
            {
                Debug.WriteLine(type.FullName);
                var mi = type.GetMethod("Resolve");
                if (mi is not null)
                {
#pragma warning disable CA1031
                    try
                    {
                        var ret = mi.Invoke(null, []);
                        Debug.WriteLine(ret);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
#pragma warning restore CA1031
                }
            }
        }
    }
#pragma warning restore RS1035
}

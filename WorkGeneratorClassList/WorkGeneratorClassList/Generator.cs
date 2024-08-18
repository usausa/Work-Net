namespace WorkGeneratorClassList;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;
        var classSymbolsProvider = compilationProvider.Select((compilation, cancellationToken) =>
        {
            // TODO Addの属性を取得して、アセンブリ名が一致したらそこ、指定がなかったら自信の
            var list = new List<INamedTypeSymbol>();

            foreach (var reference in compilation.References)
            {
                if ((compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol) &&
                    assemblySymbol.Name.StartsWith("Work", StringComparison.InvariantCulture))
                {
                    foreach (var namespaceSymbol in assemblySymbol.GlobalNamespace.GetNamespaceMembers())
                    {
                        AddTypeSymbol(namespaceSymbol, list);
                    }
                }
            }

            foreach (var symbol in list)
            {
                Debug.WriteLine(symbol.Name);
            }

            return list;
        });

        context.RegisterImplementationSourceOutput(
            classSymbolsProvider,
            static (context, provider) => Execute(context, provider));
    }

    private static void AddTypeSymbol(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> symbols)
    {
        foreach (var typeSymbol in namespaceSymbol.GetTypeMembers().Where(x => x.TypeKind == TypeKind.Class))
        {
            symbols.Add(typeSymbol);
        }

        foreach (var childNamespaceSymbol in namespaceSymbol.GetNamespaceMembers())
        {
            AddTypeSymbol(childNamespaceSymbol, symbols);
        }
    }

    private static void Execute(SourceProductionContext context, List<INamedTypeSymbol> symbols)
    {
        var sb = new StringBuilder();

        foreach (var symbol in symbols)
        {
            sb.AppendLine("//" + symbol.ToDisplayString());
        }

        context.AddSource("Test.g.cs", sb.ToString());
    }
}

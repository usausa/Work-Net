namespace WorkScan.SourceGenerator;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var compilationProvider = context.CompilationProvider;
        var interfaceDeclarations =
            context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is InterfaceDeclarationSyntax interfaceDeclaration &&
                                        interfaceDeclaration.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString() == "WorkScan.SourceGenerator.Attributes.ComponentSourceAttribute")),
                    static (context, _) => (InterfaceDeclarationSyntax)context.Node)
                .Where(static m => m is not null)
                .Collect();

        var providers = compilationProvider.Combine(interfaceDeclarations);
        context.RegisterSourceOutput(providers, static (spc, sourceDeclarations) =>
        {
            var compilation = sourceDeclarations.Left;

            var classes = new StringBuilder();

            foreach (var referencedAssembly in compilation.References)
            {
                if (!referencedAssembly.Display?.Contains("WorkScan") ?? false)
                {
                    continue;
                }

                if (compilation.GetAssemblyOrModuleSymbol(referencedAssembly) is IAssemblySymbol assemblySymbol)
                {
                    var classSymbols = new List<INamedTypeSymbol>();
                    CollectClasses(assemblySymbol.GlobalNamespace, classSymbols);
                    foreach (var classSymbol in classSymbols)
                    {
                        var className = classSymbol.ToDisplayString();
                        classes.AppendLine($"// {className}");
                    }
                }
            }

            spc.AddSource("Generated.cs", SourceText.From(classes.ToString(), Encoding.UTF8));
        });
    }

    private static void CollectClasses(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> classes)
    {
        foreach (var typeSymbol in namespaceSymbol.GetTypeMembers())
        {
            if (typeSymbol.TypeKind == TypeKind.Class)
            {
                classes.Add(typeSymbol);
            }
        }

        foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            CollectClasses(nestedNamespace, classes);
        }
    }
}

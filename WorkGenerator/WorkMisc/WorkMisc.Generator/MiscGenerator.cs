namespace WorkMisc.Generator;

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class MiscGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //context.RegisterPostInitializationOutput(ctx =>
        //{
        //    ctx.AddSource("Work", SourceText.From("// dummy", Encoding.UTF8));
        //});

        // Executeには渡さないようにする
        //context.CompilationProvider;

        var classProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName("WorkMisc.CustomClassAttribute", ClassPredicate, ClassTransform)
            .Collect();
        var methodProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName("WorkMisc.CustomMethodAttribute", MethodPredicate, MethodTransform)
            .Collect();

        context.RegisterImplementationSourceOutput(
            classProvider.Combine(methodProvider),
            static (context, provider) => Execute(context, provider.Left, provider.Right));
    }

    private static bool ClassPredicate(SyntaxNode syntax, CancellationToken token)
    {
        DebugLog.Log("ClassPredicate");
        return syntax is ClassDeclarationSyntax;
    }

    private static ClassModel ClassTransform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        DebugLog.Log($"ClassTransform {context.TargetSymbol.Name}");
        return new ClassModel(context.TargetSymbol.Name, GetNamespace((BaseTypeDeclarationSyntax)context.TargetNode));
    }

    private static bool MethodPredicate(SyntaxNode syntax, CancellationToken token)
    {
        DebugLog.Log("MethodPredicate");
        return syntax is MethodDeclarationSyntax;
    }

    private static MethodModel MethodTransform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        DebugLog.Log($"MethodTransform {context.TargetSymbol.Name}");
        return new MethodModel(context.TargetSymbol.Name);
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<ClassModel> classes, ImmutableArray<MethodModel> methods)
    {
        var sb = new StringBuilder();
        foreach (var @class in classes)
        {
            sb.AppendLine($"// Class: {@class.Namespace}.{@class.Name}");
        }
        foreach (var method in methods)
        {
            sb.AppendLine($"// Method: {method.Name}");
        }

        context.AddSource("Work.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));

        DebugLog.Log("Execute");
    }

    private record ClassModel(string Name, string Namespace);

    private record MethodModel(string Name);

    private static string GetNamespace(BaseTypeDeclarationSyntax syntax)
    {
        var ns = string.Empty;
        var node = syntax.Parent;
        while ((node is not null) && (node is not NamespaceDeclarationSyntax) && (node is not FileScopedNamespaceDeclarationSyntax))
        {
            node = node.Parent;
        }

        if (node is BaseNamespaceDeclarationSyntax namespaceSyntax)
        {
            ns = namespaceSyntax.Name.ToString();
            while (true)
            {
                if (namespaceSyntax.Parent is not NamespaceDeclarationSyntax parent)
                {
                    break;
                }

                ns = $"{namespaceSyntax.Name}.{ns}";
                namespaceSyntax = parent;
            }
        }

        return ns;
    }
}

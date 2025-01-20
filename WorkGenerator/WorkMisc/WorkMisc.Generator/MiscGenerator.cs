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
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("Work", SourceText.From("// dummy", Encoding.UTF8));
        });

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
        return syntax is ClassDeclarationSyntax;
    }

    private static ClassModel ClassTransform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        return new ClassModel(context.TargetSymbol.Name);
    }

    private static bool MethodPredicate(SyntaxNode syntax, CancellationToken token)
    {
        return syntax is MethodDeclarationSyntax;
    }

    private static MethodModel MethodTransform(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        return new MethodModel(context.TargetSymbol.Name);
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<ClassModel> classes, ImmutableArray<MethodModel> methods)
    {
    }

    private record ClassModel(string Name);

    private record MethodModel(string Name);
}

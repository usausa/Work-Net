namespace WorkInterceptor.Library.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class WorkInterceptorGenerator : IIncrementalGenerator
{
    private const string IBuilderFullName = "WorkInterceptor.Library.IBuilder";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generate InterceptsLocationAttribute
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource("InterceptsLocationAttribute.g.cs", SourceText.From(InterceptsLocationAttributeSource, Encoding.UTF8));
        });

        // Find all invocations of IBuilder.Execute<T>()
        var invocationProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsTargetInvocation(node),
                transform: static (context, _) => GetInvocationInfo(context))
            .Where(static x => x is not null)
            .Collect();

        context.RegisterSourceOutput(invocationProvider, static (context, invocations) =>
        {
            if (invocations.IsEmpty)
            {
                return;
            }

            GenerateInterceptors(context, invocations!);
        });
    }

    private static bool IsTargetInvocation(SyntaxNode node)
    {
        // Check if it's an invocation expression
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        // Check if it's a member access (e.g., builder.Execute<T>())
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Check if method name is Execute and has no arguments
        if (memberAccess.Name is not GenericNameSyntax genericName)
        {
            return false;
        }

        return genericName.Identifier.Text == "Execute" &&
               invocation.ArgumentList.Arguments.Count == 0;
    }

    private static InvocationInfo? GetInvocationInfo(GeneratorSyntaxContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (context.SemanticModel.GetOperation(invocation) is not IInvocationOperation operation)
        {
            return null;
        }

        var method = operation.TargetMethod;

        // Check if it's the Execute<T> method
        if (method.Name != "Execute" || method.Parameters.Length != 0 || !method.IsGenericMethod)
        {
            return null;
        }

        // Check if the method's original definition belongs to IBuilder interface
        var originalDefinition = method.OriginalDefinition;
        var containingType = originalDefinition.ContainingType;

        // Check if containing type is IBuilder or implements IBuilder
        var isIBuilder = false;
        if (containingType.ToDisplayString() == IBuilderFullName)
        {
            isIBuilder = true;
        }
        else
        {
            // Check if the type implements IBuilder
            foreach (var iface in containingType.AllInterfaces)
            {
                if (iface.ToDisplayString() == IBuilderFullName)
                {
                    isIBuilder = true;
                    break;
                }
            }
        }

        if (!isIBuilder)
        {
            return null;
        }

        // Get type argument
        var typeArgument = method.TypeArguments.FirstOrDefault();
        if (typeArgument is null)
        {
            return null;
        }

        // Get the receiver type (the type on which the method is called)
        var receiverType = operation.Instance?.Type;
        if (receiverType is null)
        {
            return null;
        }

        // Get the method name location (e.g., "Execute" in "builder.Execute<T>()")
        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        var genericName = (GenericNameSyntax)memberAccess.Name;
        var methodNameToken = genericName.Identifier;
        var location = methodNameToken.GetLocation();

        if (location.SourceTree is null)
        {
            return null;
        }

        var lineSpan = location.GetLineSpan();
        var filePath = lineSpan.Path;
        var line = lineSpan.StartLinePosition.Line + 1;
        var character = lineSpan.StartLinePosition.Character + 1;

        return new InvocationInfo(
            filePath,
            line,
            character,
            typeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            receiverType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    private static void GenerateInterceptors(SourceProductionContext context, ImmutableArray<InvocationInfo> invocations)
    {
        var builder = new StringBuilder();

        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using System.Runtime.CompilerServices;");
        builder.AppendLine("using WorkInterceptor.Library;");
        builder.AppendLine();
        builder.AppendLine("namespace WorkInterceptor.Library.Generated;");
        builder.AppendLine();
        builder.AppendLine("internal static class BuilderInterceptors");
        builder.AppendLine("{");

        for (int i = 0; i < invocations.Length; i++)
        {
            var invocation = invocations[i];
            var methodName = $"Execute_Interceptor_{i}";

            builder.AppendLine($"    [InterceptsLocation(@\"{invocation.FilePath}\", {invocation.Line}, {invocation.Character})]");
            builder.AppendLine($"    internal static void {methodName}<T>(this {invocation.ReceiverType} builder)");
            builder.AppendLine("    {");
            builder.AppendLine("        builder.Execute<T>(typeof(T));");
            builder.AppendLine("    }");

            if (i < invocations.Length - 1)
            {
                builder.AppendLine();
            }
        }

        builder.AppendLine("}");

        context.AddSource("BuilderInterceptors.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private const string InterceptsLocationAttributeSource = @"#nullable enable

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
internal sealed class InterceptsLocationAttribute : Attribute
{
    public InterceptsLocationAttribute(string filePath, int line, int character)
    {
        FilePath = filePath;
        Line = line;
        Character = character;
    }

    public string FilePath { get; }
    public int Line { get; }
    public int Character { get; }
}
";

    private sealed record InvocationInfo(string FilePath, int Line, int Character, string TypeArgument, string ReceiverType);
}

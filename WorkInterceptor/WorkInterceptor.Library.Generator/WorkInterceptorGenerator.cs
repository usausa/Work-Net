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
    private const string EnableInterceptorOptionName = "build_property.EnableWorkInterceptor";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Generate InterceptsLocationAttribute
        context.RegisterPostInitializationOutput(static context =>
        {
            context.AddSource("InterceptsLocationAttribute.g.cs", SourceText.From(InterceptsLocationAttributeSource, Encoding.UTF8));
        });

        // Read option from MSBuild property
        var enableInterceptorProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
            {
                var hasValue = provider.GlobalOptions.TryGetValue(EnableInterceptorOptionName, out var value);
                var enabled = hasValue && bool.TryParse(value, out var result) && result;
                return enabled;
            });

        // Find all invocations of IBuilder.Execute<T>()
        var invocationProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsTargetInvocation(node),
                transform: static (context, _) => GetInvocationInfo(context))
            .Where(static x => x is not null)
            .Collect();

        // Combine option and invocations
        var combined = enableInterceptorProvider.Combine(invocationProvider);

        context.RegisterSourceOutput(combined, static (context, source) =>
        {
            var (enableInterceptor, invocations) = source;

            // Only generate interceptors if option is enabled
            if (!enableInterceptor)
            {
                return;
            }

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

        // Get InterceptableLocation
        var interceptableLocation = context.SemanticModel.GetInterceptableLocation(invocation);
        if (interceptableLocation is null)
        {
            return null;
        }

        return new InvocationInfo(
            interceptableLocation,
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

            builder.AppendLine($"    // {invocation.InterceptableLocation.GetDisplayLocation()}");
            builder.AppendLine($"    [InterceptsLocation({invocation.InterceptableLocation.Version}, @\"{invocation.InterceptableLocation.Data}\")]");
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
    public InterceptsLocationAttribute(int version, string data)
    {
        Version = version;
        Data = data;
    }

    public int Version { get; }
    public string Data { get; }
}
";

    private sealed record InvocationInfo(InterceptableLocation InterceptableLocation, string TypeArgument, string ReceiverType);
}

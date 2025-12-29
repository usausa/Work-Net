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
    private const string ISubBuilderFullName = "WorkInterceptor.Library.ISubBuilder";
    private const string CommandAttributeFullName = "WorkInterceptor.Library.CommandAttribute";
    private const string BaseOptionAttributeFullName = "WorkInterceptor.Library.BaseOptionAttribute";
    private const string OptionAttributeFullName = "WorkInterceptor.Library.OptionAttribute";
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
                return provider.GlobalOptions.TryGetValue(EnableInterceptorOptionName, out var value) &&
                       bool.TryParse(value, out var enabled) &&
                       enabled;
            });

        // Find all invocations of IBuilder.Execute<T>() and ISubBuilder.ExecuteSub<T>()
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

        // Check if it's a member access (e.g., builder.Execute<T>() or subBuilder.ExecuteSub<T>())
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        // Check if method name is Execute or ExecuteSub and has no arguments
        if (memberAccess.Name is not GenericNameSyntax genericName)
        {
            return false;
        }

        var methodName = genericName.Identifier.Text;
        return (methodName == "Execute" || methodName == "ExecuteSub") &&
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

        // Check if it's the Execute<T> or ExecuteSub<T> method
        if ((method.Name != "Execute" && method.Name != "ExecuteSub") ||
            method.Parameters.Length != 0 ||
            !method.IsGenericMethod)
        {
            return null;
        }

        // Check if the method's original definition belongs to IBuilder or ISubBuilder interface
        var originalDefinition = method.OriginalDefinition;
        var containingType = originalDefinition.ContainingType;

        // Check if containing type is IBuilder/ISubBuilder or implements them
        var isTargetInterface = false;
        var targetInterfaceName = string.Empty;

        if (containingType.ToDisplayString() == IBuilderFullName)
        {
            isTargetInterface = true;
            targetInterfaceName = "IBuilder";
        }
        else if (containingType.ToDisplayString() == ISubBuilderFullName)
        {
            isTargetInterface = true;
            targetInterfaceName = "ISubBuilder";
        }
        else
        {
            // Check if the type implements IBuilder or ISubBuilder
            foreach (var iface in containingType.AllInterfaces)
            {
                var ifaceFullName = iface.ToDisplayString();
                if (ifaceFullName == IBuilderFullName)
                {
                    isTargetInterface = true;
                    //targetInterfaceName = "IBuilder";
                    break;
                }
                else if (ifaceFullName == ISubBuilderFullName)
                {
                    isTargetInterface = true;
                    //targetInterfaceName = "ISubBuilder";
                    break;
                }
            }
        }

        if (!isTargetInterface)
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

        // Extract CommandAttribute and OptionAttribute information
        var commandInfo = ExtractCommandInfo(typeArgument);

        return new InvocationInfo(
            interceptableLocation,
            typeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            typeArgument.Name,
            receiverType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            method.Name,
            commandInfo);
    }

    private static CommandInfo? ExtractCommandInfo(ITypeSymbol typeSymbol)
    {
        // Get CommandAttribute
        var commandAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == CommandAttributeFullName);

        if (commandAttr is null)
        {
            return null;
        }

        var commandName = commandAttr.ConstructorArguments.Length > 0
            ? commandAttr.ConstructorArguments[0].Value?.ToString() ?? string.Empty
            : string.Empty;

        var commandDescription = commandAttr.ConstructorArguments.Length > 1
            ? commandAttr.ConstructorArguments[1].Value?.ToString()
            : null;

        // Get properties with BaseOptionAttribute from the entire class hierarchy
        var options = new List<OptionInfo>();
        var currentType = typeSymbol;
        var hierarchyLevel = 0;

        // Build a list of types in the hierarchy from most derived to base
        var typeHierarchy = new List<(ITypeSymbol Type, int Level)>();
        while (currentType is not null)
        {
            typeHierarchy.Add((currentType, hierarchyLevel));
            currentType = currentType.BaseType;
            hierarchyLevel--;
        }

        // Reverse to process from base to derived, so we can track property order correctly
        typeHierarchy.Reverse();

        // Process each type in the hierarchy
        foreach (var (type, level) in typeHierarchy)
        {
            var members = type.GetMembers();
            var propertyIndex = 0;

            foreach (var member in members)
            {
                if (member is not IPropertySymbol property)
                {
                    continue;
                }

                // Only process properties declared in this specific type
                if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, type))
                {
                    continue;
                }

                foreach (var attr in property.GetAttributes())
                {
                    var attrClass = attr.AttributeClass;
                    if (attrClass is null)
                    {
                        continue;
                    }

                    // Check if it inherits from BaseOptionAttribute
                    var isOptionAttribute = false;
                    var isGenericOption = false;
                    var currentClass = attrClass;

                    while (currentClass is not null)
                    {
                        if (currentClass.ToDisplayString() == BaseOptionAttributeFullName)
                        {
                            isOptionAttribute = true;
                            break;
                        }
                        currentClass = currentClass.BaseType;
                    }

                    if (!isOptionAttribute)
                    {
                        continue;
                    }

                    // Check if it's the generic version
                    isGenericOption = attrClass.OriginalDefinition.ToDisplayString() == $"{OptionAttributeFullName}<T>";

                    var order = int.MaxValue;
                    var name = string.Empty;
                    var aliases = ImmutableArray<string>.Empty;

                    // Parse constructor arguments
                    if (attr.ConstructorArguments.Length >= 1)
                    {
                        // First argument is always name (or order in the 3-param version)
                        if (attr.ConstructorArguments.Length == 1)
                        {
                            // OptionAttribute(string name)
                            name = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                        }
                        else if (attr.ConstructorArguments.Length >= 2)
                        {
                            // Check if first arg is int (order) or string (name)
                            if (attr.ConstructorArguments[0].Type?.SpecialType == SpecialType.System_Int32)
                            {
                                // OptionAttribute(int order, string name, ...)
                                order = (int)(attr.ConstructorArguments[0].Value ?? int.MaxValue);
                                name = attr.ConstructorArguments[1].Value?.ToString() ?? string.Empty;

                                // Get aliases from params array (3rd argument onwards)
                                if (attr.ConstructorArguments.Length > 2 &&
                                    attr.ConstructorArguments[2].Kind == TypedConstantKind.Array)
                                {
                                    aliases = ExtractStringArray(attr.ConstructorArguments[2]);
                                }
                            }
                            else
                            {
                                // OptionAttribute(string name, params string[] aliases)
                                name = attr.ConstructorArguments[0].Value?.ToString() ?? string.Empty;

                                // Get aliases from params array (2nd argument onwards)
                                if (attr.ConstructorArguments.Length > 1 &&
                                    attr.ConstructorArguments[1].Kind == TypedConstantKind.Array)
                                {
                                    aliases = ExtractStringArray(attr.ConstructorArguments[1]);
                                }
                            }
                        }
                    }

                    // Get named properties
                    string? description = null;
                    bool required = false;

                    foreach (var namedArg in attr.NamedArguments)
                    {
                        switch (namedArg.Key)
                        {
                            case "Description":
                                description = namedArg.Value.Value?.ToString();
                                break;
                            case "Required":
                                required = namedArg.Value.Value is bool b && b;
                                break;
                        }
                    }

                    // Get Completions property from syntax
                    var completionsInfo = ExtractCompletionsPropertyFromSyntax(attr, isGenericOption ? attrClass.TypeArguments.FirstOrDefault() : null);

                    options.Add(new OptionInfo(
                        property.Name,
                        property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        order,
                        name,
                        aliases,
                        description,
                        required,
                        isGenericOption,
                        isGenericOption ? attrClass.TypeArguments.FirstOrDefault()?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null,
                        completionsInfo?.Item1,
                        completionsInfo?.Item2,
                        level,
                        propertyIndex));

                    propertyIndex++;
                    break; // Only process the first OptionAttribute found
                }
            }
        }

        return new CommandInfo(commandName, commandDescription, options.ToImmutableArray());
    }

    private static ImmutableArray<string> ExtractStringArray(TypedConstant arrayConstant)
    {
        var result = ImmutableArray.CreateBuilder<string>();

        if (arrayConstant.Kind == TypedConstantKind.Array)
        {
            foreach (var element in arrayConstant.Values)
            {
                if (element.Value is string str)
                {
                    result.Add(str);
                }
            }
        }

        return result.ToImmutable();
    }

    private static (string, ImmutableArray<string>)? ExtractCompletionsPropertyFromSyntax(AttributeData attr, ITypeSymbol? genericTypeArgument)
    {
        // Get the syntax node for the attribute
        if (attr.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax attributeSyntax)
        {
            return null;
        }

        // Find the Completions argument in the attribute syntax
        if (attributeSyntax.ArgumentList is null)
        {
            return null;
        }

        foreach (var argument in attributeSyntax.ArgumentList.Arguments)
        {
            // Check if this is a named argument with name "Completions"
            if (argument.NameEquals?.Name.Identifier.Text != "Completions")
            {
                continue;
            }

            var completions = new List<string>();

            // Check for implicit array creation: new[] { ... }
            if (argument.Expression is ImplicitArrayCreationExpressionSyntax arrayCreation)
            {
                if (arrayCreation.Initializer is not null)
                {
                    foreach (var element in arrayCreation.Initializer.Expressions)
                    {
                        // Get the text of the element as written in source
                        completions.Add(element.ToString());
                    }
                }
            }
            // Check for collection expression: [ ... ] (C# 12+)
            else if (argument.Expression is CollectionExpressionSyntax collectionExpression)
            {
                foreach (var element in collectionExpression.Elements)
                {
                    if (element is ExpressionElementSyntax expressionElement)
                    {
                        // Get the text of the element as written in source
                        completions.Add(expressionElement.Expression.ToString());
                    }
                }
            }

            if (completions.Count == 0)
            {
                return null;
            }

            var elementType = genericTypeArgument?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "string";
            return (elementType, completions.ToImmutableArray());
        }

        return null;
    }

    private static void GenerateInterceptors(SourceProductionContext context, ImmutableArray<InvocationInfo> invocations)
    {
        var builder = new StringBuilder();

        builder.AppendLine("#nullable enable");
        builder.AppendLine();
        builder.AppendLine("using System;");
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
            var methodName = $"{invocation.MethodName}_Interceptor_{i}";
            var localFunctionName = $"Action_{i}";

            builder.AppendLine($"    // {invocation.InterceptableLocation.GetDisplayLocation()}");
            builder.AppendLine($"    [InterceptsLocation({invocation.InterceptableLocation.Version}, @\"{invocation.InterceptableLocation.Data}\")]");
            builder.AppendLine($"    internal static void {methodName}(this {invocation.ReceiverType} builder)");
            builder.AppendLine("    {");

            // Generate local function
            builder.AppendLine($"        void {localFunctionName}()");
            builder.AppendLine("        {");

            if (invocation.CommandInfo is not null)
            {
                var cmdInfo = invocation.CommandInfo;
                builder.AppendLine($"            Console.WriteLine(\"Type: {invocation.TypeName}\");");
                builder.AppendLine($"            Console.WriteLine(\"Command: {cmdInfo.CommandName}\");");

                if (!string.IsNullOrEmpty(cmdInfo.CommandDescription))
                {
                    builder.AppendLine($"            Console.WriteLine(\"Description: {cmdInfo.CommandDescription}\");");
                }

                builder.AppendLine("            Console.WriteLine(\"Options:\");");

                // Sort by: 1) Order, 2) HierarchyLevel (base classes first), 3) PropertyIndex
                foreach (var option in cmdInfo.Options
                    .OrderBy(o => o.Order)
                    .ThenBy(o => o.HierarchyLevel)
                    .ThenBy(o => o.PropertyIndex))
                {
                    var attributeType = option.IsGeneric
                        ? $"OptionAttribute<{option.GenericTypeArgument}>"
                        : "OptionAttribute";

                    builder.Append($"            Console.WriteLine(\"  Property: {option.PropertyName}, Type: {option.PropertyType}, Order: {option.Order}, Name: {option.Name}");

                    if (option.Aliases.Length > 0)
                    {
                        var aliasesStr = string.Join(", ", option.Aliases.Select(a => $"\\\"{a}\\\""));
                        builder.Append($", Aliases: [{aliasesStr}]");
                    }

                    builder.Append($", Attribute: {attributeType}");

                    if (!string.IsNullOrEmpty(option.Description))
                    {
                        builder.Append($", Description: {option.Description}");
                    }

                    if (option.Required)
                    {
                        builder.Append(", Required: true");
                    }

                    if (option.Completions is not null && option.CompletionsElementType is not null)
                    {
                        // Escape double quotes in the completions for embedding in a string literal
                        var escapedCompletions = option.Completions.Value.Select(c => c.Replace("\"", "\\\""));
                        var completionsString = string.Join(", ", escapedCompletions);
                        builder.Append($", Completions ({option.CompletionsElementType}[]): [{completionsString}]");
                    }

                    builder.AppendLine("\");");
                }
            }
            else
            {
                builder.AppendLine($"            Console.WriteLine(\"Type: {invocation.TypeName} (No CommandAttribute)\");");
            }

            builder.AppendLine("        }");
            builder.AppendLine();

            // Call Execute or ExecuteSub with the concrete type and action
            builder.AppendLine($"        builder.{invocation.MethodName}<{invocation.TypeArgument}>({localFunctionName});");
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

    /// <summary>
    /// インターセプトされたメソッド呼び出しに関する情報を表します。
    /// </summary>
    /// <param name="InterceptableLocation">インターセプター属性のロケーション情報。</param>
    /// <param name="TypeArgument">ジェネリック型引数の完全修飾型名（例: "global::Develop.TestCommand"）。</param>
    /// <param name="TypeName">ジェネリック型引数の単純型名（例: "TestCommand"）。</param>
    /// <param name="ReceiverType">メソッドが呼び出されるオブジェクトの完全修飾型（例: "global::WorkInterceptor.Library.Builder"）。</param>
    /// <param name="MethodName">インターセプトされるメソッドの名前（例: "Execute" または "ExecuteSub"）。</param>
    /// <param name="InterfaceName">メソッドを定義するインターフェースの名前（例: "IBuilder" または "ISubBuilder"）。</param>
    /// <param name="CommandInfo">コマンド属性とオプションに関する情報。CommandAttributeが存在しない場合はnull。</param>
    private sealed record InvocationInfo(
        InterceptableLocation InterceptableLocation,
        string TypeArgument,
        string TypeName,
        string ReceiverType,
        string MethodName,
        CommandInfo? CommandInfo);

    /// <summary>
    /// CommandAttributeから抽出された情報を表します。
    /// </summary>
    /// <param name="CommandName">CommandAttributeで指定されたコマンド名（例: "test", "sample"）。</param>
    /// <param name="CommandDescription">コマンドの説明。指定されていない場合はnull。</param>
    /// <param name="Options">OptionAttributeが指定されたプロパティから抽出されたオプション情報のコレクション。</param>
    private sealed record CommandInfo(
        string CommandName,
        string? CommandDescription,
        ImmutableArray<OptionInfo> Options);

    /// <summary>
    /// プロパティのOptionAttributeから抽出された情報を表します。
    /// </summary>
    /// <param name="PropertyName">プロパティの名前（例: "Name", "Count"）。</param>
    /// <param name="PropertyType">プロパティの完全修飾型（例: "string", "int"）。</param>
    /// <param name="Order">オプションの表示順序。小さい値が先に表示される。デフォルトはint.MaxValue。</param>
    /// <param name="Name">オプションの主要な名前（例: "name", "count"）。</param>
    /// <param name="Aliases">オプションの別名（例: ["n"], ["c"]）。</param>
    /// <param name="Description">オプションの説明。指定されていない場合はnull。</param>
    /// <param name="Required">オプションが必須かどうか。デフォルトはfalse。</param>
    /// <param name="IsGeneric">属性がOptionAttribute&lt;T&gt;（true）かOptionAttribute（false）か。</param>
    /// <param name="GenericTypeArgument">OptionAttribute&lt;T&gt;の完全修飾型引数。非ジェネリックの場合はnull。</param>
    /// <param name="CompletionsElementType">補完値配列の要素型（例: "string", "int", "float"）。補完値がない場合はnull。</param>
    /// <param name="Completions">ソースコードに記述された通りの補完値の配列（例: ["\"debug\"", "8080", "0.5f"]）。指定されていない場合はnull。</param>
    /// <param name="HierarchyLevel">プロパティの継承レベル。0 = 対象クラス、-1 = 親クラス、-2 = 祖父クラス、以降同様。</param>
    /// <param name="PropertyIndex">宣言されたクラス内でのプロパティのインデックス。ソースコードの順序を維持するために使用される。</param>
    private sealed record OptionInfo(
        string PropertyName,
        string PropertyType,
        int Order,
        string Name,
        ImmutableArray<string> Aliases,
        string? Description,
        bool Required,
        bool IsGeneric,
        string? GenericTypeArgument,
        string? CompletionsElementType,
        ImmutableArray<string>? Completions,
        int HierarchyLevel,
        int PropertyIndex);
}

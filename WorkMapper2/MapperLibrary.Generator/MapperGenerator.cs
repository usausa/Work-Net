namespace MapperLibrary.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using MapperLibrary.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

[Generator]
public sealed class MapperGenerator : IIncrementalGenerator
{
    private const string MapperAttributeName = "MapperLibrary.MapperAttribute";
    private const string MapPropertyAttributeName = "MapperLibrary.MapPropertyAttribute";
    private const string MapIgnoreAttributeName = "MapperLibrary.MapIgnoreAttribute";
    private const string MapConstantAttributeName = "MapperLibrary.MapConstantAttribute";
    private const string BeforeMapAttributeName = "MapperLibrary.BeforeMapAttribute";
    private const string AfterMapAttributeName = "MapperLibrary.AfterMapAttribute";

    // ------------------------------------------------------------
    // Initialize
    // ------------------------------------------------------------

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                MapperAttributeName,
                static (syntax, _) => IsMethodSyntax(syntax),
                static (context, _) => GetMapperMethodModel(context))
            .Collect();

        context.RegisterImplementationSourceOutput(
            methodProvider,
            static (context, methods) => Execute(context, methods));
    }

    // ------------------------------------------------------------
    // Parser
    // ------------------------------------------------------------

    private static bool IsMethodSyntax(SyntaxNode syntax) =>
        syntax is MethodDeclarationSyntax;

    private static Result<MapperMethodModel> GetMapperMethodModel(GeneratorAttributeSyntaxContext context)
    {
        var syntax = (MethodDeclarationSyntax)context.TargetNode;
        if (context.SemanticModel.GetDeclaredSymbol(syntax) is not IMethodSymbol symbol)
        {
            return Results.Error<MapperMethodModel>(null);
        }

        // Validate method definition
        if (!symbol.IsStatic || !symbol.IsPartialDefinition)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodDefinition, syntax.GetLocation(), symbol.Name));
        }

        // Validate parameters: must have 1 or 2 parameters
        // Pattern 1: void Map(Source source, Destination destination) - 2 parameters
        // Pattern 2: Destination Map(Source source) - 1 parameter with return type
        if (symbol.Parameters.Length < 1 || symbol.Parameters.Length > 2)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.GetLocation(), symbol.Name));
        }

        var containingType = symbol.ContainingType;
        var ns = String.IsNullOrEmpty(containingType.ContainingNamespace.Name)
            ? string.Empty
            : containingType.ContainingNamespace.ToDisplayString();

        var model = new MapperMethodModel
        {
            Namespace = ns,
            ClassName = containingType.GetClassName(),
            IsValueType = containingType.IsValueType,
            MethodAccessibility = symbol.DeclaredAccessibility,
            MethodName = symbol.Name
        };

        // Get source type and parameter name
        var sourceParam = symbol.Parameters[0];
        model.SourceTypeName = sourceParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        model.SourceParameterName = sourceParam.Name;

        // Determine if void method with destination parameter or return type method
        if (symbol.Parameters.Length == 2)
        {
            // void Map(Source source, Destination destination)
            var destParam = symbol.Parameters[1];
            model.DestinationTypeName = destParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            model.DestinationParameterName = destParam.Name;
            model.ReturnsDestination = false;
        }
        else
        {
            // Destination Map(Source source)
            if (symbol.ReturnsVoid)
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.GetLocation(), symbol.Name));
            }
            model.DestinationTypeName = symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            model.DestinationParameterName = null;
            model.ReturnsDestination = true;
        }

        // Parse attributes for MapProperty, MapIgnore, MapConstant, BeforeMap, AfterMap
        ParseMappingAttributes(symbol, model);

        // Get source and destination properties
        var sourceType = symbol.Parameters[0].Type;
        var destinationType = symbol.Parameters.Length == 2 ? symbol.Parameters[1].Type : symbol.ReturnType;

        BuildPropertyMappings(sourceType, destinationType, model);

        // Build constant mappings with type information
        BuildConstantMappings(destinationType, model);

        return Results.Success(model);
    }

    private static void ParseMappingAttributes(IMethodSymbol symbol, MapperMethodModel model)
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == MapPropertyAttributeName)
            {
                // MapProperty(source, target)
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var sourceName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var targetName = attribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty;

                    // Store custom mapping (will be used in BuildPropertyMappings)
                    model.PropertyMappings.Add(new PropertyMappingModel
                    {
                        SourceName = sourceName,
                        TargetName = targetName
                    });
                }
            }
            else if (attributeName == MapIgnoreAttributeName)
            {
                // MapIgnore(target)
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == MapConstantAttributeName)
            {
                // MapConstant(target, value) with optional Expression property
                if (attribute.ConstructorArguments.Length >= 2)
                {
                    var targetName = attribute.ConstructorArguments[0].Value?.ToString() ?? string.Empty;
                    var value = attribute.ConstructorArguments[1].Value;

                    var constantMapping = new ConstantMappingModel
                    {
                        TargetName = targetName,
                        Value = FormatConstantValue(value)
                    };

                    // Check for Expression named argument
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Expression" && namedArg.Value.Value is string expression)
                        {
                            constantMapping.Expression = expression;
                        }
                    }

                    model.ConstantMappings.Add(constantMapping);

                    // Also add to ignored properties so normal mapping doesn't override
                    model.IgnoredProperties.Add(targetName);
                }
            }
            else if (attributeName == BeforeMapAttributeName)
            {
                // BeforeMap(methodName)
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    model.BeforeMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
            else if (attributeName == AfterMapAttributeName)
            {
                // AfterMap(methodName)
                if (attribute.ConstructorArguments.Length >= 1)
                {
                    model.AfterMapMethod = attribute.ConstructorArguments[0].Value?.ToString();
                }
            }
        }
    }

    private static string? FormatConstantValue(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        return value switch
        {
            string s => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"",
            char c => $"'{c}'",
            bool b => b ? "true" : "false",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            long l => $"{l}L",
            ulong ul => $"{ul}UL",
            uint ui => $"{ui}U",
            _ => value.ToString()
        };
    }

    private static void BuildConstantMappings(ITypeSymbol destinationType, MapperMethodModel model)
    {
        var destinationProperties = GetAllProperties(destinationType);

        foreach (var constantMapping in model.ConstantMappings)
        {
            var destProp = destinationProperties.FirstOrDefault(p => p.Name == constantMapping.TargetName);
            if (destProp is not null)
            {
                constantMapping.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
    }

    private static void BuildPropertyMappings(ITypeSymbol sourceType, ITypeSymbol destinationType, MapperMethodModel model)
    {
        var sourceProperties = GetAllProperties(sourceType);
        var destinationProperties = GetAllProperties(destinationType);

        // Separate custom mappings (with dot notation) from simple mappings
        var customMappings = new Dictionary<string, string>(StringComparer.Ordinal);
        var nestedMappings = new List<PropertyMappingModel>();

        foreach (var mapping in model.PropertyMappings)
        {
            // Check if this is a nested mapping (contains dots)
            if (mapping.TargetPath.Contains('.') || mapping.SourcePath.Contains('.'))
            {
                // Resolve types for nested paths
                ResolveNestedMapping(mapping, sourceType, destinationType);
                nestedMappings.Add(mapping);
            }
            else
            {
                customMappings[mapping.TargetPath] = mapping.SourcePath;
            }
        }

        // Clear and rebuild property mappings
        var mappings = new List<PropertyMappingModel>();

        // Process simple (non-nested) destination properties
        foreach (var destProp in destinationProperties)
        {
            // Skip ignored properties
            if (model.IgnoredProperties.Contains(destProp.Name))
            {
                continue;
            }

            // Skip if there's a nested mapping for this target
            if (nestedMappings.Any(m => m.TargetPath.StartsWith(destProp.Name + ".") || m.TargetPath == destProp.Name))
            {
                continue;
            }

            // Skip read-only properties
            if (destProp.SetMethod is null)
            {
                continue;
            }

            string? sourcePropPath = null;
            ITypeSymbol? sourcePropertyType = null;

            // Check for custom mapping first
            if (customMappings.TryGetValue(destProp.Name, out var customSourcePath))
            {
                sourcePropPath = customSourcePath;
                sourcePropertyType = ResolvePropertyType(sourceType, customSourcePath);
            }
            else
            {
                // Try to find matching property by name
                var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == destProp.Name);
                if (sourceProp is not null)
                {
                    sourcePropPath = sourceProp.Name;
                    sourcePropertyType = sourceProp.Type;
                }
            }

            if (sourcePropPath is not null && sourcePropertyType is not null)
            {
                var sourceTypeName = sourcePropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var destTypeName = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                mappings.Add(new PropertyMappingModel
                {
                    SourcePath = sourcePropPath,
                    TargetPath = destProp.Name,
                    SourceType = sourceTypeName,
                    TargetType = destTypeName,
                    RequiresConversion = !SymbolEqualityComparer.Default.Equals(sourcePropertyType, destProp.Type)
                });
            }
        }

        // Add nested mappings
        mappings.AddRange(nestedMappings);

        model.PropertyMappings = mappings;
    }

    private static void ResolveNestedMapping(PropertyMappingModel mapping, ITypeSymbol sourceType, ITypeSymbol destinationType)
    {
        // Resolve source type
        var sourcePropertyType = ResolvePropertyType(sourceType, mapping.SourcePath);
        if (sourcePropertyType is not null)
        {
            mapping.SourceType = sourcePropertyType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        // Resolve target type and build path segments for auto-instantiation
        var targetParts = mapping.TargetPath.Split('.');
        if (targetParts.Length > 1)
        {
            var currentType = destinationType;
            var pathBuilder = new List<string>();

            // Process all but the last segment (which is the actual property to set)
            for (var i = 0; i < targetParts.Length - 1; i++)
            {
                var part = targetParts[i];
                pathBuilder.Add(part);

                var prop = GetAllProperties(currentType).FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    mapping.TargetPathSegments.Add(new NestedPathSegment
                    {
                        Path = string.Join(".", pathBuilder),
                        TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    });
                    currentType = prop.Type;
                }
            }

            // Get the final property type
            var finalProp = GetAllProperties(currentType).FirstOrDefault(p => p.Name == targetParts[targetParts.Length - 1]);
            if (finalProp is not null)
            {
                mapping.TargetType = finalProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }
        else
        {
            // Simple target, just get its type
            var destProp = GetAllProperties(destinationType).FirstOrDefault(p => p.Name == mapping.TargetPath);
            if (destProp is not null)
            {
                mapping.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        // Determine if conversion is needed
        if (!string.IsNullOrEmpty(mapping.SourceType) && !string.IsNullOrEmpty(mapping.TargetType))
        {
            mapping.RequiresConversion = mapping.SourceType != mapping.TargetType;
        }
    }

    private static ITypeSymbol? ResolvePropertyType(ITypeSymbol type, string path)
    {
        var parts = path.Split('.');
        var currentType = type;

        foreach (var part in parts)
        {
            var prop = GetAllProperties(currentType).FirstOrDefault(p => p.Name == part);
            if (prop is null)
            {
                return null;
            }
            currentType = prop.Type;
        }

        return currentType;
    }

    private static List<IPropertySymbol> GetAllProperties(ITypeSymbol type)
    {
        var properties = new List<IPropertySymbol>();
        var currentType = type;

        while (currentType is not null)
        {
            properties.AddRange(currentType.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public));

            currentType = currentType.BaseType;
        }

        return properties;
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static void Execute(SourceProductionContext context, ImmutableArray<Result<MapperMethodModel>> methods)
    {
        foreach (var info in methods.SelectError())
        {
            context.ReportDiagnostic(info);
        }

        var builder = new SourceBuilder();
        foreach (var group in methods.SelectValue().GroupBy(static x => new { x.Namespace, x.ClassName }))
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            builder.Clear();
            BuildSource(builder, group.ToList());

            var filename = MakeFilename(group.Key.Namespace, group.Key.ClassName);
            var source = builder.ToString();
            context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static void BuildSource(SourceBuilder builder, List<MapperMethodModel> methods)
    {
        var ns = methods[0].Namespace;
        var className = methods[0].ClassName;
        var isValueType = methods[0].IsValueType;

        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // namespace
        if (!String.IsNullOrEmpty(ns))
        {
            builder.Namespace(ns);
            builder.NewLine();
        }

        // class
        builder
            .Indent()
            .Append("partial ")
            .Append(isValueType ? "struct " : "class ")
            .Append(className)
            .NewLine();
        builder.BeginScope();

        var first = true;
        foreach (var method in methods)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                builder.NewLine();
            }

            BuildMethod(builder, method);
        }

        builder.EndScope();
    }

    private static void BuildMethod(SourceBuilder builder, MapperMethodModel method)
    {
        // Method signature
        builder.Indent().Append(method.MethodAccessibility.ToText()).Append(" static partial ");

        if (method.ReturnsDestination)
        {
            // Destination Map(Source source)
            builder.Append(method.DestinationTypeName).Append(" ");
            builder.Append(method.MethodName).Append("(");
            builder.Append(method.SourceTypeName).Append(" ").Append(method.SourceParameterName);
            builder.Append(")").NewLine();
        }
        else
        {
            // void Map(Source source, Destination destination)
            builder.Append("void ");
            builder.Append(method.MethodName).Append("(");
            builder.Append(method.SourceTypeName).Append(" ").Append(method.SourceParameterName).Append(", ");
            builder.Append(method.DestinationTypeName).Append(" ").Append(method.DestinationParameterName!);
            builder.Append(")").NewLine();
        }

        builder.BeginScope();

        var destVarName = method.ReturnsDestination ? "destination" : method.DestinationParameterName!;

        // Create destination if returning
        if (method.ReturnsDestination)
        {
            builder.Indent().Append("var ").Append(destVarName).Append(" = new ").Append(method.DestinationTypeName).Append("();").NewLine();
        }

        // Call BeforeMap if specified
        if (!string.IsNullOrEmpty(method.BeforeMapMethod))
        {
            builder.Indent().Append(method.BeforeMapMethod!).Append("(").Append(method.SourceParameterName).Append(", ").Append(destVarName).Append(");").NewLine();
        }

        // Collect all nested paths that need auto-instantiation
        var nestedPathsToInstantiate = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in method.PropertyMappings)
        {
            foreach (var segment in mapping.TargetPathSegments)
            {
                if (!nestedPathsToInstantiate.ContainsKey(segment.Path))
                {
                    nestedPathsToInstantiate[segment.Path] = segment.TypeName;
                }
            }
        }

        // Generate auto-instantiation for nested paths (sorted by path length to ensure parent before child)
        foreach (var kvp in nestedPathsToInstantiate.OrderBy(x => x.Key.Count(c => c == '.')))
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(kvp.Key).Append(" ??= new ").Append(kvp.Value).Append("();").NewLine();
        }

        // Generate property mappings
        foreach (var mapping in method.PropertyMappings)
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(mapping.TargetPath).Append(" = ");

            if (mapping.RequiresConversion)
            {
                // Basic type conversion
                BuildTypeConversion(builder, mapping, method.SourceParameterName);
            }
            else
            {
                // For nested source paths, add null-forgiving operator for intermediate properties
                var sourceAccessor = BuildSourceAccessor(mapping.SourcePath, method.SourceParameterName);
                builder.Append(sourceAccessor);
            }

            builder.Append(";").NewLine();
        }

        // Generate constant mappings
        foreach (var constant in method.ConstantMappings)
        {
            builder.Indent();
            builder.Append(destVarName).Append(".").Append(constant.TargetName).Append(" = ");

            if (constant.IsExpression)
            {
                // Use expression directly
                builder.Append(constant.Expression!);
            }
            else
            {
                // Use constant value
                builder.Append(constant.Value ?? "null");
            }

            builder.Append(";").NewLine();
        }

        // Call AfterMap if specified
        if (!string.IsNullOrEmpty(method.AfterMapMethod))
        {
            builder.Indent().Append(method.AfterMapMethod!).Append("(").Append(method.SourceParameterName).Append(", ").Append(destVarName).Append(");").NewLine();
        }

        // Return destination if needed
        if (method.ReturnsDestination)
        {
            builder.Indent().Append("return ").Append(destVarName).Append(";").NewLine();
        }

        builder.EndScope();
    }

    private static string BuildSourceAccessor(string sourcePath, string sourceParamName)
    {
        // For nested paths, add null-forgiving operator (!) to intermediate properties
        if (!sourcePath.Contains('.'))
        {
            return $"{sourceParamName}.{sourcePath}";
        }

        var parts = sourcePath.Split('.');
        var result = new StringBuilder();
        result.Append(sourceParamName);

        for (var i = 0; i < parts.Length; i++)
        {
            result.Append('.');
            result.Append(parts[i]);

            // Add null-forgiving operator for all but the last segment
            if (i < parts.Length - 1)
            {
                result.Append('!');
            }
        }

        return result.ToString();
    }

    private static void BuildTypeConversion(SourceBuilder builder, PropertyMappingModel mapping, string sourceParamName)
    {
        var sourceExpr = BuildSourceAccessor(mapping.SourcePath, sourceParamName);

        // Normalize type names for comparison
        var sourceType = NormalizeTypeName(mapping.SourceType);
        var targetType = NormalizeTypeName(mapping.TargetType);

        // Same normalized type - no conversion needed
        if (sourceType == targetType)
        {
            builder.Append(sourceExpr);
            return;
        }

        // Convert to string
        if (targetType == "string")
        {
            if (IsNullableType(mapping.SourceType))
            {
                builder.Append(sourceExpr).Append("?.ToString()");
            }
            else
            {
                builder.Append(sourceExpr).Append(".ToString()");
            }
            return;
        }

        // Convert from string to other types
        if (sourceType == "string")
        {
            var parseMethod = GetParseMethod(targetType);
            if (parseMethod is not null)
            {
                builder.Append(parseMethod).Append("(").Append(sourceExpr).Append(")");
                return;
            }
        }

        // Numeric conversions
        if (IsNumericType(sourceType) && IsNumericType(targetType))
        {
            // Use explicit cast for numeric conversions
            builder.Append("(").Append(mapping.TargetType).Append(")").Append(sourceExpr);
            return;
        }

        // DateTime conversions
        if (sourceType == "DateTime" && targetType == "DateTimeOffset")
        {
            builder.Append("new global::System.DateTimeOffset(").Append(sourceExpr).Append(")");
            return;
        }
        if (sourceType == "DateTimeOffset" && targetType == "DateTime")
        {
            builder.Append(sourceExpr).Append(".DateTime");
            return;
        }

        // DateOnly/TimeOnly conversions (if available)
        if (sourceType == "DateTime" && targetType == "DateOnly")
        {
            builder.Append("global::System.DateOnly.FromDateTime(").Append(sourceExpr).Append(")");
            return;
        }
        if (sourceType == "DateTime" && targetType == "TimeOnly")
        {
            builder.Append("global::System.TimeOnly.FromDateTime(").Append(sourceExpr).Append(")");
            return;
        }

        // Guid conversions
        if (sourceType == "string" && targetType == "Guid")
        {
            builder.Append("global::System.Guid.Parse(").Append(sourceExpr).Append(")");
            return;
        }
        if (sourceType == "Guid" && targetType == "string")
        {
            builder.Append(sourceExpr).Append(".ToString()");
            return;
        }

        // Enum conversions
        if (sourceType == "string")
        {
            // Assume target might be an enum - use Enum.Parse
            builder.Append("(").Append(mapping.TargetType).Append(")global::System.Enum.Parse(typeof(").Append(mapping.TargetType).Append("), ").Append(sourceExpr).Append(")");
            return;
        }

        // Fallback: try explicit cast
        builder.Append("(").Append(mapping.TargetType).Append(")").Append(sourceExpr);
    }

    private static string NormalizeTypeName(string typeName)
    {
        // Remove global:: prefix and System. prefix for comparison
        var normalized = typeName
            .Replace("global::", "")
            .Replace("System.", "");

        // Handle nullable types
        if (normalized.EndsWith("?"))
        {
            normalized = normalized.TrimEnd('?');
        }
        if (normalized.StartsWith("Nullable<") && normalized.EndsWith(">"))
        {
            normalized = normalized.Substring(9, normalized.Length - 10);
        }

        return normalized;
    }

    private static bool IsNullableType(string typeName)
    {
        return typeName.EndsWith("?") ||
               typeName.Contains("Nullable<") ||
               typeName.Contains("Nullable`1");
    }

    private static bool IsNumericType(string normalizedType)
    {
        return normalizedType is "int" or "Int32"
            or "long" or "Int64"
            or "short" or "Int16"
            or "byte" or "Byte"
            or "sbyte" or "SByte"
            or "uint" or "UInt32"
            or "ulong" or "UInt64"
            or "ushort" or "UInt16"
            or "float" or "Single"
            or "double" or "Double"
            or "decimal" or "Decimal";
    }

    private static string? GetParseMethod(string normalizedTargetType)
    {
        return normalizedTargetType switch
        {
            "int" or "Int32" => "int.Parse",
            "long" or "Int64" => "long.Parse",
            "short" or "Int16" => "short.Parse",
            "byte" or "Byte" => "byte.Parse",
            "sbyte" or "SByte" => "sbyte.Parse",
            "uint" or "UInt32" => "uint.Parse",
            "ulong" or "UInt64" => "ulong.Parse",
            "ushort" or "UInt16" => "ushort.Parse",
            "float" or "Single" => "float.Parse",
            "double" or "Double" => "double.Parse",
            "decimal" or "Decimal" => "decimal.Parse",
            "bool" or "Boolean" => "bool.Parse",
            "DateTime" => "global::System.DateTime.Parse",
            "DateTimeOffset" => "global::System.DateTimeOffset.Parse",
            "DateOnly" => "global::System.DateOnly.Parse",
            "TimeOnly" => "global::System.TimeOnly.Parse",
            "TimeSpan" => "global::System.TimeSpan.Parse",
            "Guid" => "global::System.Guid.Parse",
            _ => null
        };
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static string MakeFilename(string ns, string className)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(className.Replace('<', '[').Replace('>', ']'));
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}

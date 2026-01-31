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

        // Validate parameters:
        // Pattern 1: void Map(Source source, Destination destination, ...customParams) - at least 2 parameters
        // Pattern 2: Destination Map(Source source, ...customParams) - at least 1 parameter with return type
        if (symbol.Parameters.Length < 1)
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

        // Get source type and parameter name (always first parameter)
        var sourceParam = symbol.Parameters[0];
        model.SourceTypeName = sourceParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        model.SourceParameterName = sourceParam.Name;

        ITypeSymbol destinationType;
        var customParamStartIndex = 0;

        // Determine if void method with destination parameter or return type method
        if (symbol.ReturnsVoid)
        {
            // void Map(Source source, Destination destination, ...customParams)
            if (symbol.Parameters.Length < 2)
            {
                return Results.Error<MapperMethodModel>(new DiagnosticInfo(Diagnostics.InvalidMethodParameter, syntax.GetLocation(), symbol.Name));
            }

            var destParam = symbol.Parameters[1];
            model.DestinationTypeName = destParam.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            model.DestinationParameterName = destParam.Name;
            model.ReturnsDestination = false;
            destinationType = destParam.Type;
            customParamStartIndex = 2;
        }
        else
        {
            // Destination Map(Source source, ...customParams)
            model.DestinationTypeName = symbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            model.DestinationParameterName = null;
            model.ReturnsDestination = true;
            destinationType = symbol.ReturnType;
            customParamStartIndex = 1;
        }

        // Extract custom parameters
        var customParameters = new List<CustomParameterModel>();
        for (var i = customParamStartIndex; i < symbol.Parameters.Length; i++)
        {
            var param = symbol.Parameters[i];
            customParameters.Add(new CustomParameterModel
            {
                Name = param.Name,
                TypeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            });
        }

        // Check for duplicate custom parameter types
        var duplicateType = customParameters
            .GroupBy(p => p.TypeName)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateType is not null)
        {
            return Results.Error<MapperMethodModel>(new DiagnosticInfo(
                Diagnostics.DuplicateCustomParameterType,
                syntax.GetLocation(),
                $"{duplicateType.Key}, {symbol.Name}"));
        }

        model.CustomParameters = customParameters;

        // Parse attributes for MapProperty, MapIgnore, MapConstant, BeforeMap, AfterMap
        ParseMappingAttributes(symbol, model);

        // Validate BeforeMap/AfterMap signatures if specified
        var validationError = ValidateCallbackMethods(symbol, model, syntax);
        if (validationError is not null)
        {
            return Results.Error<MapperMethodModel>(validationError);
        }

        // Get source and destination properties
        var sourceType = symbol.Parameters[0].Type;

        BuildPropertyMappings(sourceType, destinationType, model);

        // Validate Converter methods if specified
        var converterError = ValidateConverterMethods(symbol, model, syntax);
        if (converterError is not null)
        {
            return Results.Error<MapperMethodModel>(converterError);
        }

        // Build constant mappings with type information
        BuildConstantMappings(destinationType, model);

        return Results.Success(model);
    }

    private static DiagnosticInfo? ValidateConverterMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        foreach (var mapping in model.PropertyMappings)
        {
            if (string.IsNullOrEmpty(mapping.ConverterMethod))
            {
                continue;
            }

            var converterMethods = containingType.GetMembers(mapping.ConverterMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingConverterMethod(converterMethods, mapping, model);
            if (matchResult == ConverterMatchResult.NoMatch)
            {
                return new DiagnosticInfo(
                    Diagnostics.InvalidConverterSignature,
                    syntax.GetLocation(),
                    $"{mapping.ConverterMethod}, {mapping.SourcePath} -> {mapping.TargetPath}");
            }

            mapping.ConverterAcceptsCustomParameters = matchResult == ConverterMatchResult.MatchWithCustomParams;
        }

        return null;
    }

    private enum ConverterMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams
    }

    private static ConverterMatchResult FindMatchingConverterMethod(List<IMethodSymbol> candidates, PropertyMappingModel mapping, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;
        var sourceType = mapping.SourceType;

        foreach (var method in candidates)
        {
            // Return type must match target type (or be assignable)
            var returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            // Check for match with custom parameters: (SourceType, customParams...)
            if (model.CustomParameters.Count > 0 &&
                method.Parameters.Length == 1 + model.CustomParameters.Count)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                var customParamsMatch = true;
                for (var i = 0; i < model.CustomParameters.Count; i++)
                {
                    if (method.Parameters[i + 1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != model.CustomParameters[i].TypeName)
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (sourceMatch && customParamsMatch)
                {
                    hasMatchWithCustomParams = true;
                }
            }

            // Check for match without custom parameters: (SourceType)
            if (method.Parameters.Length == 1)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceType;

                if (sourceMatch)
                {
                    hasMatchWithoutCustomParams = true;
                }
            }
        }

        // Prefer match with custom parameters
        if (hasMatchWithCustomParams)
        {
            return ConverterMatchResult.MatchWithCustomParams;
        }

        if (hasMatchWithoutCustomParams)
        {
            return ConverterMatchResult.MatchWithoutCustomParams;
        }

        return ConverterMatchResult.NoMatch;
    }

    private static DiagnosticInfo? ValidateCallbackMethods(IMethodSymbol mapperMethod, MapperMethodModel model, MethodDeclarationSyntax syntax)
    {
        var containingType = mapperMethod.ContainingType;

        // Validate BeforeMap
        if (!string.IsNullOrEmpty(model.BeforeMapMethod))
        {
            var beforeMapMethods = containingType.GetMembers(model.BeforeMapMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingCallbackMethod(beforeMapMethods, model);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidBeforeMapSignature, syntax.GetLocation(), $"{model.BeforeMapMethod!}, {mapperMethod.Name}");
            }
            model.BeforeMapAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams;
        }

        // Validate AfterMap
        if (!string.IsNullOrEmpty(model.AfterMapMethod))
        {
            var afterMapMethods = containingType.GetMembers(model.AfterMapMethod!)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic)
                .ToList();

            var matchResult = FindMatchingCallbackMethod(afterMapMethods, model);
            if (matchResult == CallbackMatchResult.NoMatch)
            {
                return new DiagnosticInfo(Diagnostics.InvalidAfterMapSignature, syntax.GetLocation(), $"{model.AfterMapMethod!}, {mapperMethod.Name}");
            }
            model.AfterMapAcceptsCustomParameters = matchResult == CallbackMatchResult.MatchWithCustomParams;
        }

        return null;
    }

    private enum CallbackMatchResult
    {
        NoMatch,
        MatchWithoutCustomParams,
        MatchWithCustomParams
    }

    private static CallbackMatchResult FindMatchingCallbackMethod(List<IMethodSymbol> candidates, MapperMethodModel model)
    {
        var hasMatchWithCustomParams = false;
        var hasMatchWithoutCustomParams = false;

        foreach (var method in candidates)
        {
            // Check for match with custom parameters: (Source, Destination, customParams...)
            if (model.CustomParameters.Count > 0 &&
                method.Parameters.Length == 2 + model.CustomParameters.Count)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.SourceTypeName;
                var destMatch = method.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.DestinationTypeName;

                var customParamsMatch = true;
                for (var i = 0; i < model.CustomParameters.Count; i++)
                {
                    if (method.Parameters[i + 2].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != model.CustomParameters[i].TypeName)
                    {
                        customParamsMatch = false;
                        break;
                    }
                }

                if (sourceMatch && destMatch && customParamsMatch)
                {
                    hasMatchWithCustomParams = true;
                }
            }

            // Check for match without custom parameters: (Source, Destination)
            if (method.Parameters.Length == 2)
            {
                var sourceMatch = method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.SourceTypeName;
                var destMatch = method.Parameters[1].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == model.DestinationTypeName;

                if (sourceMatch && destMatch)
                {
                    hasMatchWithoutCustomParams = true;
                }
            }
        }

        // Prefer match with custom parameters
        if (hasMatchWithCustomParams)
        {
            return CallbackMatchResult.MatchWithCustomParams;
        }

        if (hasMatchWithoutCustomParams)
        {
            return CallbackMatchResult.MatchWithoutCustomParams;
        }

        return CallbackMatchResult.NoMatch;
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

                    var mapping = new PropertyMappingModel
                    {
                        SourceName = sourceName,
                        TargetName = targetName
                    };

                    // Check for Converter named argument
                    foreach (var namedArg in attribute.NamedArguments)
                    {
                        if (namedArg.Key == "Converter" && namedArg.Value.Value is string converter)
                        {
                            mapping.ConverterMethod = converter;
                        }
                    }

                    // Store custom mapping (will be used in BuildPropertyMappings)
                    model.PropertyMappings.Add(mapping);
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

        // Preserve original mappings with Converter info
        var originalMappings = model.PropertyMappings.ToDictionary(m => m.TargetPath, m => m);

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
            if (nestedMappings.Any(m => m.TargetPath.StartsWith(destProp.Name + ".", StringComparison.Ordinal) || m.TargetPath == destProp.Name))
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
            string? converterMethod = null;

            // Check for custom mapping first
            if (customMappings.TryGetValue(destProp.Name, out var customSourcePath))
            {
                sourcePropPath = customSourcePath;
                sourcePropertyType = ResolvePropertyType(sourceType, customSourcePath);

                // Preserve converter info from original mapping
                if (originalMappings.TryGetValue(destProp.Name, out var originalMapping))
                {
                    converterMethod = originalMapping.ConverterMethod;
                }
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

                var mapping = new PropertyMappingModel
                {
                    SourcePath = sourcePropPath,
                    TargetPath = destProp.Name,
                    SourceType = sourceTypeName,
                    TargetType = destTypeName,
                    RequiresConversion = !SymbolEqualityComparer.Default.Equals(sourcePropertyType, destProp.Type),
                    IsSourceNullable = IsNullableSymbol(sourcePropertyType),
                    IsTargetNullable = IsNullableSymbol(destProp.Type),
                    ConverterMethod = converterMethod
                };

                mappings.Add(mapping);
            }
        }

        // Add nested mappings
        mappings.AddRange(nestedMappings);

        model.PropertyMappings = mappings;
    }

    private static bool IsNullableSymbol(ITypeSymbol type)
    {
        // Check for nullable reference type
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        // Check for Nullable<T> value type
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }

    private static void ResolveNestedMapping(PropertyMappingModel mapping, ITypeSymbol sourceType, ITypeSymbol destinationType)
    {
        // Resolve source path segments and check for nullable intermediate types
        var sourceParts = mapping.SourcePath.Split('.');
        if (sourceParts.Length > 1)
        {
            var currentType = sourceType;
            var pathBuilder = new List<string>();

            // Process all but the last segment
            for (var i = 0; i < sourceParts.Length - 1; i++)
            {
                var part = sourceParts[i];
                pathBuilder.Add(part);

                var prop = GetAllProperties(currentType).FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    var isNullable = IsNullableSymbol(prop.Type);
                    mapping.SourcePathSegments.Add(new NestedPathSegment
                    {
                        Path = string.Join(".", pathBuilder),
                        TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        IsNullable = isNullable
                    });
                    currentType = prop.Type;
                }
            }

            // Get the final property type
            var finalSourceProp = GetAllProperties(currentType).FirstOrDefault(p => p.Name == sourceParts[sourceParts.Length - 1]);
            if (finalSourceProp is not null)
            {
                mapping.SourceType = finalSourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsSourceNullable = IsNullableSymbol(finalSourceProp.Type);
            }
        }
        else
        {
            // Simple source path
            var sourceProp = GetAllProperties(sourceType).FirstOrDefault(p => p.Name == mapping.SourcePath);
            if (sourceProp is not null)
            {
                mapping.SourceType = sourceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsSourceNullable = IsNullableSymbol(sourceProp.Type);
            }
        }

        // Resolve target type and build path segments for auto-instantiation
        var targetParts = mapping.TargetPath.Split('.');
        if (targetParts.Length > 1)
        {
            var currentTargetType = destinationType;
            var pathBuilder = new List<string>();

            // Process all but the last segment (which is the actual property to set)
            for (var i = 0; i < targetParts.Length - 1; i++)
            {
                var part = targetParts[i];
                pathBuilder.Add(part);

                var prop = GetAllProperties(currentTargetType).FirstOrDefault(p => p.Name == part);
                if (prop is not null)
                {
                    mapping.TargetPathSegments.Add(new NestedPathSegment
                    {
                        Path = string.Join(".", pathBuilder),
                        TypeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    });
                    currentTargetType = prop.Type;
                }
            }

            // Get the final property type
            var finalProp = GetAllProperties(currentTargetType).FirstOrDefault(p => p.Name == targetParts[targetParts.Length - 1]);
            if (finalProp is not null)
            {
                mapping.TargetType = finalProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsTargetNullable = IsNullableSymbol(finalProp.Type);
            }
        }
        else
        {
            // Simple target, just get its type
            var destProp = GetAllProperties(destinationType).FirstOrDefault(p => p.Name == mapping.TargetPath);
            if (destProp is not null)
            {
                mapping.TargetType = destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                mapping.IsTargetNullable = IsNullableSymbol(destProp.Type);
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
            // Destination Map(Source source, ...customParams)
            builder.Append(method.DestinationTypeName).Append(" ");
            builder.Append(method.MethodName).Append("(");
            builder.Append(method.SourceTypeName).Append(" ").Append(method.SourceParameterName);

            // Add custom parameters
            foreach (var customParam in method.CustomParameters)
            {
                builder.Append(", ").Append(customParam.TypeName).Append(" ").Append(customParam.Name);
            }

            builder.Append(")").NewLine();
        }
        else
        {
            // void Map(Source source, Destination destination, ...customParams)
            builder.Append("void ");
            builder.Append(method.MethodName).Append("(");
            builder.Append(method.SourceTypeName).Append(" ").Append(method.SourceParameterName).Append(", ");
            builder.Append(method.DestinationTypeName).Append(" ").Append(method.DestinationParameterName!);

            // Add custom parameters
            foreach (var customParam in method.CustomParameters)
            {
                builder.Append(", ").Append(customParam.TypeName).Append(" ").Append(customParam.Name);
            }

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
            builder.Indent().Append(method.BeforeMapMethod!).Append("(").Append(method.SourceParameterName).Append(", ").Append(destVarName);
            if (method.BeforeMapAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }
            builder.Append(");").NewLine();
        }

        // Collect all nested paths that need auto-instantiation (excluding those with nullable source paths)
        var nestedPathsToInstantiate = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var mapping in method.PropertyMappings)
        {
            // Skip auto-instantiation if source has nullable path segments
            if (mapping.SourcePathSegments.Any(s => s.IsNullable))
            {
                continue;
            }

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

        // Group mappings by whether they require null check
        var mappingsWithoutNullCheck = method.PropertyMappings.Where(m => !m.RequiresNullCheck).ToList();
        var mappingsWithNullCheck = method.PropertyMappings.Where(m => m.RequiresNullCheck).ToList();

        // Generate property mappings without null check
        foreach (var mapping in mappingsWithoutNullCheck)
        {
            BuildPropertyAssignment(builder, mapping, method.SourceParameterName, destVarName, method);
        }

        // Generate property mappings with null check (grouped by source null check condition)
        var groupedByNullCheck = mappingsWithNullCheck
            .GroupBy(m => GetNullCheckCondition(m, method.SourceParameterName))
            .Where(g => !string.IsNullOrEmpty(g.Key));

        foreach (var group in groupedByNullCheck)
        {
            builder.Indent().Append("if (").Append(group.Key).Append(")").NewLine();
            builder.BeginScope();

            // Generate auto-instantiation for these mappings' target paths
            var groupNestedPaths = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var mapping in group)
            {
                foreach (var segment in mapping.TargetPathSegments)
                {
                    if (!groupNestedPaths.ContainsKey(segment.Path))
                    {
                        groupNestedPaths[segment.Path] = segment.TypeName;
                    }
                }
            }

            foreach (var kvp in groupNestedPaths.OrderBy(x => x.Key.Count(c => c == '.')))
            {
                builder.Indent();
                builder.Append(destVarName).Append(".").Append(kvp.Key).Append(" ??= new ").Append(kvp.Value).Append("();").NewLine();
            }

            foreach (var mapping in group)
            {
                BuildPropertyAssignment(builder, mapping, method.SourceParameterName, destVarName, method, nullChecked: true);
            }

            builder.EndScope();
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
            builder.Indent().Append(method.AfterMapMethod!).Append("(").Append(method.SourceParameterName).Append(", ").Append(destVarName);
            if (method.AfterMapAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }
            builder.Append(");").NewLine();
        }

        // Return destination if needed
        if (method.ReturnsDestination)
        {
            builder.Indent().Append("return ").Append(destVarName).Append(";").NewLine();
        }

        builder.EndScope();
    }

    private static void BuildPropertyAssignment(SourceBuilder builder, PropertyMappingModel mapping, string sourceParamName, string destVarName, MapperMethodModel method, bool nullChecked = false)
    {
        builder.Indent();
        builder.Append(destVarName).Append(".").Append(mapping.TargetPath).Append(" = ");

        // Use custom converter if specified
        if (mapping.HasConverter)
        {
            var sourceAccessor = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);
            builder.Append(mapping.ConverterMethod!).Append("(").Append(sourceAccessor);

            // Add custom parameters if converter accepts them
            if (mapping.ConverterAcceptsCustomParameters)
            {
                foreach (var customParam in method.CustomParameters)
                {
                    builder.Append(", ").Append(customParam.Name);
                }
            }

            builder.Append(")");
        }
        else if (mapping.RequiresConversion)
        {
            BuildTypeConversion(builder, mapping, sourceParamName, nullChecked);
        }
        else
        {
            var sourceAccessor = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);
            builder.Append(sourceAccessor);

            // For nullable to non-nullable terminal element, add null-forgiving operator
            if (mapping.RequiresNullCoalescing)
            {
                builder.Append("!");
            }
        }

        builder.Append(";").NewLine();
    }

    private static string GetNullCheckCondition(PropertyMappingModel mapping, string sourceParamName)
    {
        var conditions = new List<string>();

        // Check for nullable source path segments (intermediate elements only)
        if (mapping.SourcePathSegments.Count > 0)
        {
            var pathBuilder = new StringBuilder();
            pathBuilder.Append(sourceParamName);

            foreach (var segment in mapping.SourcePathSegments)
            {
                pathBuilder.Append('.').Append(segment.Path.Split('.').Last());
                if (segment.IsNullable)
                {
                    conditions.Add($"{pathBuilder} is not null");
                }
            }
        }

        // Note: Terminal nullable to non-nullable is handled by RequiresNullCoalescing, not here

        return string.Join(" && ", conditions);
    }

    private static string BuildSourceAccessor(string sourcePath, string sourceParamName, bool nullChecked = false)
    {
        // For simple paths, just return the accessor
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

            // Add null-forgiving operator for intermediate segments only if not null checked
            if (i < parts.Length - 1 && !nullChecked)
            {
                result.Append('!');
            }
        }

        return result.ToString();
    }

    private static void BuildTypeConversion(SourceBuilder builder, PropertyMappingModel mapping, string sourceParamName, bool nullChecked = false)
    {
        var sourceExpr = BuildSourceAccessor(mapping.SourcePath, sourceParamName, nullChecked);
        var requiresNullCoalescing = mapping.RequiresNullCoalescing;

        // Normalize type names for comparison
        var sourceType = NormalizeTypeName(mapping.SourceType);
        var targetType = NormalizeTypeName(mapping.TargetType);

        // Same normalized type - no conversion needed (but may need null handling)
        if (sourceType == targetType)
        {
            builder.Append(sourceExpr);
            if (requiresNullCoalescing)
            {
                builder.Append("!");
            }
            return;
        }

        // Convert to string
        if (targetType == "string")
        {
            if (IsNullableType(mapping.SourceType))
            {
                // int? -> string: source.Value?.ToString() ?? default!
                builder.Append(sourceExpr).Append("?.ToString()");
                if (!mapping.IsTargetNullable)
                {
                    builder.Append(" ?? default!");
                }
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
                if (IsNullableType(mapping.SourceType) && !mapping.IsTargetNullable)
                {
                    // string? -> int: source.Value is not null ? int.Parse(source.Value) : default
                    builder.Append(sourceExpr).Append(" is not null ? ")
                           .Append(parseMethod).Append("(").Append(sourceExpr).Append(") : default");
                }
                else
                {
                    builder.Append(parseMethod).Append("(").Append(sourceExpr).Append(")");
                }
                return;
            }
        }

        // Numeric conversions (including nullable)
        if (IsNumericType(sourceType) && IsNumericType(targetType))
        {
            if (IsNullableType(mapping.SourceType))
            {
                if (mapping.IsTargetNullable)
                {
                    // int? -> long?: (long?)source.Value
                    builder.Append("(").Append(mapping.TargetType).Append(")").Append(sourceExpr);
                }
                else
                {
                    // int? -> long: source.Value ?? default
                    builder.Append("(").Append(mapping.TargetType).Append(")(").Append(sourceExpr).Append(" ?? default)");
                }
            }
            else
            {
                // int -> long: (long)source.Value
                builder.Append("(").Append(mapping.TargetType).Append(")").Append(sourceExpr);
            }
            return;
        }

        // DateTime conversions
        if (sourceType == "DateTime" && targetType == "DateTimeOffset")
        {
            builder.Append("new global::System.DateTimeOffset(").Append(sourceExpr);
            if (IsNullableType(mapping.SourceType))
            {
                builder.Append(" ?? default");
            }
            builder.Append(")");
            return;
        }
        if (sourceType == "DateTimeOffset" && targetType == "DateTime")
        {
            if (IsNullableType(mapping.SourceType))
            {
                builder.Append(sourceExpr).Append("?.DateTime ?? default");
            }
            else
            {
                builder.Append(sourceExpr).Append(".DateTime");
            }
            return;
        }

        // DateOnly/TimeOnly conversions
        if (sourceType == "DateTime" && targetType == "DateOnly")
        {
            builder.Append("global::System.DateOnly.FromDateTime(").Append(sourceExpr);
            if (IsNullableType(mapping.SourceType))
            {
                builder.Append(" ?? default");
            }
            builder.Append(")");
            return;
        }
        if (sourceType == "DateTime" && targetType == "TimeOnly")
        {
            builder.Append("global::System.TimeOnly.FromDateTime(").Append(sourceExpr);
            if (IsNullableType(mapping.SourceType))
            {
                builder.Append(" ?? default");
            }
            builder.Append(")");
            return;
        }

        // Guid conversions
        if (sourceType == "string" && targetType == "Guid")
        {
            if (IsNullableType(mapping.SourceType) && !mapping.IsTargetNullable)
            {
                builder.Append(sourceExpr).Append(" is not null ? global::System.Guid.Parse(").Append(sourceExpr).Append(") : default");
            }
            else
            {
                builder.Append("global::System.Guid.Parse(").Append(sourceExpr).Append(")");
            }
            return;
        }
        if (sourceType == "Guid" && targetType == "string")
        {
            if (IsNullableType(mapping.SourceType))
            {
                builder.Append(sourceExpr).Append("?.ToString()");
                if (!mapping.IsTargetNullable)
                {
                    builder.Append(" ?? default!");
                }
            }
            else
            {
                builder.Append(sourceExpr).Append(".ToString()");
            }
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
        if (requiresNullCoalescing && IsNullableType(mapping.SourceType))
        {
            // Unlikely to reach here, but add null handling just in case
        }
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

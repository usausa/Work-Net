namespace MapperLibrary.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidMethodDefinition { get; } = new(
        id: "ML0001",
        title: "Invalid method definition",
        messageFormat: "Mapper method must be static partial. method=[{0}]",
        category: "MapperLibrary",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidMethodParameter { get; } = new(
        id: "ML0002",
        title: "Invalid method parameter",
        messageFormat: "Mapper method must have at least 1 parameter (for return type) or 2 parameters (for void). method=[{0}]",
        category: "MapperLibrary",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DuplicateCustomParameterType { get; } = new(
        id: "ML0003",
        title: "Duplicate custom parameter type",
        messageFormat: "Custom parameters must have unique types. [{0}]",
        category: "MapperLibrary",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidBeforeMapSignature { get; } = new(
        id: "ML0004",
        title: "Invalid BeforeMap method signature",
        messageFormat: "BeforeMap method signature does not match. Expected (Source, Destination) or (Source, Destination, customParams...). [{0}]",
        category: "MapperLibrary",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidAfterMapSignature { get; } = new(
        id: "ML0005",
        title: "Invalid AfterMap method signature",
        messageFormat: "AfterMap method signature does not match. Expected (Source, Destination) or (Source, Destination, customParams...). [{0}]",
        category: "MapperLibrary",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidConverterSignature { get; } = new(
        id: "ML0006",
        title: "Invalid Converter method signature",
        messageFormat: "Converter method signature does not match. Expected (SourceType) or (SourceType, customParams...) returning TargetType. [{0}]",
        category: "MapperLibrary",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}



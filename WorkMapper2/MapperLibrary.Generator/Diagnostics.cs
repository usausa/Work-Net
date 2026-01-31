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
        messageFormat: "Mapper method must have 1 or 2 parameters. method=[{0}]",
        category: "MapperLibrary",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}


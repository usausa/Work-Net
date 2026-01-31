namespace Develop;

using MapperLibrary;

internal static class Program
{
    public static void Main()
    {
        // Test basic mapping with void method
        var source = new Source { Value1 = 42 };
        var destination = new Destination();
        ObjectMapper.Map(source, destination);
        Console.WriteLine($"Void method: source.Value1={source.Value1}, destination.Value1={destination.Value1}");

        // Test basic mapping with return type
        var source2 = new Source { Value1 = 100 };
        var destination2 = ObjectMapper.Map(source2);
        Console.WriteLine($"Return method: source.Value1={source2.Value1}, destination.Value1={destination2.Value1}");

        // Test different property names
        var diffSource = new DiffSource { SourceId = 123, SourceName = "Test" };
        var diffDest = new DiffDestination();
        ObjectMapper.Map(diffSource, diffDest);
        Console.WriteLine($"Different names: SourceId={diffSource.SourceId} -> DestId={diffDest.DestId}");

        // Test ignore property
        var ignoreSource = new IgnoreSource { Id = 1, Name = "Public", Secret = "TopSecret" };
        var ignoreDest = new IgnoreDestination { Secret = "Original" };
        ObjectMapper.Map(ignoreSource, ignoreDest);
        Console.WriteLine($"Ignore: Secret should be 'Original': {ignoreDest.Secret}");

        Console.WriteLine("All tests passed!");
    }
}

internal static partial class ObjectMapper
{
    [Mapper]
    public static partial void Map(Source source, Destination destination);

    [Mapper]
    public static partial Destination Map(Source source);

    [Mapper]
    [MapProperty("SourceId", "DestId")]
    [MapProperty("SourceName", "DestName")]
    public static partial void Map(DiffSource source, DiffDestination destination);

    [Mapper]
    [MapIgnore("Secret")]
    public static partial void Map(IgnoreSource source, IgnoreDestination destination);
}

internal sealed class Source
{
    public int Value1 { get; set; }
}

internal sealed class Destination
{
    public string Value1 { get; set; } = default!;
}

internal sealed class DiffSource
{
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
}

internal sealed class DiffDestination
{
    public int DestId { get; set; }
    public string DestName { get; set; } = string.Empty;
}

internal sealed class IgnoreSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

internal sealed class IgnoreDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}

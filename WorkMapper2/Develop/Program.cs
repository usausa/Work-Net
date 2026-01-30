namespace Develop;

using MapperLibrary;

internal static class Program
{
    public static void Main()
    {
        var source = new Source();
        var destination = new Destination();

        ObjectMapper.Map(source, destination);
        _ = ObjectMapper.Map(source);
    }
}

internal static partial class ObjectMapper
{
    //[Mapper]
    //public static partial void Map(Source source, Destination destination);

    //[Mapper]
    //public static partial Destination Map(Source source);

    public static void Map(Source source, Destination destination)
    {
        // Dummy
    }

    public static Destination Map(Source source)
    {
        // Dummy
        return new Destination();
    }
}

internal sealed class Source
{
    public int Value1 { get; set; }
}

internal sealed class Destination
{
    public string Value1 { get; set; } = default!;
}

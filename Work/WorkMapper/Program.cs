namespace WorkMapper;

using Mapster;

using MapsterMapper;

// Mapster       2023/09/21 44.1M(...,138,149) 4.7k  Lambda
// Riok.Mapperly 2025        9.7M(...,30,60)   3.5k  Gen


internal class Program
{
    static void Main(string[] args)
    {
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        typeAdapterConfig.Apply(new MappingConfig());
        var mapper = new Mapper(typeAdapterConfig);

        var d = mapper.Map<Source, Destination>(new Source { });

        var d2 = SourceMapper.Map(new Source { });
    }
}

[Riok.Mapperly.Abstractions.Mapper()]
public static partial class SourceMapper
{
    public static partial Destination Map(Source source);
}

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Source, Destination>();
    }
}

public class Source
{
    public int? Id { get; set; }

    public string? Name { get; set; }

    public int Value { get; set; }
}

public class Destination
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;

    public string Value { get; set; } = default!;
}

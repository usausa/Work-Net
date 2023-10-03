namespace WorkMapper.Mappers
{

    using WorkMapper.Options;

    internal interface IMapperFactory
    {
        object Create(MapperCreateContext context);
    }
}

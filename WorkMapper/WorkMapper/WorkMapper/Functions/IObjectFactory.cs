namespace WorkMapper.Functions
{
    public interface IObjectFactory<in TSource, out TDestination>
    {
        TDestination Create(TSource source, ResolutionContext context);
    }
}

namespace WorkMapper.Functions
{
    public interface IMappingAction<in TSource, in TDestination>
    {
        void Process(TSource source, TDestination destination, ResolutionContext context);
    }
}

namespace WorkMapper.Functions
{
    public interface IMemberCondition<in TSourceMember, in TDestinationMember>
    {
        bool Eval(TSourceMember source, TDestinationMember destination, ResolutionContext context);
    }
}

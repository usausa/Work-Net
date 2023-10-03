namespace WorkMapper.Expressions
{
    using System;

    using WorkMapper.Functions;

    public interface IMappingDefaultExpression
    {
        //--------------------------------------------------------------------------------
        // Convert
        //--------------------------------------------------------------------------------

        IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember> converter);

        IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(Func<TSourceMember, TDestinationMember, ResolutionContext> converter);

        IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember>(IValueConverter<TSourceMember, TDestinationMember> converter);

        IMappingDefaultExpression ConvertUsing<TSourceMember, TDestinationMember, TValueConverter>()
            where TValueConverter : IValueConverter<TSourceMember, TDestinationMember>;

        //--------------------------------------------------------------------------------
        // Constant
        //--------------------------------------------------------------------------------

        IMappingDefaultExpression Const<TMember>(TMember value);

        //--------------------------------------------------------------------------------
        // Null
        //--------------------------------------------------------------------------------

        IMappingDefaultExpression NullIf<TMember>(TMember value);

        IMappingDefaultExpression NullIgnore(Type type);
    }
}

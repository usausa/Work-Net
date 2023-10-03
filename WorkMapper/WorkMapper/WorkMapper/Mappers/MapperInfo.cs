namespace WorkMapper.Mappers
{
    using System;
    using System.Diagnostics.CodeAnalysis;

#pragma warning disable 0649
#pragma warning disable 8618
    internal sealed class MapperInfo<TSource, TDestination>
    {
        [AllowNull]
        public Action<TSource, TDestination> MapAction;

        [AllowNull]
        public Func<TSource, TDestination> MapFunc;

        [AllowNull]
        public Action<TSource, TDestination, object> ParameterMapAction;

        [AllowNull]
        public Func<TSource, object, TDestination> ParameterMapFunc;
    }
#pragma warning restore 8618
#pragma warning restore 0649
}
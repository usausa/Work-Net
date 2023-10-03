using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace WorkSourceExpression
{
    class Program
    {
        static void Main()
        {
            var option = new MemberOption(typeof(Destination).GetProperty("Value")!);
            var expression = new MemberExpression<Source, Destination, int>(option);
            expression.MapFrom(x => x.Value1);
            expression.MapFrom(x => x.Value2);
            expression.MapFrom(x => x.Value2!.Length);
            expression.MapFrom(x => 1);
            expression.MapFrom(x => "1");
            expression.MapFrom((s, _) => s.Value1);
            expression.MapFrom((_, d) => d.Value + 1);
            expression.MapFrom((s, _, _) => s.Value1);
            expression.MapFrom((_, d, _) => d.Value + 1);
            expression.MapFrom("Value2.Length");
        }
    }

    public class Source
    {
        public int Value1 { get; set; }
        public string? Value2 { get; set; }
    }

    public class Destination
    {
        public int Value { get; set; }
    }

    public interface IMemberExpression<TSource, out TDestination, in TMember>
    {
        PropertyInfo DestinationMember { get; }

        IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> expression);

        IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Func<TSource, TDestination, TSourceMember> func);

        IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Func<TSource, TDestination, ResolutionContext, TSourceMember> func);

        IMemberExpression<TSource, TDestination, TMember> MapFrom(IValueResolver<TSource, TDestination, TMember> resolver);

        IMemberExpression<TSource, TDestination, TMember> MapFrom<TValueResolver>()
            where TValueResolver : IValueResolver<TSource, TDestination, TMember>;

        IMemberExpression<TSource, TDestination, TMember> MapFrom(string sourcePath);
    }

    internal class MemberExpression<TSource, TDestination, TMember> : IMemberExpression<TSource, TDestination, TMember>
    {
        private readonly MemberOption memberOption;

        public PropertyInfo DestinationMember => memberOption.Property;

        public IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> expression)
        {
            memberOption.SetMapFrom(expression);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Func<TSource, TDestination, TSourceMember> func)
        {
            memberOption.SetMapFrom(func);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom<TSourceMember>(Func<TSource, TDestination, ResolutionContext, TSourceMember> func)
        {
            memberOption.SetMapFrom(func);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom(IValueResolver<TSource, TDestination, TMember> resolver)
        {
            memberOption.SetMapFrom(resolver);
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom<TValueResolver>() where TValueResolver : IValueResolver<TSource, TDestination, TMember>
        {
            memberOption.SetMapFrom<TSource, TDestination, TMember, TValueResolver>();
            return this;
        }

        public IMemberExpression<TSource, TDestination, TMember> MapFrom(string sourcePath)
        {
            memberOption.SetMapFrom<TSource>(sourcePath);
            return this;
        }

        public MemberExpression(MemberOption memberOption)
        {
            this.memberOption = memberOption;
        }
    }


    public class MemberOption
    {
        public PropertyInfo Property { get; }

        private FromTypeEntry? mapFrom;

        private bool useConst;

        private object? constValue;

        public MemberOption(PropertyInfo property)
        {
            Property = property;
        }

        //--------------------------------------------------------------------------------
        // MapFrom
        //--------------------------------------------------------------------------------

        public void SetMapFrom<TSource, TSourceMember>(Expression<Func<TSource, TSourceMember>> value)
        {
            if (value.Body is MemberExpression memberExpression)
            {
                var type = typeof(TSource);
                if ((memberExpression.Member is PropertyInfo pi) && (pi.ReflectedType == type))
                {
                    mapFrom = new FromTypeEntry(FromType.Properties, pi.PropertyType, new[] { pi });
                    return;
                }
            }

            if (value.Body is ConstantExpression constantExpression)
            {
                useConst = true;
                constValue = constantExpression.Value;
                return;
            }

            mapFrom = new FromTypeEntry(FromType.LazyFunc, typeof(TSourceMember), new Lazy<Func<TSource, TSourceMember>>(value.Compile));
        }

        public void SetMapFrom<TSource, TDestination, TSourceMember>(Func<TSource, TDestination, TSourceMember> func) =>
            mapFrom = new FromTypeEntry(FromType.Func, typeof(TSourceMember), func);

        public void SetMapFrom<TSource, TDestination, TSourceMember>(Func<TSource, TDestination, ResolutionContext, TSourceMember> func) =>
            mapFrom = new FromTypeEntry(FromType.FuncContext, typeof(TSourceMember), func);


        public void SetMapFrom<TSource, TDestination, TMember>(IValueResolver<TSource, TDestination, TMember> value) =>
            mapFrom = new FromTypeEntry(FromType.Interface, typeof(TMember), value);

        public void SetMapFrom<TSource, TDestination, TMember, TValueResolver>()
            where TValueResolver : IValueResolver<TSource, TDestination, TMember> =>
            mapFrom = new FromTypeEntry(FromType.InterfaceType, typeof(TMember), typeof(TValueResolver));

        public void SetMapFrom<TSource>(string sourcePath)
        {
            var type = typeof(TSource);
            var properties = new List<PropertyInfo>();
            foreach (var name in sourcePath.Split("."))
            {
                var pi = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (pi is null)
                {
                    throw new ArgumentException("Invalid source.", nameof(sourcePath));
                }

                properties.Add(pi);
                type = pi.PropertyType;
            }

            mapFrom = new FromTypeEntry(FromType.Properties, properties[^1].PropertyType, properties);
        }

        //--------------------------------------------------------------------------------
        // Internal
        //--------------------------------------------------------------------------------

        internal FromTypeEntry? GetMapFrom() => mapFrom;
    }

    public interface IValueResolver<in TSource, in TDestination, out TDestinationMember>
    {
        TDestinationMember Resolve(TSource source, TDestination destination, ResolutionContext context);
    }

    public sealed class ResolutionContext
    {
    }

    public enum FromType
    {
        None,
        Properties,
        LazyFunc,
        Func,
        FuncContext,
        Interface,
        InterfaceType,
    }

    internal sealed class FromTypeEntry
    {
        public FromType Type { get; }

        public Type MemberType { get; }

        public object Value { get; }

        public FromTypeEntry(FromType type, Type memberType, object value)
        {
            Type = type;
            MemberType = memberType;
            Value = value;
        }
    }
}

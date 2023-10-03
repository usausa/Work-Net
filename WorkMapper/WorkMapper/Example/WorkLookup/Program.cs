using System;
using System.Collections.Generic;
using System.Diagnostics;

using Smart;

namespace WorkLookup
{
    class Program
    {
        static void Main(string[] args)
        {
            var r = new Resolver();
            // TODO 4 pattern 優先度確認
            var conv = r.Get<int?, int?>();
            Debug.WriteLine(conv(0));

            var holder = new EnumHolder();
        }
    }

    public enum MyEnum
    {
        Zero,
        One
    }

    public class EnumHolder
    {
        public MyEnum Value { get; set; }
    }

    public class Resolver
    {
        public readonly Dictionary<Tuple<Type, Type>, Func<object, object>> map = new()
        {
            { Tuple.Create(typeof(int), typeof(int)), (Func<object, object>)(_ => 1) },
            //{ Tuple.Create(typeof(int?), typeof(int)), (Func<object, object>)(_ => 2) },
            { Tuple.Create(typeof(int), typeof(int?)), (Func<object, object>)(_ => 3) },
            //{ Tuple.Create(typeof(int?), typeof(int?)), (Func<object, object>)(_ => 4) },
        };



        public Func<object, object> Get<TS, TD>()
        {
            var sourceType = typeof(TS);
            var destinationType = typeof(TD);
            if (map.TryGetValue(Tuple.Create(sourceType, destinationType), out var func))
            {
                return func;
            }

            // 相手がNULL
            var destinationUnderlyingType = Nullable.GetUnderlyingType(destinationType);
            if ((destinationUnderlyingType is not null) &&
                map.TryGetValue(Tuple.Create(sourceType, destinationUnderlyingType), out func))
            {
                return func;
            }

            // FuncのNullableに使える
            if (sourceType.IsValueType && !sourceType.IsNullableType())
            {
                var nullableSourceType = typeof(Nullable<>).MakeGenericType(sourceType);

                if (map.TryGetValue(Tuple.Create(nullableSourceType, destinationType), out func))
                {
                    return func;
                }

                if ((destinationUnderlyingType is not null) &&
                    map.TryGetValue(Tuple.Create(nullableSourceType, destinationUnderlyingType), out func))
                {
                    return func;
                }
            }

            return null;
        }
    }
}

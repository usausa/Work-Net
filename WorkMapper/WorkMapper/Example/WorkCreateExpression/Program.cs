using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace WorkCreateExpression
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = new Data { Value = 1 };
            var func = (Func<Data, int>)CallByType(data.GetType(), nameof(Data.Value));
            Debug.WriteLine(func(data));
        }

        private static object CallByType(Type type, string name)
        {
            var pi = type.GetProperty(name);

            var expressionMethod = typeof(Program).GetMethod("CreateExpression", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type, pi.PropertyType);
            var expression = expressionMethod.Invoke(null, new object[] { type, pi });

            var method = typeof(Program).GetMethod("Call", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type, pi.PropertyType);

            return method.Invoke(null, new object[] { expression });
        }

        private static Expression<Func<TSource, TSourceMember>> CreateExpression<TSource, TSourceMember>(Type type, PropertyInfo pi)
        {
            var parameter = Expression.Parameter(type);
            var property = Expression.Property(parameter, pi);
            var conversion = Expression.Convert(property, pi.PropertyType);
            return Expression.Lambda<Func<TSource, TSourceMember>>(conversion, parameter);
        }

        private static object Call<TSource, TSourceMember>(Expression<Func<TSource, TSourceMember>> expression)
        {
            return expression.Compile();
        }
    }

    public class Data
    {
        public int Value { get; set; }
    }
}

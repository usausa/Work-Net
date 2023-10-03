using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace ExampleExpression
{
    class Program
    {
        static void Main(string[] args)
        {
            //var a = 0;
            //MapFrom<Data, int>(x => x.Value);
            //MapFrom<Data, int>(x => 0);
            //MapFrom<Data, int>(x => a);
            //MapFrom<Data, int>(x => x.Value.ToString().Length);
            Convert<int, long>(x => Converter.Int32ToInt64(x));
            Convert<int, long>(x => Converter.ConstInt64());
            //Convert<int, long>(Converter.Int32ToInt64);
            Convert<int, string>(x => x.ToString());
        }

        public static void MapFrom<TSource, TMember>(Expression<Func<TSource, TMember>> expression)
        {
            Debug.WriteLine("--");
            Debug.WriteLine(expression.NodeType);
            Debug.WriteLine(expression.Body.GetType());

            if (expression.Body is MemberExpression memberExpression)
            {
                var type = typeof(TSource);
                var pi = memberExpression.Member as PropertyInfo;
                if ((pi is null) || ((type != pi.ReflectedType) && !type.IsSubclassOf(pi.ReflectedType)))
                {
                    return;
                }

                Debug.WriteLine(pi.Name);
                return;
            }

            if (expression.Body is ConstantExpression constantExpression)
            {
                Debug.WriteLine(constantExpression.Value);
                return;
            }
        }

        public static void Convert<TSource, TDestination>(Expression<Func<TSource, TDestination>> expression)
        {
            if (expression.Body is MethodCallExpression callExpression)
            {
                var method = callExpression.Method;
                if (method.IsStatic)
                {
                    if ((callExpression.Arguments.Count == 1) &&
                        (callExpression.Arguments[0] is ParameterExpression parameterExpression) &&
                        (expression.Parameters[0].Name == parameterExpression.Name))
                    {
                        Debug.WriteLine(method.Name);
                        return;
                    }

                    if (callExpression.Arguments.Count == 0)
                    {
                        Debug.WriteLine(method.Name);
                        return;
                    }
                }
                else
                {
                    if ((callExpression.Arguments.Count == 0) &&
                        (callExpression.Object is ParameterExpression parameterExpression) &&
                        (expression.Parameters[0].Name == parameterExpression.Name))
                    {
                        Debug.WriteLine(method.Name);
                        return;
                    }
                }
            }
        }
    }

    public static class Converter
    {
        public static long Int32ToInt64(int value) => value;

        public static long ConstInt64() => 0;
    }


    public class Data
    {
        public int Value { get; set; }
    }
}

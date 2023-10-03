using System;
using System.Diagnostics;
using System.Linq.Expressions;

namespace FuncTypeBenchmark
{
    public static class ExpressionTest
    {
        public static void Test<TS, TM>(Expression<Func<TS, TM>> expression)
        {
            Debug.WriteLine("----");
            var body = expression.Body;
            Debug.WriteLine(body.NodeType);
            Debug.WriteLine(expression.ReturnType);
            Debug.WriteLine(expression.Parameters.Count);
        }
    }
}

namespace Develop;

using WorkInterceptor.Library;

internal static class Program
{
    public static void Main()
    {
        var builder = new Builder();

        builder.Execute<string>();
        builder.Execute<int>();

        var builder2 = new Builder();
        builder2.Execute<object>();
    }
}

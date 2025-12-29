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

        // Extension method test
        builder.AddExecutes();
    }
}

public static class Extensions
{
    public static void AddExecutes(this IBuilder builder)
    {
        builder.Execute<DateTime>();
        builder.Execute<Data>();
    }
}

public sealed class Data
{
}

namespace Develop;

using WorkInterceptor.Library;

internal static class Program
{
    public static void Main()
    {
        var builder = new Builder();
        builder.Execute();
        builder.Execute();
    }
}

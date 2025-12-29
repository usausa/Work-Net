namespace WorkInterceptor.Library;

public interface IBuilder
{
    void Execute<T>();

    void Execute<T>(Type t);
}

#pragma warning disable CA1822
public sealed class Builder : IBuilder
{
    public void Execute<T>()
    {
        Console.WriteLine("Execute");
    }

    public void Execute<T>(Type t)
    {
        Console.WriteLine($"Execute {t.FullName}");
    }
}
#pragma warning restore CA1822

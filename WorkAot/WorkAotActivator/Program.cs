namespace WorkAotActivator;

public static class Program
{
    public static void Main()
    {
        var obj = Registry.Create<IHoge>();
        obj?.Execute();
    }
}

public static class Initializer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
        Registry.Register<IHoge, Hoge>();
    }
}

public sealed class Registry
{
    private static readonly Dictionary<Type, Type> Map = new();

    public static void Register<TInterface, TImplement>()
        where TImplement : class, TInterface
    {
        Map[typeof(TInterface)] = typeof(TImplement);
    }

    public static TInterface? Create<TInterface>()
    {
        if (Map.TryGetValue(typeof(TInterface), out var type))
        {
            return (TInterface?)Activator.CreateInstance(type);
        }

        return default;
    }
}

public interface IHoge
{
    void Execute();
}

public class Hoge : IHoge
{
    public void Execute() => Console.WriteLine("dummy");
}

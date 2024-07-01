namespace WorkAccessor;

using WorkAccessorLib;

public static class Program
{
    public static void Main()
    {
        var accessorFactory = AccessorRegistry.FindFactory<Data>()!;
        var getId = accessorFactory.CreateGetter<int>(nameof(Data.Id))!;
        var getName = accessorFactory.CreateGetter<string>(nameof(Data.Name))!;

        var data = new Data { Id = 123, Name = "abc" };
        Console.WriteLine($"{getId(data)} {getName(data)}");
    }
}

#pragma warning disable SA1503
#pragma warning disable CS8603 // Possible null reference return.
#pragma warning disable CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
internal static class AccessorFactoryInitializer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
        AccessorRegistry.RegisterFactory(typeof(Data), typeof(DataAccessorFactory));
    }
}

internal sealed class DataAccessorFactory : IAccessorFactory<Data>
{
    private static readonly Func<object, object> ObjectIdGetter = x => ((Data)x).Id;
    private static readonly Func<object, object> ObjectNameGetter = x => ((Data)x).Name;
    private static readonly Action<object, object> ObjectIdSetter = (x, v) => ((Data)x).Id = (int)v;
    private static readonly Action<object, object> ObjectNameSetter = (x, v) => ((Data)x).Name = (string)v;
    private static readonly Func<Data, int> TypedIdGetter = x => x.Id;
    private static readonly Func<Data, string> TypedNameGetter = x => x.Name;
    private static readonly Action<Data, int> TypedIdSetter = (x, v) => x.Id = v;
    private static readonly Action<Data, string> TypedNameSetter = (x, v) => x.Name = v;

    public Func<object, object> CreateGetter(string name)
    {
        if (name == "Id") return ObjectIdGetter;
        if (name == "Name") return ObjectNameGetter;
        return null;
    }

    public Action<object, object> CreateSetter(string name)
    {
        if (name == "Id") return ObjectIdSetter;
        if (name == "Name") return ObjectNameSetter;
        return null;
    }

    public Func<Data, TProperty> CreateGetter<TProperty>(string name)
    {
        if (name == "Id") return (Func<Data, TProperty>)(object)TypedIdGetter;
        if (name == "Name") return (Func<Data, TProperty>)(object)TypedNameGetter;
        return null;
    }

    public Action<Data, TProperty> CreateSetter<TProperty>(string name)
    {
        if (name == "Id") return (Action<Data, TProperty>)(object)TypedIdSetter;
        if (name == "Name") return (Action<Data, TProperty>)(object)TypedNameSetter;
        return null;
    }
}
#pragma warning restore CS8613 // Nullability of reference types in return type doesn't match implicitly implemented member.
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore SA1503

public class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

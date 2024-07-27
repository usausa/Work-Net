namespace WorkAotAccessor;

using MemberAccessorGenerator;

public static class Program
{
    public static void Main()
    {
        //var accessorFactory = AccessorRegistry.FindFactory<Data>()!;
        //var getter = accessorFactory.CreateGetter<int>(nameof(Data.Id))!;
        //var setter = accessorFactory.CreateSetter<int>(nameof(Data.Id))!;

        //var data = new Data();
        //setter(data, 123);
        //var id = getter(data);
        //Console.WriteLine(id);

        // TODO
        AccessorRegistry.RegisterFactory(typeof(Data<int>), typeof(Data_AccessorFactory<int>));

        var accessorFactory = AccessorRegistry.FindFactory<Data<int>>()!;
        var getter = accessorFactory.CreateGetter<int>(nameof(Data<int>.Value))!;
        var setter = accessorFactory.CreateSetter<int>(nameof(Data<int>.Value))!;

        var data = new Data<int>();
        setter(data, 123);
        var value = getter(data);
        Console.WriteLine(value);
    }
}

[GenerateAccessor]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

[GenerateAccessor]
public partial class Data<T>
{
    public T Value { get; set; } = default!;
}

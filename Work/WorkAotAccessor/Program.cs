using BunnyTail.MemberAccessor;

namespace WorkAotAccessor;

internal class Program
{
    static void Main(string[] args)
    {
        var accessorFactory = AccessorRegistry.FindFactory<Data>()!;
        var getter = accessorFactory.CreateGetter<int>(nameof(Data.Id))!;
        var setter = accessorFactory.CreateSetter<int>(nameof(Data.Id))!;

        var data = new Data();
        setter(data, 123);
        var id = getter(data);
        Console.WriteLine(id);
    }
}

[GenerateAccessor]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

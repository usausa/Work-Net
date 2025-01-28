using BunnyTail.MemberAccessor;

namespace WorkAotAccessor;

internal class Program
{
    static void Main(string[] args)
    {
        // Required
        // rd.xml

        // TODO
        // Generic
        // NoWarn IL

        var accessorFactory1 = AccessorRegistry.FindFactory<Data>()!;
        var getter1 = accessorFactory1.CreateGetter<int>(nameof(Data.Id))!;
        var setter1 = accessorFactory1.CreateSetter<int>(nameof(Data.Id))!;

        var data1 = new Data();
        setter1(data1, 123);
        var id = getter1(data1);
        Console.WriteLine(id);

        //var accessorFactory2 = AccessorRegistry.FindFactory<GenericData<int>>()!;
        //var getter2 = accessorFactory2.CreateGetter<int>(nameof(GenericData<int>.Value))!;
        //var setter2 = accessorFactory2.CreateSetter<int>(nameof(GenericData<int>.Value))!;

        //var data2 = new GenericData<int>();
        //setter2(data2, 123);
        //var value = getter2(data2);
        //Console.WriteLine(value);
    }
}

[GenerateAccessor]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

[GenerateAccessor]
public class GenericData<T>
{
    public T Value { get; set; } = default!;
}

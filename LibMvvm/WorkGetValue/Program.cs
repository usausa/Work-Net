namespace WorkGetValue;

using BunnyTail.MemberAccessor;

using static System.Runtime.InteropServices.JavaScript.JSType;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
    }
}

[GenerateAccessor]
public class ViewModel
{
    public string Name { get; set; } = default!;

    public int Age { get; set; }

    public void Validate(string name)
    {
        //var accessorFactory = AccessorRegistry.FindFactory(GetType());
    }
}

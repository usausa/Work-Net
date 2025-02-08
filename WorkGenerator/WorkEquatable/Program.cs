namespace WorkEquatable;

using Generator.Equals;

internal class Program
{
    static void Main(string[] args)
    {
    }
}

[Equatable]
partial class MyClass
{
    [DefaultEquality]
    private int _secretNumber = 42;

    [OrderedEquality]
    public string[] Fruits { get; set; }
}

[Equatable]
partial struct MyStruct
{
    [OrderedEquality]
    public string[] Fruits { get; set; }
}

[Equatable]
partial record MyRecord(
    [property: OrderedEquality] string[] Fruits);

// IgnoreEquality
// OrderedEquality
// UnorderedEquality
// SetEquality
// ReferenceEquality
// CustomEquality typeof
// Explicit, IgnoreInheritedMembers

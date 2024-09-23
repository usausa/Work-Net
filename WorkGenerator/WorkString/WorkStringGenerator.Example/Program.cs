namespace WorkStringGenerator.Example;

using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class Program
{
    public static void Main()
    {
        Debug.WriteLine(new Data { Id = 123, Name = "Data-xyz", Values = [1, 2] });
        Debug.WriteLine(new Data { Id = 123, Name = "Data-xyz" });
    }
}

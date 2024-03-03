namespace WorkStringGenerator.Example;

using System.Diagnostics;

public static class Program
{
    public static void Main()
    {
        var d = new Data { Id = 123, Name = "Data-xyz" };
        Debug.WriteLine(d.ToString());
    }
}

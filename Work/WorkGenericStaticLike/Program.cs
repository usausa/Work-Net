using System.Diagnostics;

namespace WorkGenericDefault;

internal static class Program
{
    public static void Main()
    {
        // class is NullReferenceException
        //Debug.WriteLine(GetDataSize<Data1>());
        //Debug.WriteLine(GetDataSize<Data2>());
        Debug.WriteLine(GetDataSize<SData1>());
        Debug.WriteLine(GetDataSize<SData2>());
    }

    // Policy pattern
    private static int GetDataSize<T>()
        where T : IData
    {
        return default(T)!.Size;
    }
}

public interface IData
{
    int Size { get; }
}

public sealed class Data1 : IData
{
    public int Size => 12;
}


public sealed class Data2 : IData
{
    public int Size => 16;
}

public struct SData1 : IData
{
    public int Size => 24;
}

public struct SData2 : IData
{
    public int Size => 32;
}

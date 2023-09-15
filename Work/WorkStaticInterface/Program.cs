Test.Display<Data1>();
Test.Display<Data2>();

public static class Test
{
    // IL_0000:  constrained. !!T
    // IL_0006:  call int32 IMetadata::Size()
    // IL_000b:  call void [System.Console] System.Console::WriteLine(int32)
    // IL_0010:  ret
    public static void Display<T>()
        where T : IMetadata
    {
        Console.WriteLine(T.Size());
    }
}

public interface IMetadata
{
    static abstract int Size();
}

public class Data1 : IMetadata
{
    public static int Size() => 4;
}

public class Data2 : IMetadata
{
    public static int Size() => 8;
}
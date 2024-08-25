namespace WorkCallIL;

public static class Program
{
    public static void Main()
    {
    }

    // class
    // IL_0000:  ldarg.0
    // IL_0001:  callvirt   instance int32 WorkCallIL.ClassParameter::get_Value()
    // IL_0006:  ret

    // class in, ref
    // IL_0000:  ldarg.0
    // IL_0001:  ldind.ref
    // IL_0002:  callvirt   instance int32 WorkCallIL.ClassParameter::get_Value()
    // IL_0007:  ret

    // struct
    // IL_0000:  ldarga.s   parameter
    // IL_0002:  call       instance int32 WorkCallIL.StructParameter::get_Value()
    // IL_0007:  ret

    // struct in, ref
    // IL_0000:  ldarg.0
    // IL_0001:  call       instance int32 WorkCallIL.StructParameter::get_Value()
    // IL_0006:  ret

    public static int Call(ClassParameter parameter) => parameter.Value;

    public static int CallIn(in ClassParameter parameter) => parameter.Value;

    public static int CallRef(ref ClassParameter parameter) => parameter.Value;

    public static int Call(StructParameter parameter) => parameter.Value;

    public static int CallIn(in StructParameter parameter) => parameter.Value;

    public static int CallRef(ref StructParameter parameter) => parameter.Value;

    public static int Call(RefStructParameter parameter) => parameter.Value;

    public static int CallIn(in RefStructParameter parameter) => parameter.Value;

    public static int CallRef(ref RefStructParameter parameter) => parameter.Value;

    public static int Call(ReadonlyRefStructParameter parameter) => parameter.Value;

    public static int CallIn(in ReadonlyRefStructParameter parameter) => parameter.Value;

    public static int CallRef(ref ReadonlyRefStructParameter parameter) => parameter.Value;
}

public class ClassParameter
{
    public int Value { get; }

    public ClassParameter(int value)
    {
        Value = value;
    }
}

public struct StructParameter
{
    public int Value { get; }

    public StructParameter(int value)
    {
        Value = value;
    }
}

public ref struct RefStructParameter
{
    public int Value { get; }

    public RefStructParameter(int value)
    {
        Value = value;
    }
}

public readonly ref struct ReadonlyRefStructParameter
{
    public int Value { get; }

    public ReadonlyRefStructParameter(int value)
    {
        Value = value;
    }
}

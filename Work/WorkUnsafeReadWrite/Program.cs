namespace WorkUnsafeReadWrite;

using System.Runtime.CompilerServices;

internal static unsafe class Program
{
    public static void Main()
    {
        var data = new byte[sizeof(ExampleStruct)];

        fixed (byte* ptr = data)
        {
            // Unaligned offset
            byte* unalignedPtr = ptr + 1;

            *(int*)unalignedPtr = 42;

            try
            {
                int value = Unsafe.Read<int>(unalignedPtr);
                Console.WriteLine($"Unsafe.Read<int>: {value}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unsafe.Read<int> failed: {ex.Message}");
            }

            int unalignedValue = Unsafe.ReadUnaligned<int>(unalignedPtr);
            Console.WriteLine($"Unsafe.ReadUnaligned<int>: {unalignedValue}");
        }
    }
}

public struct ExampleStruct
{
    public int Value1;

    public int Value2;
}

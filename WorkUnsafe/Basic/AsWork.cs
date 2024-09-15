namespace Basic;

using System.Diagnostics;
using System.Runtime.CompilerServices;

public static class AsWork
{
    public static void Run()
    {
        var target = new Target(100);
        var hack = Unsafe.As<TargetHack>(target);

        Debug.WriteLine(hack.Value);
        hack.Value = 123;
        Debug.WriteLine(hack.Value);

        hack.Execute();
    }

    public class Target
    {
        private readonly int value;

        public Target(int value)
        {
            this.value = value;
        }

        public void Execute()
        {
            Debug.WriteLine(value);
        }
    }

    public class TargetHack
    {
        public int Value;

        public void Execute()
        {
            Debug.WriteLine("Hack");
        }
    }
}

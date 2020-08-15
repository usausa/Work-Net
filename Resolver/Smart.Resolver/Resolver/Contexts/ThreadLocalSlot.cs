namespace Smart.Resolver.Contexts
{
    using System;
    using System.Runtime.CompilerServices;

    public sealed class ThreadLocalSlot
    {
        [ThreadStatic]
        public static ThreadLocalSlot Current;

        // TODO

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            // TODO
        }
    }
}

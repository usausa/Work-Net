namespace Smart.Resolver.Contexts
{
    using System.Runtime.CompilerServices;

    public sealed class ThreadLocalChildContext : IContext
    {
        private readonly ThreadLocalSlot slot = new ThreadLocalSlot();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            slot.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Switch()
        {
            ThreadLocalSlot.Current = slot;
        }
    }
}

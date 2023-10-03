namespace Awaiter
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var ret = await AwaitableResultCall();

            Console.WriteLine(ret);
        }

        private static AwaitableResult AwaitableResultCall()
        {
            return new AwaitableResult();
        }
    }

    public class AwaitableResult
    {
        public AwaitableResultAwaiter GetAwaiter()
        {
            return new AwaitableResultAwaiter();
        }
    }

    public class AwaitableResultAwaiter : INotifyCompletion
    {
        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            continuation();
        }

        public string GetResult()
        {
            return "UsaUsa";
        }
    }
}

namespace WorkCliHost.Core;

public interface ICliHost : IAsyncDisposable
{
    Task<int> RunAsync();
}

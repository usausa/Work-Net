namespace WorkCliHost;

public interface ICliHost : IAsyncDisposable
{
    Task<int> RunAsync();
}

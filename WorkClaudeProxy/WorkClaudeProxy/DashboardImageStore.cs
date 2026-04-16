namespace WorkClaudeProxy;

internal sealed class DashboardImageStore
{
    private readonly Lock stateLock = new();
    private DisplayState? currentState;

    public string OutputPath { get; }

    public DashboardImageStore(string outputPath)
    {
        OutputPath = outputPath;
    }

    public void UpdateState(DisplayState state)
    {
        lock (stateLock)
        {
            currentState = state;
        }
    }

    public DisplayState? GetState()
    {
        lock (stateLock)
        {
            return currentState;
        }
    }
}

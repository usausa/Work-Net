namespace WorkTcpServer.Services;

public sealed class DataService
{
    private readonly Lock sync = new();

    private int store;

    public int GetValue()
    {
        lock (sync)
        {
            return store;
        }
    }

    public void UpdateValue(int value)
    {
        lock (sync)
        {
            store = value;
        }
    }
}

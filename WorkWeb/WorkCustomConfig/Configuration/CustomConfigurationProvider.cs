namespace WorkCustomConfig.Configuration;

internal sealed class CustomConfigurationProvider : ConfigurationProvider, IConfigurationOperator, IDisposable
{
    private readonly CustomConfigurationOption option;

    public CustomConfigurationProvider(CustomConfigurationOption option)
    {
        this.option = option;
    }

    public void Dispose()
    {
    }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        data["Test"] = "1";
        data["Sub:Key1"] = "abc";
        data["Sub:Key2"] = "123";

        Data = data;
    }

    public void Update(string key, string value)
    {
        // TODO sync ?
        Data[key] = value;
        OnReload();
    }

    public void Delete(string key)
    {
        // TODO sync ?
        Data.Remove(key);
        OnReload();
    }
}

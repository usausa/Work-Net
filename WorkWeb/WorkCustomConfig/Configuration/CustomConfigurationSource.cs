namespace WorkCustomConfig.Configuration;

internal sealed class CustomConfigurationSource : IConfigurationSource
{
    private readonly CustomConfigurationOption option;

    public CustomConfigurationSource(CustomConfigurationOption option)
    {
        this.option = option;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new CustomConfigurationProvider(option);
}

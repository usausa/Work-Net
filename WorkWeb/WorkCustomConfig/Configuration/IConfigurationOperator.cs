namespace WorkCustomConfig.Configuration;

public interface IConfigurationOperator
{
    void Update(string key, string value);

    void Delete(string key);
}

public static class ConfigurationOperatorExtensions
{
    public static void Update<T>(this IConfigurationOperator configurationOperator, string key, T value) =>
        configurationOperator.Update(key, value!.ToString()!);
}

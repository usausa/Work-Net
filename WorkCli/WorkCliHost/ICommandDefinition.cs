namespace WorkCliHost;

public interface ICommandDefinition
{
    ValueTask ExecuteAsync();
}

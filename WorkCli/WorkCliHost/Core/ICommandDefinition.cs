namespace WorkCliHost.Core;

public interface ICommandDefinition
{
    ValueTask ExecuteAsync(CommandContext context);
}

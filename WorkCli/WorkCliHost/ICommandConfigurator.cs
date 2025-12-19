namespace WorkCliHost;

public interface ICommandConfigurator
{
    ICommandConfigurator AddSubCommand<TCommand>(Action<ICommandConfigurator>? configure = null)
        where TCommand : class, ICommandDefinition;
}

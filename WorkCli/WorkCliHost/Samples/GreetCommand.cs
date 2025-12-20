using Microsoft.Extensions.Logging;
using WorkCliHost.Core;

namespace WorkCliHost.Samples;

[CliCommand("greet", Description = "Greet someone")]
public sealed class GreetCommand : ICommandDefinition
{
    private readonly ILogger<GreetCommand> _logger;

    public GreetCommand(ILogger<GreetCommand> logger)
    {
        _logger = logger;
    }

    [CliArgument<string>(0, "name", Description = "Name to greet", IsRequired = true)]
    public string Name { get; set; } = default!;

    [CliArgument<string>(1, "greeting", Description = "Greeting message", IsRequired = false, DefaultValue = "Hello")]
    public string Greeting { get; set; } = default!;

    [CliArgument<int>(2, "count", Description = "Number of times to greet", IsRequired = false, DefaultValue = 1)]
    public int Count { get; set; }

    public ValueTask ExecuteAsync(CommandContext context)
    {
        _logger.LogInformation("Greeting: {Greeting}, {Name}!", Greeting, Name);
        
        for (int i = 0; i < Count; i++)
        {
            Console.WriteLine($"{Greeting}, {Name}!");
        }
        
        return ValueTask.CompletedTask;
    }
}

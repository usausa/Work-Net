// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Example.SystemInfo.ConsoleApp;

using MacDotNet.SystemInfo;

using Smart.CommandLine.Hosting;

public static class CommandBuilderExtensions
{
    public static void AddCommands(this ICommandBuilder commands)
    {
        commands.AddCommand<UptimeCommand>();
    }
}

// Uptime
[Command("uptime", "Uptime")]
public sealed class UptimeCommand : ICommandHandler
{
   public ValueTask ExecuteAsync(CommandContext context)
    {
        var uptime = PlatformProvider.GetUptime();
        Console.WriteLine($"Uptime: {uptime.Uptime}");

        return ValueTask.CompletedTask;
    }
}

using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;

namespace WorkCliHost;

public interface ICliHostBuilder
{
    ICliHostBuilder ConfigureServices(Action<IServiceCollection> configureServices);
    ICliHostBuilder ConfigureCommands(Action<RootCommand> configureRoot);
    ICliHostBuilder UseRootCommand(RootCommand rootCommand);

    ICliHost Build();
}

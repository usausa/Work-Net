using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection;

namespace WorkCliHost;

internal sealed class CliHostBuilder : ICliHostBuilder
{
    private readonly string[] _args;
    private readonly ServiceCollection _services = new();
    private readonly List<Action<RootCommand>> _commandConfigurations = new();
    private RootCommand? _customRootCommand;

    public CliHostBuilder(string[] args)
    {
        _args = args;

        _services.AddLogging(builder =>
        {
            builder.AddConsole();
        });
    }

    public ICliHostBuilder ConfigureServices(Action<IServiceCollection> configureServices)
    {
        configureServices(_services);
        return this;
    }

    public ICliHostBuilder ConfigureCommands(Action<RootCommand> configureRoot)
    {
        _commandConfigurations.Add(configureRoot);
        return this;
    }

    public ICliHostBuilder UseRootCommand(RootCommand rootCommand)
    {
        _customRootCommand = rootCommand;
        return this;
    }

    public ICliHost Build()
    {
        var serviceProvider = _services.BuildServiceProvider();

        var rootCommand = _customRootCommand ?? new RootCommand();

        foreach (var configure in _commandConfigurations)
        {
            configure(rootCommand);
        }

        var commandRegistrations = serviceProvider.GetServices<CommandRegistration>();
        foreach (var registration in commandRegistrations)
        {
            var command = CreateCommand(registration.CommandType, serviceProvider);
            rootCommand.Add(command);
        }

        return new CliHostImplementation(_args, rootCommand, serviceProvider);
    }

    private Command CreateCommand(Type commandType, IServiceProvider serviceProvider)
    {
        var attribute = commandType.GetCustomAttribute<CliCommandAttribute>()
            ?? throw new InvalidOperationException($"Type {commandType.Name} must have CliCommandAttribute");

        var command = new Command(attribute.Name, attribute.Description);

        var properties = commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<CliArgumentAttribute>()
            })
            .Where(x => x.Attribute != null)
            .OrderBy(x => x.Attribute!.Position)
            .ToList();

        var arguments = new List<(Argument Argument, PropertyInfo Property)>();
        foreach (var prop in properties)
        {
            var argAttr = prop.Attribute!;
            var argumentType = typeof(Argument<>).MakeGenericType(prop.Property.PropertyType);
            var argument = (Argument)Activator.CreateInstance(
                argumentType,
                argAttr.Name,
                argAttr.Description)!;

            // デフォルト値を設定
            if (argAttr.DefaultValue != null)
            {
                var setDefaultValueMethod = argument.GetType().GetMethod("SetDefaultValue");
                setDefaultValueMethod?.Invoke(argument, [argAttr.DefaultValue]);
            }
            else if (!argAttr.IsRequired)
            {
                // IsRequiredがfalseでデフォルト値が指定されていない場合、型のデフォルト値を設定
                var defaultValue = prop.Property.PropertyType.IsValueType
                    ? Activator.CreateInstance(prop.Property.PropertyType)
                    : null;
                var setDefaultValueMethod = argument.GetType().GetMethod("SetDefaultValue");
                setDefaultValueMethod?.Invoke(argument, [defaultValue]);
            }

            // Arityを設定（オプション引数の場合）
            if (!argAttr.IsRequired || argAttr.DefaultValue != null)
            {
                var arityProperty = argument.GetType().GetProperty("Arity");
                if (arityProperty != null)
                {
                    var arityType = arityProperty.PropertyType;
                    var zeroOrOneField = arityType.GetProperty("ZeroOrOne", BindingFlags.Public | BindingFlags.Static);
                    if (zeroOrOneField != null)
                    {
                        arityProperty.SetValue(argument, zeroOrOneField.GetValue(null));
                    }
                }
            }

            arguments.Add((argument, prop.Property));
            command.Add(argument);
        }

        command.SetHandler(async (InvocationContext context) =>
        {
            var instance = (ICommandDefinition)ActivatorUtilities.CreateInstance(serviceProvider, commandType);

            foreach (var (argument, property) in arguments)
            {
                var value = context.ParseResult.GetValueForArgument(argument);
                property.SetValue(instance, value);
            }

            await instance.ExecuteAsync();
        });

        return command;
    }
}

internal sealed class CliHostImplementation : ICliHost
{
    private readonly string[] _args;
    private readonly RootCommand _rootCommand;
    private readonly IServiceProvider _serviceProvider;

    public CliHostImplementation(string[] args, RootCommand rootCommand, IServiceProvider serviceProvider)
    {
        _args = args;
        _rootCommand = rootCommand;
        _serviceProvider = serviceProvider;
    }

    public async Task<int> RunAsync()
    {
        return await _rootCommand.InvokeAsync(_args);
    }

    public async ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

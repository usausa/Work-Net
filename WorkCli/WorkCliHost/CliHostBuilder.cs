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
                Attribute = GetCliArgumentAttribute(p)
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
            var defaultValue = GetDefaultValue(prop.Property, argAttr);
            if (defaultValue.HasValue)
            {
                var setDefaultValueMethod = argument.GetType().GetMethod("SetDefaultValue");
                setDefaultValueMethod?.Invoke(argument, [defaultValue.Value]);
            }

            // Arityを設定（オプション引数の場合）
            if (!argAttr.IsRequired || defaultValue.HasValue)
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

    private static CliArgumentInfo? GetCliArgumentAttribute(PropertyInfo property)
    {
        // ジェネリック版の属性を検索
        var genericAttr = property.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType().IsGenericType && 
                                 a.GetType().GetGenericTypeDefinition() == typeof(CliArgumentAttribute<>));

        if (genericAttr != null)
        {
            var attrType = genericAttr.GetType();
            var position = (int)attrType.GetProperty("Position")!.GetValue(genericAttr)!;
            var name = (string)attrType.GetProperty("Name")!.GetValue(genericAttr)!;
            var description = (string?)attrType.GetProperty("Description")?.GetValue(genericAttr);
            var isRequired = (bool)attrType.GetProperty("IsRequired")!.GetValue(genericAttr)!;
            var defaultValue = attrType.GetProperty("DefaultValue")?.GetValue(genericAttr);

            return new CliArgumentInfo(position, name, description, isRequired, defaultValue);
        }

        // 非ジェネリック版の属性を検索
        var attr = property.GetCustomAttribute<CliArgumentAttribute>();
        if (attr != null)
        {
            return new CliArgumentInfo(attr.Position, attr.Name, attr.Description, attr.IsRequired, null);
        }

        return null;
    }

    private static (bool HasValue, object? Value) GetDefaultValue(PropertyInfo property, CliArgumentInfo argInfo)
    {
        if (argInfo.DefaultValue != null)
        {
            return (true, argInfo.DefaultValue);
        }

        if (!argInfo.IsRequired)
        {
            var defaultValue = property.PropertyType.IsValueType
                ? Activator.CreateInstance(property.PropertyType)
                : null;
            return (true, defaultValue);
        }

        return (false, null);
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

internal sealed record CliArgumentInfo(
    int Position,
    string Name,
    string? Description,
    bool IsRequired,
    object? DefaultValue);

namespace WorkMeditator;

using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

// TODO 内容確認
internal class Program
{
    static async Task Main(string[] args)
    {
        var services = new ServiceCollection();
        services
            .AddDispatchR(options =>
        {
            options.Assemblies.Add(typeof(Ping).Assembly);
            options.RegisterPipelines = true;
            options.PipelineOrder =
            [
                typeof(FirstPipelineBehavior),
                typeof(SecondPipelineBehavior),
                typeof(GenericPipelineBehavior<,>)
            ];
            options.IncludeHandlers = null;
            options.ExcludeHandlers = null;
        });

        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IMediator>();

        var response = await dispatcher.Send(new Ping(), default);

        Debug.WriteLine($"Response: {response}");
    }
}

//--------------------------------------------------------------------------------
// Sample
//--------------------------------------------------------------------------------

public class Ping : IRequest<Ping, ValueTask<int>>
{
}

public class PingHandler : IRequestHandler<Ping, ValueTask<int>>
{
    public ValueTask<int> Handle(Ping request, CancellationToken cancellationToken)
    {
        Debug.WriteLine("Received in request handler");
        return ValueTask.FromResult(1);
    }
}

public class GenericPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, ValueTask<TResponse>>
    where TRequest : class, IRequest<TRequest, ValueTask<TResponse>>
{
    public ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        Debug.WriteLine("Generic Request Pipeline");
        return NextPipeline.Handle(request, cancellationToken);
    }

    public required IRequestHandler<TRequest, ValueTask<TResponse>> NextPipeline { get; set; }
}

public class FirstPipelineBehavior : IPipelineBehavior<Ping, ValueTask<int>>
{
    public required IRequestHandler<Ping, ValueTask<int>> NextPipeline { get; set; }
    public ValueTask<int> Handle(Ping request, CancellationToken cancellationToken)
    {
        Debug.WriteLine("First Request Pipeline");
        return NextPipeline.Handle(request, cancellationToken);
    }
}

public class SecondPipelineBehavior : IPipelineBehavior<Ping, ValueTask<int>>
{
    public required IRequestHandler<Ping, ValueTask<int>> NextPipeline { get; set; }

    public ValueTask<int> Handle(Ping request, CancellationToken cancellationToken)
    {
        Debug.WriteLine("Second Request Pipeline");
        return NextPipeline.Handle(request, cancellationToken);
    }
}

//--------------------------------------------------------------------------------
// Meditator
//--------------------------------------------------------------------------------

public interface IMediator
{
    TResponse Send<TRequest, TResponse>(IRequest<TRequest, TResponse> request,
        CancellationToken cancellationToken) where TRequest : class, IRequest;
}

public sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public TResponse Send<TRequest, TResponse>(IRequest<TRequest, TResponse> request,
        CancellationToken cancellationToken) where TRequest : class, IRequest
    {
        return serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>()
            .Handle(Unsafe.As<TRequest>(request), cancellationToken);
    }
}

//--------------------------------------------------------------------------------
// Extension
//--------------------------------------------------------------------------------

public class ConfigurationOptions
{
    public bool RegisterPipelines { get; set; } = true;
    public bool RegisterNotifications { get; set; } = true;
    public List<Assembly> Assemblies { get; } = new();
    public List<Type>? PipelineOrder { get; set; }
    /// <summary>
    /// If null, this List is ignored.
    /// If set, only the specified handlers will be included.
    /// </summary>
    public List<Type>? IncludeHandlers { get; set; }
    /// <summary>
    /// If null, this List is ignored.
    /// If set, only the specified handlers will be NOT included.
    /// </summary>
    public List<Type>? ExcludeHandlers { get; set; }

    internal bool IsHandlerIncluded(Type handlerType)
    {
        var included = IncludeHandlers?.Contains(handlerType) ?? true;
        var excluded = ExcludeHandlers?.Contains(handlerType) ?? false;

        return included && !excluded;
    }

}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDispatchR(this IServiceCollection services, Action<ConfigurationOptions> configuration)
    {
        var config = new ConfigurationOptions();
        configuration(config);

        if (config is { IncludeHandlers.Count: 0 })
        {
            throw new Exception();
        }

        if (config is { ExcludeHandlers.Count: 0 })
        {
            throw new Exception();
        }

        return services.AddDispatchR(config);
    }

    public static IServiceCollection AddDispatchR(this IServiceCollection services, Assembly assembly, bool withPipelines = true, bool withNotifications = true)
    {
        var config = new ConfigurationOptions
        {
            RegisterPipelines = withPipelines,
            RegisterNotifications = withNotifications
        };

        config.Assemblies.Add(assembly);

        return services.AddDispatchR(config);
    }

    public static IServiceCollection AddDispatchR(this IServiceCollection services, ConfigurationOptions configurationOptions)
    {
        services.AddScoped<IMediator, Mediator>();
        var requestHandlerType = typeof(IRequestHandler<,>);
        var pipelineBehaviorType = typeof(IPipelineBehavior<,>);

        var allTypes = configurationOptions.Assemblies.SelectMany(x => x.GetTypes()).Distinct()
            .Where(p =>
            {
                var interfaces = p.GetInterfaces();
                return interfaces.Length >= 1 &&
                       interfaces
                           .Where(i => i.IsGenericType)
                           .Select(i => i.GetGenericTypeDefinition())
                           .Any(i =>
                           {
                               if (i == requestHandlerType)
                               {
                                   return configurationOptions.IsHandlerIncluded(p);
                               }

                               return new[]
                               {
                                   pipelineBehaviorType,
                               }.Contains(i);
                           });
            })
            .Where(x => !x.IsInterface) // TODO
            .ToList();


        ServiceRegistrator.RegisterHandlers(services, allTypes, requestHandlerType, pipelineBehaviorType,
            configurationOptions.RegisterPipelines, configurationOptions.PipelineOrder);

        return services;
    }
}

internal static class ServiceRegistrator
{
    public static void RegisterHandlers(IServiceCollection services, List<Type> allTypes,
        Type requestHandlerType, Type pipelineBehaviorType, bool withPipelines, List<Type>? pipelineOrder = null)
    {
        var allHandlers = allTypes
            .Where(p =>
            {
                var @interface = p.GetInterfaces().First(i => i.IsGenericType);
                return new[] { requestHandlerType }
                    .Contains(@interface.GetGenericTypeDefinition());
            }).ToList();

        var allPipelines = allTypes
            .Where(p =>
            {
                var @interface = p.GetInterfaces().First(i => i.IsGenericType);
                return new[] { pipelineBehaviorType }
                    .Contains(@interface.GetGenericTypeDefinition());
            }).ToList();

        foreach (var handler in allHandlers)
        {
            object key = handler.GUID;
            var handlerType = requestHandlerType;
            var behaviorType = pipelineBehaviorType;

            services.AddKeyedScoped(typeof(IRequestHandler), key, handler);

            var handlerInterface = handler.GetInterfaces()
                .First(p => p.IsGenericType && p.GetGenericTypeDefinition() == handlerType);

            // find pipelines
            if (withPipelines)
            {
                var pipelines = allPipelines
                    .Where(p =>
                    {
                        var interfaces = p.GetInterfaces();
                        if (p.IsGenericType)
                        {
                            // handle generic pipelines
                            return interfaces
                                       .FirstOrDefault(inter =>
                                           inter.IsGenericType &&
                                           inter.GetGenericTypeDefinition() == behaviorType)
                                       ?.GetInterfaces().First().GetGenericTypeDefinition() ==
                                   handlerInterface.GetGenericTypeDefinition();
                        }

                        return interfaces
                            .FirstOrDefault(inter =>
                                inter.IsGenericType &&
                                inter.GetGenericTypeDefinition() == behaviorType)
                            ?.GetInterfaces().First() == handlerInterface;
                    }).ToList();

                // Sort pipelines by the specified order passed via ConfigurationOptions
                if (pipelineOrder is { Count: > 0 })
                {
                    pipelines = pipelines
                        .OrderBy(p =>
                        {
                            var idx = pipelineOrder.IndexOf(p);
                            return idx == -1 ? int.MaxValue : idx;
                        })
                        .ToList();
                    pipelines.Reverse();
                }

                foreach (var pipeline in pipelines)
                {
                    if (pipeline.IsGenericType)
                    {
                        var genericHandlerResponseType = pipeline.GetInterfaces().First(inter =>
                            inter.IsGenericType &&
                            inter.GetGenericTypeDefinition() == behaviorType).GenericTypeArguments[1];

                        var genericHandlerResponseIsAwaitable = IsAwaitable(genericHandlerResponseType);
                        var handlerResponseTypeIsAwaitable = IsAwaitable(handlerInterface.GenericTypeArguments[1]);
                        if (genericHandlerResponseIsAwaitable ^ handlerResponseTypeIsAwaitable)
                        {
                            continue;
                        }

                        var responseTypeArg = handlerInterface.GenericTypeArguments[1];
                        if (genericHandlerResponseIsAwaitable && handlerResponseTypeIsAwaitable)
                        {
                            var areGenericTypeArgumentsInHandlerInterfaceMismatched =
                                genericHandlerResponseType.IsGenericType &&
                                handlerInterface.GenericTypeArguments[1].IsGenericType &&
                                genericHandlerResponseType.GetGenericTypeDefinition() !=
                                handlerInterface.GenericTypeArguments[1].GetGenericTypeDefinition();

                            if (areGenericTypeArgumentsInHandlerInterfaceMismatched ||
                                genericHandlerResponseType.IsGenericType ^
                                handlerInterface.GenericTypeArguments[1].IsGenericType)
                            {
                                continue;
                            }

                            // register async generic pipelines
                            if (responseTypeArg.GenericTypeArguments.Any())
                            {
                                responseTypeArg = responseTypeArg.GenericTypeArguments[0];
                            }
                        }

                        var closedGenericType = pipeline.MakeGenericType(handlerInterface.GenericTypeArguments[0],
                            responseTypeArg);
                        services.AddKeyedScoped(typeof(IRequestHandler), key, closedGenericType);
                    }
                    else
                    {
                        services.AddKeyedScoped(typeof(IRequestHandler), key, pipeline);
                    }
                }
            }

            services.AddScoped(handlerInterface, sp =>
            {
                var pipelinesWithHandler = Unsafe
                    .As<IRequestHandler[]>(sp.GetKeyedServices<IRequestHandler>(key));

                IRequestHandler lastPipeline = pipelinesWithHandler[0];
                for (int i = 1; i < pipelinesWithHandler.Length; i++)
                {
                    var pipeline = pipelinesWithHandler[i];
                    pipeline.SetNext(lastPipeline);
                    lastPipeline = pipeline;
                }

                return lastPipeline;
            });
        }
    }

    private static bool IsAwaitable(Type type)
    {
        if (type == typeof(Task) || type == typeof(ValueTask))
            return true;

        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition();
            return genericDef == typeof(Task<>) || genericDef == typeof(ValueTask<>) ||
                   genericDef == typeof(IAsyncEnumerable<>);
        }

        return false;
    }
}

//--------------------------------------------------------------------------------
// Request
//--------------------------------------------------------------------------------

public interface IRequest;

public interface IRequest<TRequest, TResponse> : IRequest where TRequest : class;

public interface IRequestHandler
{
    [ExcludeFromCodeCoverage]
    internal void SetNext(object handler)
    {
    }
}
public interface IRequestHandler<in TRequest, out TResponse> : IRequestHandler where TRequest : class, IRequest
{
    TResponse Handle(TRequest request, CancellationToken cancellationToken);
}

public interface IPipelineBehavior<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : class, IRequest<TRequest, TResponse>
{
    public IRequestHandler<TRequest, TResponse> NextPipeline { get; set; }

    void IRequestHandler.SetNext(object handler)
    {
        NextPipeline = Unsafe.As<IRequestHandler<TRequest, TResponse>>(handler);
    }
}

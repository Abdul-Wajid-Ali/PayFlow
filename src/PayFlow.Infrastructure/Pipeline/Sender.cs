// Infrastructure/Pipeline/Sender.cs
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Common.CQRS;

namespace PayFlow.Infrastructure.Pipeline;

public class Sender : ISender
{
    private readonly IServiceProvider _serviceProvider;

    public Sender(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(command.GetType(), typeof(TResponse));

        var handler = _serviceProvider.GetRequiredService(handlerType);

        return await ExecutePipeline<TResponse>(
            command,
            handlerType,
            handler,
            cancellationToken);
    }

    public async Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>)
            .MakeGenericType(query.GetType(), typeof(TResponse));

        var handler = _serviceProvider.GetRequiredService(handlerType);

        return await ExecutePipeline<TResponse>(
            query,
            handlerType,
            handler,
            cancellationToken);
    }

    private async Task<TResponse> ExecutePipeline<TResponse>(
        object request,
        Type handlerType,
        object handler,
        CancellationToken cancellationToken)
    {
        var requestType = request.GetType();

        // Resolve behaviors for this specific request/response pair
        var behaviorType = typeof(IPipelineBehavior<,>)
            .MakeGenericType(requestType, typeof(TResponse));

        var behaviorsType = typeof(IEnumerable<>)
            .MakeGenericType(behaviorType);

        var behaviors = ((IEnumerable<object>)_serviceProvider
            .GetRequiredService(behaviorsType))
            .ToList();

        // Core handler invocation at the end of the pipeline
        Func<Task<TResponse>> pipeline = () =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync")!;
            return (Task<TResponse>)handleMethod
                .Invoke(handler, new object[] { request, cancellationToken })!;
        };

        // Wrap behaviors in reverse — first registered executes first
        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var next = pipeline;
            var current = behavior;
            var handleMethod = behaviorType.GetMethod("HandleAsync")!;

            pipeline = () => (Task<TResponse>)handleMethod
                .Invoke(current, new object[] { request, next, cancellationToken })!;
        }

        return await pipeline();
    }
}
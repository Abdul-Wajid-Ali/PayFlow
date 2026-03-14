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
        // 1: Build the closed generic ICommandHandler type for this specific command
        var handlerType = typeof(ICommandHandler<,>)
            .MakeGenericType(command.GetType(), typeof(TResponse));

        // 2: Resolve the matching command handler from the DI container
        var handler = _serviceProvider.GetRequiredService(handlerType);

        // 3: Run the command through the full behavior pipeline before reaching the handler
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
        // 1: Build the closed generic IQueryHandler type for this specific query
        var handlerType = typeof(IQueryHandler<,>)
            .MakeGenericType(query.GetType(), typeof(TResponse));

        // 2: Resolve the matching query handler from the DI container
        var handler = _serviceProvider.GetRequiredService(handlerType);

        // 3: Run the query through the full behavior pipeline before reaching the handler
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
        // 1: Get the concrete runtime type of the request (e.g. RegisterCommand)
        var requestType = request.GetType();

        // 2: Build the closed generic IPipelineBehavior type for this request/response pair
        var behaviorType = typeof(IPipelineBehavior<,>)
            .MakeGenericType(requestType, typeof(TResponse));

        // 3: Wrap in IEnumerable so DI returns all behaviors registered for this pair
        var behaviorsType = typeof(IEnumerable<>)
            .MakeGenericType(behaviorType);

        // 4: Resolve all matching behaviors from DI (e.g. ValidationBehavior)
        var behaviors = ((IEnumerable<object>)_serviceProvider
            .GetRequiredService(behaviorsType))
            .ToList();

        // 5: Define the innermost step — the actual handler invocation via reflection
        Func<Task<TResponse>> pipeline = () =>
        {
            var handleMethod = handlerType.GetMethod("HandleAsync")!;
            return (Task<TResponse>)handleMethod
                .Invoke(handler, [request, cancellationToken])!;
        };

        // 6: Wrap behaviors around the pipeline in reverse so first-registered runs first
        foreach (var behavior in behaviors.AsEnumerable().Reverse())
        {
            var next = pipeline;
            var current = behavior;
            var handleMethod = behaviorType.GetMethod("HandleAsync")!;

            pipeline = () => (Task<TResponse>)handleMethod
                .Invoke(current, [request, next, cancellationToken])!;
        }

        // 7: Execute the fully composed pipeline from outermost behavior to handler
        return await pipeline();
    }
}
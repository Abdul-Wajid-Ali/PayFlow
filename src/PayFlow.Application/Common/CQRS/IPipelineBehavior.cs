namespace PayFlow.Application.Common.CQRS
{
    // Interface for pipeline behavior in CQRS pattern
    public interface IPipelineBehavior<TRequest, TResponse>
    {
        Task<TResponse> HandleAsync(
            TRequest request,
            Func<Task<TResponse>> next,
            CancellationToken cancellationToken = default);
    }
}
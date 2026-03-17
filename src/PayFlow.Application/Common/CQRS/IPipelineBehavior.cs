namespace PayFlow.Application.Common.CQRS
{
    // Interface for pipeline behavior in CQRS pattern
    public interface IPipelineBehavior<TRequest, TResponse> : MediatR.IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    { }
}
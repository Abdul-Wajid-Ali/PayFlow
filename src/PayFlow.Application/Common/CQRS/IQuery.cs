using MediatR;

namespace PayFlow.Application.Common.CQRS
{
    // Generic marker interface for queries that return a response of type TResponse.
    public interface IQuery<TResponse> : IRequest<TResponse>
    { }
}
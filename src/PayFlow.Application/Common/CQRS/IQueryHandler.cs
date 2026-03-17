using MediatR;

namespace PayFlow.Application.Common.CQRS
{
    // Generic interface for handling queries that return a response of type TResponse.
    public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
         where TQuery : IQuery<TResponse>
    { }
}
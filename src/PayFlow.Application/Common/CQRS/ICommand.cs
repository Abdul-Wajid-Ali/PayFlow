using MediatR;

namespace PayFlow.Application.Common.CQRS
{
    // Generic marker interface for commands that return a response of type TResponse.
    public interface ICommand<TResponse> : IRequest<TResponse>
    { }
}
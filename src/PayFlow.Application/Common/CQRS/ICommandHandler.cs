using MediatR;

namespace PayFlow.Application.Common.CQRS
{
    // Generic interface for handling commands that return a response of type TResponse.
    public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
            where TCommand : ICommand<TResponse>
    { }
}
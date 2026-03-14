namespace PayFlow.Application.Common.CQRS
{
    // Generic marker interface for commands that return a response of type TResponse.
    public interface ICommand<TResponse> : IBaseCommand
    { }

    // Base marker interface for all command types, used for type safety and to group command-related interfaces.
    public interface IBaseCommand
    { }
}
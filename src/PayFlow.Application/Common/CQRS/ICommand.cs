namespace PayFlow.Application.Common.CQRS
{
    // Marker interface for commands that do not return a response.
    public interface ICommand : IBaseCommand
    { }

    // Generic marker interface for commands that return a response of type TResponse.
    public interface ICommand<TResponse> : IBaseCommand
    { }

    // Base marker interface for all command types, used for type safety and to group command-related interfaces.
    public interface IBaseCommand
    { }
}
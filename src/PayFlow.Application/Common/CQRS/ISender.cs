namespace PayFlow.Application.Common.CQRS
{
    public interface ISender
    {
        // Method to send a command and receive a response
        Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

        // Method to execute a query and receive a response
        Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
    }
}
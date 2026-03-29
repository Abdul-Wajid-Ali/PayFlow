namespace PayFlow.Application.Common.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default);
    }
}
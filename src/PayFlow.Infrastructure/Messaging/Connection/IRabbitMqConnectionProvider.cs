using RabbitMQ.Client;

namespace PayFlow.Infrastructure.Messaging.Connection
{
    public interface IRabbitMqConnectionProvider
    {
        IConnection Connection { get; }
    }
}
using PayFlow.Application.Common.Interfaces;
using PayFlow.Infrastructure.Messaging.Connection;
using RabbitMQ.Client;
using System.Text;

namespace PayFlow.Infrastructure.Messaging
{
    public class RabbitMqPublisher : IEventPublisher
    {
        private readonly IRabbitMqConnectionProvider _manager;

        public RabbitMqPublisher(RabbitMqConnectionManager manager)
        => _manager = manager;

        public async Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default)
        {
            //1: Open a short-lived channel for this publish operation
            using var channel = await _manager.Connection.CreateChannelAsync(cancellationToken: cancellationToken);

            //2: Set message durability and content type
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
            };

            //3: Serialize and publish to the topic exchange with the specified routing key
            var body = Encoding.UTF8.GetBytes(payload);

            await channel.BasicPublishAsync(
                exchange: RabbitMqTopologyInitializer.Exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
    }
}
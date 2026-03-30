using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayFlow.Infrastructure.Messaging.Connection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace PayFlow.Infrastructure.Messaging.Consumers
{
    public class NotificationConsumer : BackgroundService
    {
        private IChannel? _channel;

        private readonly IRabbitMqConnectionProvider _connectionProvider;
        private readonly ILogger<NotificationConsumer> _logger;

        public NotificationConsumer(ILogger<NotificationConsumer> logger, RabbitMqConnectionManager connectionManager)
        {
            _logger = logger;
            _connectionProvider = connectionManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationConsumer starting...");

            //1: Open a dedicated channel for this consumer — one channel per consumer is the RabbitMQ best practice
            _channel = await _connectionProvider.Connection.CreateChannelAsync(cancellationToken: stoppingToken);

            //2: Limit broker to one unacked message at a time — ensures sequential processing
            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false,
                cancellationToken: stoppingToken);

            //3: Create an async consumer and wire up the message handler before registering with the broker
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, args) =>
            {
                //4: Deserialize the message body and extract routing key for dispatch
                var body = Encoding.UTF8.GetString(args.Body.ToArray());
                var routingKey = args.RoutingKey;

                _logger.LogDebug("NotificationConsumer received message. RoutingKey: {RoutingKey}", routingKey);

                try
                {
                    //5: Process the message — dispatch based on routing key
                    HandleMessage(routingKey, body);

                    //6: Ack on success — broker removes the message from the queue
                    await _channel.BasicAckAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "NotificationConsumer failed to process message. RoutingKey: {RoutingKey}", routingKey);

                    //7: Nack without requeue — prevents poison message infinite loop
                    await _channel.BasicNackAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        requeue: false,
                        cancellationToken: stoppingToken);
                }
            };

            //8: Register consumer with the broker — messages start flowing after this call
            await _channel.BasicConsumeAsync(
                consumer: consumer,
                queue: RabbitMqTopologyInitializer.NotificationQueue,
                autoAck: false,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "NotificationConsumer listening on queue: {Queue}",
                RabbitMqTopologyInitializer.NotificationQueue);
        }

        // Simple dispatch method to simulate different notification types based on routing key
        private void HandleMessage(string routingKey, string payload)
        {
            switch (routingKey)
            {
                case "user.registered":
                    _logger.LogInformation(
                        "[NOTIFICATION] Welcome email simulated. Payload: {Payload}", payload);
                    break;

                case "transfer.completed":
                    _logger.LogInformation(
                        "[NOTIFICATION] Transfer receipt simulated. Payload: {Payload}", payload);
                    break;

                default:
                    _logger.LogWarning(
                        "NotificationConsumer received unknown routing key: {RoutingKey}", routingKey);
                    break;
            }
        }

        // Clean up the channel on shutdown — ensures graceful disconnection from the broker
        public override async void Dispose()
        {
            _logger.LogInformation("NotificationConsumer shutting down. Closing channel.");

            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }

            base.Dispose();
        }
    }
}
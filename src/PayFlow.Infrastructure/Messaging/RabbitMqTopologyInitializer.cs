using PayFlow.Application.Common.Constants;
using RabbitMQ.Client;

namespace PayFlow.Infrastructure.Messaging
{
    public static class RabbitMqTopologyInitializer
    {
        public const string Exchange = "app-exchange";

        public const string NotificationQueue = "notification-queue";

        public const string TransferProcessingQueue = "transfer-processing-queue ";

        public const string CacheUpdateQueue = "cache-update-queue";

        public static async Task InitializeAsync(IConnection connection, CancellationToken cancellationToken)
        {
            // 1: Create a channel for declaring the topology (exchanges, queues, bindings)
            using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            //2: Declare a durable topic exchange — routes messages to queues by routing key pattern
            await channel.ExchangeDeclareAsync(
                exchange: Exchange,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var queueArgs = new Dictionary<string, object?>();

            // 3:  Declare durable queues — survive broker restarts, no exclusive ownership
            // For different processing purposes (notifications, transfer processing, cache updates)
            await channel.QueueDeclareAsync(
                queue: NotificationQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: TransferProcessingQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken);

            await channel.QueueDeclareAsync(
                queue: CacheUpdateQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArgs,
                cancellationToken: cancellationToken);

            // 4: Bind queues to the exchange with specific routing keys — determines which messages go to which queues

            // 4a: transfer-processing-queue ← user.registered, transfer.completed (for processing transfers after user registration and transfer completion)
            await channel.QueueBindAsync(
                queue: NotificationQueue,
                exchange: Exchange,
                routingKey: DomainEvents.UserRegistered,
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: NotificationQueue,
                exchange: Exchange,
                routingKey: DomainEvents.TransferCompleted,
                cancellationToken: cancellationToken);

            // 4b: notification-queue ← transfer.requested (for sending notifications about transfer requests)
            await channel.QueueBindAsync(
                queue: TransferProcessingQueue,
                exchange: Exchange,
                routingKey: DomainEvents.TransferRequested,
                cancellationToken: cancellationToken);

            // 4c: cache-update-queue ← wallet.balance.changed (for updating cached wallet balances after changes)
            await channel.QueueBindAsync(
                queue: CacheUpdateQueue,
                exchange: Exchange,
                routingKey: DomainEvents.WalletBalanceChanged,
                cancellationToken: cancellationToken);
        }
    }
}
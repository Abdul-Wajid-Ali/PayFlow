using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Application.Common.Models;
using PayFlow.Domain.Events;
using PayFlow.Infrastructure.Messaging.Connection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PayFlow.Infrastructure.Messaging.Consumers
{
    public class CacheUpdateConsumer : BackgroundService
    {
        private IChannel? _channel;

        private readonly ILogger _logger;
        private readonly IWalletCacheService _cacheService;
        private readonly IRabbitMqConnectionProvider _connectionProvider;

        public CacheUpdateConsumer(
            IWalletCacheService cacheService,
            ILogger<CacheUpdateConsumer> logger,
            RabbitMqConnectionManager connectionManager)
        {
            _logger = logger;
            _cacheService = cacheService;
            _connectionProvider = connectionManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CacheUpdateConsumer starting...");

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

                _logger.LogDebug("CacheUpdateConsumer received message. RoutingKey: {RoutingKey}", routingKey);

                try
                {
                    //5: Process the message — dispatch based on routing key
                    await HandleMessageAsync(body, stoppingToken);

                    //6: Ack on success — broker removes the message from the queue
                    await _channel.BasicAckAsync(deliveryTag: args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "CacheUpdateConsumer failed to process message. RoutingKey: {RoutingKey}", routingKey);

                    //7: Nack without requeue — prevents poison message infinite loop
                    await _channel.BasicNackAsync(
                        deliveryTag: args.DeliveryTag,
                        requeue: false,
                        multiple: false,
                        cancellationToken: stoppingToken);
                }
            };

            //8: Register consumer with the broker — messages start flowing after this call
            await _channel.BasicConsumeAsync(
                autoAck: false,
                consumer: consumer,
                queue: RabbitMqTopologyInitializer.CacheUpdateQueue,
                cancellationToken: stoppingToken);
        }

        private async Task HandleMessageAsync(string payload, CancellationToken stoppingToken)
        {
            // 1: Deserialize WalletBalanceChangedEvent from payload
            var balanceChangedEvent = JsonSerializer.Deserialize<WalletBalanceChangedEvent>(payload);

            // 2: If payload was empty, fail fast
            if (balanceChangedEvent is null)
            {
                _logger.LogWarning("CacheUpdateConsumer received null payload — skipping.");
                return;
            }

            // 4: Updating Redis cache with new balance
            await _cacheService.SetBalanceAsync(
                result: new WalletCacheResult(
                        WalletId: balanceChangedEvent.WalletId,
                        UserId: balanceChangedEvent.UserId,
                        Currency: balanceChangedEvent.Currency,
                        Balance: balanceChangedEvent.NewBalance
                    ),
                userId: balanceChangedEvent.UserId,
                cancellationToken: stoppingToken
            );

            _logger.LogInformation(
            "[CACHE UPDATE] Balance updated for WalletId {WalletId}, NewBalance {NewBalance} {Currency}",
            balanceChangedEvent.WalletId, balanceChangedEvent.NewBalance, balanceChangedEvent.Currency);
        }

        public override async void Dispose()
        {
            _logger.LogInformation("CacheUpdateConsumer shutting down. Closing channel.");

            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }

            base.Dispose();
        }
    }
}
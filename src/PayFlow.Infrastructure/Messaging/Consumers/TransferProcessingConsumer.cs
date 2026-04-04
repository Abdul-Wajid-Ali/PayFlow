using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Constants;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Enums;
using PayFlow.Domain.Events;
using PayFlow.Domain.Exceptions;
using PayFlow.Domain.Interfaces;
using PayFlow.Infrastructure.Messaging.Connection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PayFlow.Infrastructure.Messaging.Consumers
{
    public class TransferProcessingConsumer : BackgroundService
    {
        private IChannel? _channel;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TransferProcessingConsumer> _logger;
        private readonly IRabbitMqConnectionProvider _connectionProvider;
        private readonly IDateTimeProvider _dateTimeProvider;

        public TransferProcessingConsumer(
            IServiceScopeFactory scopeFactory,
            ILogger<TransferProcessingConsumer> logger,
            IRabbitMqConnectionProvider connectionProvider,
            IDateTimeProvider dateTimeProvider)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _connectionProvider = connectionProvider;
            _dateTimeProvider = dateTimeProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TransferProcessingConsumer starting...");

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

                _logger.LogInformation(
                    "TransferProcessingConsumer received message. RoutingKey: {RoutingKey}", routingKey);

                try
                {
                    //5: Process the message — dispatch based on routing key
                    await HandleMessageAsync(body, args.RoutingKey, stoppingToken);

                    //6: Ack on success — broker removes the message from the queue
                    await _channel.BasicAckAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        cancellationToken: stoppingToken);

                    _logger.LogInformation(
                        "TransferProcessingConsumer acknowledged message. RoutingKey: {RoutingKey}.", routingKey);
                }
                catch (NonRecoverableTransferException ex)
                {
                    _logger.LogWarning(ex,
                        "TransferProcessingConsumer non-recoverable failure. RoutingKey: {RoutingKey}.", routingKey);

                    //7: Nack without requeue — prevents poison message infinite loop
                    await _channel.BasicNackAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        requeue: false,     // ← discard, retrying won't help
                        cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "TransferProcessingConsumer transient failure. RoutingKey: {RoutingKey}. Message will be requeued.",
                        routingKey);

                    //8: Nack with requeue — requeue to resolve transient errors
                    await _channel.BasicNackAsync(
                        deliveryTag: args.DeliveryTag,
                        multiple: false,
                        requeue: true,      // ← requeue for retry on transient errors
                        cancellationToken: stoppingToken);
                }
            };

            //8: Register consumer with the broker — messages start flowing after this call
            await _channel.BasicConsumeAsync(
                queue: RabbitMqTopologyInitializer.TransferProcessingQueue,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "TransferProcessingConsumer listening on queue: {Queue}",
                RabbitMqTopologyInitializer.TransferProcessingQueue);

            //9: Keeps the background service alive indefinitely until a shutdown signal is received via the cancellation token.
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }

        private async Task HandleMessageAsync(string payload, string routingKey, CancellationToken ct)
        {
            // 1: Deserialize TransferRequestedEvent from payload
            var requestedEvent = JsonSerializer.Deserialize<TransferRequestedEvent>(payload);

            // 2: If payload was empty, fail fast
            if (requestedEvent is null)
            {
                _logger.LogWarning(
                    "TransferProcessingConsumer deserialization returned null. Payload: {Payload}", payload);

                throw new NonRecoverableTransferException("Deserialization returned null.");
            }

            // 3: Create scoped services for this message processing
            var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var walletRepo = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var transactionRepo = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();

            // 4: Fetch transaction from database
            var transaction = await transactionRepo.GetByIdAsync(id: requestedEvent.TransactionId, cancellationToken: ct);

            // 5: If transaction not found, treat as non-recoverable
            if (transaction is null)
            {
                _logger.LogWarning(
                    "TransferProcessingConsumer: Transaction not found. TransactionId: {TransactionId}", requestedEvent.TransactionId);

                throw new NonRecoverableTransferException($"Transaction {requestedEvent.TransactionId} not found.");
            }

            // 6: Skip processing if transaction already handled (idempotency)
            if (transaction.Status != TransactionStatus.Pending)
            {
                _logger.LogInformation(
                    "TransferProcessingConsumer: Transaction already processed. TransactionId: {TransactionId}, Status: {Status}. Skipping.",
                    transaction.Id, transaction.Status);

                return;
            }

            // 7: Load sender and receiver wallets
            var senderWallet = await walletRepo.GetByIdAsync(walletId: requestedEvent.FromWalletId, cancellationToken: ct);
            var receiverWallet = await walletRepo.GetByIdAsync(walletId: requestedEvent.ToWalletId, cancellationToken: ct);

            // 8: Validate both wallets exist before proceeding
            if (senderWallet is null || receiverWallet is null)
            {
                _logger.LogError(
                    "TransferProcessingConsumer: Wallet not found. FromWalletId: {From}, ToWalletId: {To}",
                    requestedEvent.FromWalletId, requestedEvent.ToWalletId);

                transaction.MarkFailed();
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);

                throw new NonRecoverableTransferException("One or both wallets not found.");
            }

            // 9: Validate currency consistency between wallets and event
            if (senderWallet.Currency != requestedEvent.Currency ||
                receiverWallet.Currency != requestedEvent.Currency)
            {
                _logger.LogError(
                    "TransferProcessingConsumer: Currency mismatch. Transfer={TransferCurrency}, Sender={SenderCurrency}, Receiver={ReceiverCurrency}",
                    requestedEvent.Currency, senderWallet.Currency, receiverWallet.Currency);

                transaction.MarkFailed();
                await unitOfWork.SaveChangesAsync(ct);

                throw new NonRecoverableTransferException("Currency mismatch.");
            }

            // 10: Prevent self-transfer at consumer level
            if (senderWallet.Id == receiverWallet.Id)
            {
                _logger.LogWarning(
                    "TransferProcessingConsumer: Self-transfer detected. WalletId: {WalletId}",
                    senderWallet.Id);

                transaction.MarkFailed();
                await unitOfWork.SaveChangesAsync(ct);

                throw new NonRecoverableTransferException("Self-transfer is not allowed.");
            }

            // 11: Ensure sender has sufficient balance at execution time
            if (senderWallet.Balance < requestedEvent.Amount)
            {
                _logger.LogWarning(
                    @"TransferProcessingConsumer: Insufficient balance at execution time.
            WalletId: {WalletId}, Balance: {Balance}, Required: {Amount}",
                    senderWallet.Id, senderWallet.Balance, requestedEvent.Amount);

                transaction.MarkFailed();
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);

                throw new NonRecoverableTransferException("Insufficient balance at execution time.");
            }

            // 12: Apply debit and credit operations
            senderWallet.Debit(requestedEvent.Amount);
            receiverWallet.Credit(requestedEvent.Amount);

            // 11: Mark transaction as successfully completed
            transaction.MarkCompleted();

            // 12: Create TransferCompleted domain event
            var completedEvent = new TransferCompletedEvent(
                TransactionId: transaction.Id,
                FromWalletId: transaction.FromWalletId,
                ToWalletId: transaction.ToWalletId,
                Amount: transaction.Amount,
                Currency: transaction.Currency,
                CompletedAt: _dateTimeProvider.UtcNow);

            // 13: Persist TransferCompleted event to outbox
            await outboxRepo.AddAsync(
                message: OutboxMessage.Create(
                    eventType: DomainEvents.TransferCompleted,
                    payload: JsonSerializer.Serialize(completedEvent),
                    routingKey: routingKey,
                    createdAt: _dateTimeProvider.UtcNow),
                cancellationToken: ct);

            // 14: Create sender wallet balance changed event
            var senderBalanceEvent = new WalletBalanceChangedEvent(
                WalletId: senderWallet.Id,
                UserId: senderWallet.UserId,
                NewBalance: senderWallet.Balance,
                Currency: senderWallet.Currency,
                UpdatedAt: completedEvent.CompletedAt);

            // 15: Persist sender balance change event to outbox
            await outboxRepo.AddAsync(OutboxMessage.Create(
                    eventType: DomainEvents.WalletBalanceChanged,
                    payload: JsonSerializer.Serialize(senderBalanceEvent),
                    routingKey: routingKey,
                    createdAt: _dateTimeProvider.UtcNow),
                cancellationToken: ct);

            // 16: Create receiver wallet balance changed event
            var receiverBalanceEvent = new WalletBalanceChangedEvent(
                WalletId: receiverWallet.Id,
                UserId: receiverWallet.UserId,
                NewBalance: receiverWallet.Balance,
                Currency: receiverWallet.Currency,
                UpdatedAt: completedEvent.CompletedAt);

            // 17: Persist receiver balance change event to outbox
            await outboxRepo.AddAsync(OutboxMessage.Create(
                    eventType: DomainEvents.WalletBalanceChanged,
                    payload: JsonSerializer.Serialize(receiverBalanceEvent),
                    routingKey: routingKey,
                    createdAt: _dateTimeProvider.UtcNow),
                cancellationToken: ct);

            // 18: Commit all DB changes and outbox messages atomically
            await unitOfWork.SaveChangesAsync(ct);

            // 19: Log successful processing and event publishing
            _logger.LogInformation(
                @"TransferProcessingConsumer: DB committed. 3 outbox messages queued for downstream events.
                 TransactionId: {TransactionId}", transaction.Id);
        }

        // Clean up the channel on shutdown — ensures graceful disconnection from the broker
        public override async void Dispose()
        {
            _logger.LogInformation("TransferProcessingConsumer shutting down. Closing channel.");

            if (_channel is not null)
            {
                await _channel.CloseAsync();
                _channel.Dispose();
            }

            base.Dispose();
        }
    }
}
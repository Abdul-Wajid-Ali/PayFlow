using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Interfaces;

namespace PayFlow.Infrastructure.Messaging
{
    public class OutboxWorker : BackgroundService
    {
        private const int MaxRetries = 3;
        private const int BatchSize = 20;
        private const int PollingIntervalSeconds = 10;

        private readonly ILogger<OutboxWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDateTimeProvider _dateTimeProvider;

        public OutboxWorker(ILogger<OutboxWorker> logger, IServiceScopeFactory scopeFactory, IDateTimeProvider dateTimeProvider )
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _dateTimeProvider = dateTimeProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox worker started. Poll interval: {Interval}s", PollingIntervalSeconds);

            // Main loop: iteratively process batches of pending outbox messages until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessBatchAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), cancellationToken: stoppingToken);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            // 1: Create a new DI scope for this batch processing to ensure fresh instances of repositories and services
            using var scope = _scopeFactory.CreateScope();

            // 2: Resolve the dependencies needed for processing the outbox messages
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            // 3: Fetch pending outbox messages that are due for processing and return immediately if there are none
            var pendingMessages = await outboxRepo.GetPendingAsync(
                batchSize: BatchSize,
                dateTimeNow: _dateTimeProvider.UtcNow,
                cancellationToken);

            if (pendingMessages.Count == 0)
                return;

            _logger.LogInformation("Outbox worker: processing {Count} pending message(s)", pendingMessages.Count);

            // 4: Process each pending message in the batch
            foreach (var item in pendingMessages)
            {
                try
                { // 4a: Attempt to publish the message using the event publisher service
                    await publisher.PublishAsync(
                        payload: item.Payload,
                        routingKey: item.RoutingKey,
                        cancellationToken: cancellationToken);

                    // 4b: If publish succeeds, mark the message as processed with the current timestamp
                    item.MarkAsProcessed(_dateTimeProvider.UtcNow);

                    _logger.LogInformation("Outbox message published. Id: {Id}, EventType: {EventType}, RoutingKey: {RoutingKey}",
                        item.Id, item.EventType, item.RoutingKey);
                }
                catch (Exception ex)
                {
                    // 4c: If publish fails, record the failure details on the message, including error message, retry count, and next retry time
                    item.RecordFailure(
                        errorMessage: ex.Message,
                        maxTries: MaxRetries,
                        dateTimeNow: _dateTimeProvider.UtcNow);

                    // 4d: Log a warning if the message will be retried, or an error if it has been dead-lettered after exceeding max retries
                    if (item.DeadLetteredAt.HasValue)
                        _logger.LogError(
                        "Outbox message dead-lettered after {MaxRetries} retries. Id: {Id}, EventType: {EventType}, LastError: {Error}",
                        MaxRetries, item.Id, item.EventType, item.LastError);
                    else
                        _logger.LogWarning(
                            "Outbox message publish failed. Id: {Id}, RetryCount: {RetryCount}, NextRetryAt: {NextRetryAt}, Error: {Error}",
                            item.Id, item.RetryCount, item.NextRetryAt, item.LastError);
                }
            }

            // 5: After processing the batch, save all changes to the database in a single transaction
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
namespace PayFlow.Domain.Entities
{
    public class OutboxMessage
    {
        public Guid Id { get; private set; }

        public string EventType { get; private set; } = default!;

        public string Payload { get; private set; } = default!;

        public string RoutingKey { get; private set; } = default!;

        public DateTime CreatedAt { get; private set; }

        public DateTime? ProcessedAt { get; private set; }

        public DateTime? DeadLetteredAt { get; private set; }

        public string? LastError { get; private set; }

        public DateTime? NextRetryAt { get; private set; }

        public int RetryCount { get; private set; }

        // Private constructor to enforce use of factory method
        private OutboxMessage()
        { }

        // Factory method to create a new outbox message
        public static OutboxMessage Create(
            string eventType,
            string payload,
            string routingKey,
            DateTime createdAt)
        => new()
        {
            EventType = eventType,
            Payload = payload,
            RoutingKey = routingKey,
            CreatedAt = createdAt,
            RetryCount = 0
        };

        // Mark the message as successfully processed
        public void MarkAsProcessed(DateTime processedAt)
        {
            ProcessedAt = processedAt;
        }

        public void RecordFailure(string errorMessage, int maxTries, DateTime dateTimeNow)
        {
            // 1. Increment retry attempt count and record error message (truncated to 1000 chars if too long)
            RetryCount++;
            LastError = errorMessage.Length > 1000 ? errorMessage[..1000] : errorMessage;

            //2: Check if we've reached the maximum retry attempts, if yes mark as dead-lettered
            if (RetryCount >= maxTries)
                DeadLetteredAt = dateTimeNow;
            else
            {
                //3: Calculate next retry time using exponential backoff strategy and set next retry time
                var delaySeconds = Math.Pow(2, RetryCount) * 5;  // 10s, 20s, 40s... capped at 3 retries
                NextRetryAt = dateTimeNow.AddSeconds(delaySeconds);
            }
        }
    }
}
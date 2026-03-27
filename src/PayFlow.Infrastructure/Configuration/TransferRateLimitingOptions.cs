namespace PayFlow.Infrastructure.Configuration
{
    // Defines configuration options controlling rate limiting behavior for transfer operations.
    public sealed class TransferRateLimitingOptions
    {
        public const string SectionName = "RateLimiting:Transfer";

        public int PermitLimit { get; set; } = 5;

        public int WindowSeconds { get; set; } = 60;

        public int QueueLimit { get; set; } = 0;
    }
}
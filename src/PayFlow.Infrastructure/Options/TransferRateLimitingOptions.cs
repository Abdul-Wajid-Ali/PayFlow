using System.ComponentModel.DataAnnotations;

namespace PayFlow.Infrastructure.Options
{
    // Defines configuration options controlling rate limiting behavior for transfer operations.
    public sealed class TransferRateLimitingOptions
    {
        public const string SectionName = "RateLimiting:Transfer";

        [Range(1, 1000, ErrorMessage = "PermitLimit must be between 1 and 1000.")]
        public int PermitLimit { get; set; } = 5;

        [Range(1, 3600, ErrorMessage = "WindowSeconds must be between 1 and 3600.")]
        public int WindowSeconds { get; set; } = 60;

        [Range(0, 1000, ErrorMessage = "QueueLimit must be between 0 and 1000.")]
        public int QueueLimit { get; set; } = 0;
    }
}
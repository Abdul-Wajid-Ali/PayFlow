using System.ComponentModel.DataAnnotations;

namespace PayFlow.Infrastructure.Options
{
    public sealed class RedisOptions
    {
        public const string SectionName = "Redis";

        [Required]
        public string ConnectionString { get; init; } = string.Empty;
    }
}
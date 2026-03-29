using System.ComponentModel.DataAnnotations;

namespace PayFlow.Infrastructure.Options
{
    // Represents RabbitMQ connection configuration used for establishing messaging infrastructure communication.
    public sealed class RabbitMqOptions
    {
        public const string SectionName = "RabbitMQ";

        [Required]
        public string HostName { get; init; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int Port { get; init; } = 5672;

        [Required]
        public string UserName { get; init; } = string.Empty;

        [Required]
        public string Password { get; init; } = string.Empty;

        [Required]
        public string VirtualHost { get; init; } = "/";
    }
}
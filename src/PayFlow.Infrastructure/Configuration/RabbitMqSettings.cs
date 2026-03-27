namespace PayFlow.Infrastructure.Configuration
{
    // Represents RabbitMQ connection configuration used for establishing messaging infrastructure communication.
    public sealed class RabbitMqSettings
    {
        public const string SectionName = "RabbitMQ";

        public string HostName { get; init; } = string.Empty;
        public int Port { get; init; } = 5672;
        public string UserName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public string VirtualHost { get; init; } = "/";
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayFlow.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace PayFlow.API.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1: Get SQL Server ConnectionString from AppSettings, throw Exception if not available
            var sqlConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Configuration value 'ConnectionStrings:DefaultConnection' is not configured.");

            // 2: Get Redis ConnectionString from AppSettings, throw Exception if not available
            var redisConnectionString = configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Configuration value 'Redis:ConnectionString' is not configured.");

            var rabbitMqSettings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
                ?? throw new InvalidOperationException($"Configuration section '{RabbitMqSettings.SectionName}' is missing or invalid.");

            var rabbitMqConnectionString = BuildRabbitMqConnectionString(rabbitMqSettings);

            services.AddHealthChecks()
                .AddSqlServer(
                    connectionString: sqlConnectionString,
                    name: "sqlserver",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["db"])
                .AddRedis(
                    redisConnectionString: redisConnectionString,
                    name: "redis",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["cache"])
                .AddRabbitMQ(
                    factory: sp =>
                    {
                        var factory = new ConnectionFactory
                        {
                            Uri = new Uri(rabbitMqConnectionString)
                        };

                        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                    },
                    name: "rabbitmq",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["messaging"]);

            return services;
        }

        private static string BuildRabbitMqConnectionString(RabbitMqSettings settings)
        {
            var userName = Uri.EscapeDataString(settings.UserName);
            var password = Uri.EscapeDataString(settings.Password);
            var virtualHost = settings.VirtualHost.Trim('/');
            var encodedVirtualHost = string.IsNullOrWhiteSpace(virtualHost)
                ? "%2F"
                : Uri.EscapeDataString(virtualHost);

            return $"amqp://{userName}:{password}@{settings.HostName}:{settings.Port}/{encodedVirtualHost}";
        }
    }
}

using Microsoft.Extensions.Diagnostics.HealthChecks;
using PayFlow.Infrastructure.Configuration;
using RabbitMQ.Client;

namespace PayFlow.API.Extensions
{
    public static class HealthCheckServiceCollectionExtensions
    {
        public static IServiceCollection AddDependencyHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            // 1: Retrieve SQL Server connection string from configuration, fail fast if missing
            var sqlConnectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            // 2: Retrieve Redis connection string from configuration, fail fast if missing
            var redisConnectionString = configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Configuration value 'Redis:ConnectionString' is not configured.");

            // 3: Bind RabbitMQ settings from configuration section, ensure valid configuration
            var rabbitMqSettings = configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
                ?? throw new InvalidOperationException($"Configuration section '{RabbitMqSettings.SectionName}' is missing or invalid.");

            // 4: Build RabbitMQ connection string from strongly typed settings
            var rabbitMqConnectionString = BuildRabbitMqConnectionString(rabbitMqSettings);

            // 5: Register health checks for all external dependencies (SQL Server, Redis, RabbitMQ)
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
                        // 6: Create RabbitMQ connection using connection string for health probe
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
            // 1: Encode credentials and virtual host to ensure valid URI format
            var userName = Uri.EscapeDataString(settings.UserName);
            var password = Uri.EscapeDataString(settings.Password);

            // 2: Normalize and encode virtual host (default "/" must be encoded as "%2F")
            var virtualHost = settings.VirtualHost.Trim('/');
            var encodedVirtualHost = string.IsNullOrWhiteSpace(virtualHost)
                ? "%2F"
                : Uri.EscapeDataString(virtualHost);

            // 3: Construct AMQP connection string
            return $"amqp://{userName}:{password}@{settings.HostName}:{settings.Port}/{encodedVirtualHost}";
        }
    }
}
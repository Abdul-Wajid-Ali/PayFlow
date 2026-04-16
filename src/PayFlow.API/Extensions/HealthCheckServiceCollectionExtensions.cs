using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PayFlow.Infrastructure.Options;
using RabbitMQ.Client;

namespace PayFlow.API.Extensions
{
    public static class HealthCheckServiceCollectionExtensions
    {
        public static IServiceCollection AddDependencyHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            // Register health checks for all external dependencies (database, cache, messaging)
            services.AddHealthChecks()

                // 1: PostgreSQL health check using connection string from configuration
                .AddNpgSql(
                    connectionStringFactory: _ =>
                        configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured."),
                    name: "postgres",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["db"])

                // 2: Resolves RedisOptions via DI and verifies broker connectivity
                .AddRedis(
                    connectionStringFactory: sp => sp.GetRequiredService<IOptions<RedisOptions>>().Value.ConnectionString,
                    name: "redis",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["cache"])

                // 3: Builds the connection string from RabbitMqOptions resolved via DI and attempts to connect
                .AddRabbitMQ(
                    factory: sp =>
                    {
                        var rabbitOptions = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                        var rabbitMqConnectionString = BuildRabbitMqConnectionString(rabbitOptions);

                        var factory = new ConnectionFactory
                        {
                            Uri = new Uri(rabbitMqConnectionString)
                        };

                        // Create connection to validate RabbitMQ availability
                        return factory.CreateConnectionAsync().GetAwaiter().GetResult();
                    },
                    name: "rabbitmq",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["messaging"]);

            return services;
        }

        private static string BuildRabbitMqConnectionString(RabbitMqOptions options)
        {
            // 1: Encode credentials and virtual host to ensure valid URI format
            var userName = Uri.EscapeDataString(options.UserName);
            var password = Uri.EscapeDataString(options.Password);

            // 2: Normalize and encode virtual host (default "/" must be encoded as "%2F")
            var virtualHost = options.VirtualHost.Trim('/');
            var encodedVirtualHost = string.IsNullOrWhiteSpace(virtualHost)
                ? "%2F"
                : Uri.EscapeDataString(virtualHost);

            // 3: Construct AMQP connection string
            return $"amqp://{userName}:{password}@{options.HostName}:{options.Port}/{encodedVirtualHost}";
        }
    }
}
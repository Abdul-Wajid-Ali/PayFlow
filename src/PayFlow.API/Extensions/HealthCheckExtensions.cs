using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PayFlow.API.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHealthChecks()
                .AddSqlServer(
                    connectionString: configuration.GetConnectionString("DefaultConnection")!,
                    name: "sqlserver",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["db"])
                .AddRedis(
                    redisConnectionString: configuration["Redis:ConnectionString"]!,
                    name: "redis",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["cache"])
                .AddRabbitMQ(
                    rabbitConnectionString: BuildRabbitMqConnectionString(configuration),
                    name: "rabbitmq",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: ["messaging"]);

            return services;
        }

        private static string BuildRabbitMqConnectionString(IConfiguration configuration)
        {
            var hostName = configuration["RabbitMQ:HostName"];
            var port = configuration["RabbitMQ:Port"];
            var userName = configuration["RabbitMQ:UserName"];
            var password = configuration["RabbitMQ:Password"];
            var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";

            var encodedVHost = Uri.EscapeDataString(virtualHost);

            return $"amqp://{userName}:{password}@{hostName}:{port}/{encodedVHost}";
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Infrastructure.Messaging;
using PayFlow.Infrastructure.Messaging.Connection;
using PayFlow.Infrastructure.Messaging.Consumers;
using PayFlow.Infrastructure.Options;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Persistence.Repositories;
using PayFlow.Infrastructure.Services;

namespace PayFlow.Infrastructure;

public static class DependencyInjection
{
    // Registers infrastructure services and external implementations
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1: Register unit of work and repository implementations
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<WalletRepository>(); // Register the concrete inner repository directly

        // 2: Decorator: IWalletRepository → CachedWalletRepository wrapping WalletRepository
        services.AddScoped<IWalletRepository>(sp =>
        new CachedWalletRepository(
            sp.GetRequiredService<WalletRepository>(),
            sp.GetRequiredService<IDistributedCache>(),
            sp.GetRequiredService<ILogger<CachedWalletRepository>>())
        );

        // 3: Register infrastructure services (JWT, password hashing, time provider)
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IEventPublisher, RabbitMqPublisher>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 4: Register EF Core DbContext with SQL Server
        services.AddDbContext<PayFlowDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            });
        });

        //5: Bind RedisOptions from appsettings.json and validate at startup
        services.AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 6: Register Redis cache services with default configuration (options will be configured via DI in step 4)
        services.AddStackExchangeRedisCache(_ => { });

        // 7: Configure Redis distributed cache by binding RedisOptions and applying them to RedisCacheOptions via DI
        services.AddOptions<RedisCacheOptions>()
            .Configure<IOptions<RedisOptions>>((cacheOptions, redisOptions) =>
            {
                cacheOptions.Configuration = redisOptions.Value.ConnectionString;
            });

        //8: Bind RabbitMqOptions from appsettings.json and validate at startup
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // 9: Register RabbitMQ connection as a singleton service and expose it via an interface for other services to use
        services.AddSingleton<RabbitMqConnectionManager>();
        services.AddSingleton<IRabbitMqConnectionProvider>(sp => sp.GetRequiredService<RabbitMqConnectionManager>());

        // 10: Register hosted services for RabbitMQ connection management and outbox processing
        // Register the same instance as a hosted service so the host manages its lifecycle
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConnectionManager>());
        services.AddHostedService<OutboxWorker>();
        services.AddHostedService<NotificationConsumer>();

        return services;
    }
}
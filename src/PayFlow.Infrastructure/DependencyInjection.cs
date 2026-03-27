using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Infrastructure.Configuration;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Persistence.Repositories;
using PayFlow.Infrastructure.Services;

namespace PayFlow.Infrastructure;

public static class DependencyInjection
{
    // Registers infrastructure services and external implementations
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1: Register EF Core DbContext with SQL Server
        services.AddDbContext<PayFlowDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // 2: Register repository implementations and unit of work
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 3: Register the concrete inner repository directly
        services.AddScoped<WalletRepository>();

        // 4: Register Redis distributed cache
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration["Redis:ConnectionString"];
        });

        // 5: Decorator: IWalletRepository → CachedWalletRepository wrapping WalletRepository
        services.AddScoped<IWalletRepository>(sp =>
        new CachedWalletRepository(
            sp.GetRequiredService<WalletRepository>(),
            sp.GetRequiredService<IDistributedCache>(),
            sp.GetRequiredService<ILogger<CachedWalletRepository>>())
        );

        // 6: Register infrastructure services (JWT, password hashing, time provider)
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 7: Bind JwtSettings configuration for IOptions<JwtSettings>
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        return services;
    }
}
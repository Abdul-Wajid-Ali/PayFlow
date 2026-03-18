using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Persistence.Repositories;
using PayFlow.Infrastructure.Services;
using PayFlow.Infrastructure.Settings;

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
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 3: Register infrastructure services (JWT, password hashing, time provider)
        services.AddScoped<IJwtService, JwtService>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 4: Bind JwtSettings configuration for IOptions<JwtSettings>
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        return services;
    }
}
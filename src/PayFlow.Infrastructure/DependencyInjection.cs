using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Persistence.Repositories;
using PayFlow.Infrastructure.Pipeline;
using PayFlow.Infrastructure.Services;

namespace PayFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DbContext with SQL Server provider
        services.AddDbContext<PayFlowDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Register repositories and unit of work
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<ISender, Sender>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
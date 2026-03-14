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
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<PayFlowDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositories and Unit of Work
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Sender lives in Infrastructure because Sender.cs is an Infrastructure class
        services.AddScoped<ISender, Sender>();

        return services;
    }
}

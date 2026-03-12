using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Features.Auth.Commands;
using PayFlow.Application.Common.Features.Auth.Validators;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Persistence.Repositories;
using PayFlow.Infrastructure.Services;
using System.Text.Json.Serialization;

namespace PayFlow.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // This is where you would add API-level services like controllers, Swagger, etc.
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddOpenApi();
            services.AddControllers().AddJsonOptions(options =>
             {
                 // Serialize enum as string in API responses
                 options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
             });

            return services;
        }

        // This is where you would add infrastructure-level services like DbContext, repositories, etc.
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
            services.AddSingleton<IPasswordHasher, PasswordHasher>();

            return services;
        }

        // This is where you would add application-level services like MediatR handlers, AutoMapper profiles, etc.
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Handlers
            services.AddScoped<RegisterCommandHandler>();

            // Validators
            services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();

            return services;
        }
    }
}
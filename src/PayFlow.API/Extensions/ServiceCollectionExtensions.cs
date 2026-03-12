using Microsoft.EntityFrameworkCore;
using PayFlow.Infrastructure.Persistence;

namespace PayFlow.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // This is where you would add API-level services like controllers, Swagger, etc.
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddOpenApi();

            return services;
        }

        // This is where you would add infrastructure-level services like DbContext, repositories, etc.
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PayFlowDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        // This is where you would add application-level services like MediatR handlers, AutoMapper profiles, etc.
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            return services;
        }
    }
}
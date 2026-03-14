using PayFlow.API.ExceptionHandlers;
using System.Text.Json.Serialization;

namespace PayFlow.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // This is where you would add API-level services like controllers, Swagger, etc.
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddOpenApi();
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<BusinessRuleExceptionHandler>();
            services.AddProblemDetails();
            services.AddControllers().AddJsonOptions(options =>
             {
                 // Serialize enum as string in API responses
                 options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
             });

            return services;
        }
    }
}

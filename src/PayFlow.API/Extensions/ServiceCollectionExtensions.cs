using PayFlow.API.ExceptionHandlers;
using System.Text.Json.Serialization;

namespace PayFlow.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // Extension method to register application services
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            services.AddOpenApi();
            services.AddProblemDetails();
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<BusinessRuleExceptionHandler>();
            services.AddControllers().AddJsonOptions(options =>
             {
                 // Serialize enum as string in API responses
                 options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
             });

            return services;
        }
    }
}
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PayFlow.API.ExceptionHandlers;
using PayFlow.Infrastructure.Settings;
using System.Text;
using System.Text.Json.Serialization;

namespace PayFlow.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        // Registers API layer services
        public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1: Add API documentation, problem details support and HTTP context accessor
            services.AddOpenApi();
            services.AddProblemDetails();
            services.AddHttpContextAccessor();

            // 2: Register controllers and configure JSON serialization
            services.AddControllers().AddJsonOptions(options =>
            {
                // Serialize enums as strings in API responses
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // 3: Configure URL path-based API versioning (/api/v{version}/...)
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });

            // 4: Register global exception handlers
            services.AddExceptionHandler<DomainExceptionHandler>();
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<BusinessRuleExceptionHandler>();

            // 5: Bind JWT settings from configuration
            var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

            // 6: Add authentication and configure JWT bearer validation
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = jwtSettings.Issuer,
                            ValidAudience = jwtSettings.Audience,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                            ClockSkew = TimeSpan.Zero
                        };
                    });

            // 7: Add authorization services
            services.AddAuthorization();

            return services;
        }
    }
}
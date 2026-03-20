using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using PayFlow.API.Constants;
using PayFlow.API.ExceptionHandlers;
using PayFlow.API.RateLimiting;
using PayFlow.API.Settings;
using PayFlow.Infrastructure.Settings;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

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

            // 8: Read and validate transfer rate-limiting settings
            var transferRateLimitingOptions = configuration
                .GetSection(TransferRateLimitingOptions.SectionName)
                .Get<TransferRateLimitingOptions>();

            // 9: Add per-user fixed-window rate limiting for transfer endpoint
            services.AddRateLimiter(options =>
            {
                options.OnRejected = TransferRateLimitRejectionHandler.HandleAsync;

                options.AddPolicy(RateLimitPolicies.TransferPolicy, httpContext =>
                {
                    var userId = httpContext.User.FindFirst("uid")?.Value;
                    var partitionKey = string.IsNullOrWhiteSpace(userId) ? "missing-uid" : userId;

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: partitionKey,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = transferRateLimitingOptions.PermitLimit,
                            Window = TimeSpan.FromSeconds(transferRateLimitingOptions.WindowSeconds),
                            QueueLimit = transferRateLimitingOptions.QueueLimit
                        });
                });
            });

            // 10: Register health checks for core infrastructure dependencies
            services.AddHealthChecks()
                .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection")!,
                name: "sqlserver",
                failureStatus:HealthStatus.Unhealthy,
                tags: ["db"])
                .AddRedis(
                redisConnectionString: configuration["Redis:ConnectionString"]!,
                name: "redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["cache"]);

            return services;
        }
    }
}
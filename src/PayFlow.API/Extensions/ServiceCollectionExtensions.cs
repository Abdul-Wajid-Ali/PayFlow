using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PayFlow.API.Constants;
using PayFlow.API.ExceptionHandlers;
using PayFlow.API.RateLimiting;
using PayFlow.Infrastructure.Options;
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
            }).AddMvc();

            // 4: Register global exception handlers
            services.AddExceptionHandler<DomainExceptionHandler>();
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<BusinessRuleExceptionHandler>();
            services.AddExceptionHandler<IdempotencyConflictExceptionHandler>();

            //5: Bind JwtOptions from appsettings.json and validate at startup
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // 6: Add authentication services with JWT bearer scheme with default settings (configured later via DI in step 7)
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer();

            // 7: Configure JWT authentication by binding JwtOptions and applying them to JwtBearerOptions via DI
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
                {
                    // Get JWT settings from configuration
                    var options = jwtOptions.Value;

                    // Configure token validation parameters based on JWT settings
                    bearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = options.Issuer,
                        ValidAudience = options.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            // 8: Add authorization services
            services.AddAuthorization();

            // 9: Register and validate RateLimiting options from configuration
            services.AddOptions<TransferRateLimitingOptions>()
                .Bind(configuration.GetSection(TransferRateLimitingOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // 10: Add rate limiting services with default settings (configured later via DI in step 11)
            services.AddRateLimiter();

            // 11: Configure rate limiting by defining policies and applying TransferRateLimitingOptions via DI
            services.AddOptions<RateLimiterOptions>()
                .Configure<IOptions<TransferRateLimitingOptions>>((rateLimiterOptions, rateLimitingOptions) =>
                {
                    // Get rate limiting settings from configuration
                    var options = rateLimitingOptions.Value;

                    // Set a global rejection handler for when requests exceed rate limits
                    rateLimiterOptions.OnRejected = TransferRateLimitRejectionHandler.HandleAsync;

                    // Define a rate limiting policy for transfer operations, partitioned by user ID
                    rateLimiterOptions.AddPolicy(RateLimitPolicies.TransferPolicy, httpContext =>
                    {
                        var userId = httpContext.User.FindFirst("userId")?.Value;

                        var partitionKey = string.IsNullOrWhiteSpace(userId) ? "missing-uid" : userId;

                        // Use a fixed window rate limiter with settings from configuration, partitioned by user ID
                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            factory: _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = options.PermitLimit,
                                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                                QueueLimit = options.QueueLimit
                            });
                    });
                });

            // 12: Register health checks for core infrastructure dependencies
            services.AddDependencyHealthChecks(configuration);

            return services;
        }
    }
}
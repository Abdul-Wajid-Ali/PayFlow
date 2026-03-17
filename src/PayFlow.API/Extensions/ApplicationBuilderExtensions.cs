using PayFlow.API.Middlewares;
using Serilog;

namespace PayFlow.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        // Configures the HTTP request processing pipeline for the API.
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            var env = app.Environment;

            // Enable OpenAPI endpoints only in development environment
            if (env.IsDevelopment())
            {
                app.MapOpenApi();
            }

            // Global exception handler to catch unhandled errors across the pipeline
            app.UseExceptionHandler();

            // Attach a correlation ID to every request for end-to-end tracing
            app.UseMiddleware<CorrelationIdMiddleware>();

            // Serilog structured HTTP request logging with enriched properties
            app.UseSerilogRequestLogging(options =>
            {
                options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                {
                    diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                    diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                    diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
                };
            });

            // Redirect HTTP requests to HTTPS for secure communication
            app.UseHttpsRedirection();

            // Authenticate the user and populate HttpContext.User
            app.UseAuthentication();

            // Enforce authorization policies on authenticated users
            app.UseAuthorization();

            // Map controller endpoints to the routing system
            app.MapControllers();

            return app;
        }
    }
}
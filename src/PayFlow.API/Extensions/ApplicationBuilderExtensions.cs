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
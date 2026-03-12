namespace PayFlow.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        // This is where you would configure the middleware pipeline for your API.
        public static WebApplication UseApiPipeline(this WebApplication app)
        {
            var env = app.Environment;

            if (env.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();

            app.MapControllers();

            return app;
        }
    }
}
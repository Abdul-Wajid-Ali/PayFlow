using PayFlow.API.Extensions;

// Create the application builder and load configuration, logging, and DI container
var builder = WebApplication.CreateBuilder(args);

// Register application services into the dependency injection container
builder.Services
    .AddApiServices()
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration);

// Build the application and finalize the service container
var app = builder.Build();

// Configure the HTTP request processing pipeline (middleware + endpoints)
app.UseApiPipeline();

// Start the web server and begin handling incoming HTTP requests
await app.RunAsync();
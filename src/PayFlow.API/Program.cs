using PayFlow.API.Extensions;
using PayFlow.Application;
using PayFlow.Infrastructure;

// Create the application builder and load configuration, logging, and DI container
var builder = WebApplication.CreateBuilder(args);

//Register services in the DI container for API, Application, and Infrastructure layers
builder.Services
    .AddApiServices(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration);

// Build the application and finalize the service container
var app = builder.Build();

// Configure the HTTP request processing pipeline (middleware + endpoints)
app.UseApiPipeline();

// Start the web server and begin handling incoming HTTP requests
await app.RunAsync();
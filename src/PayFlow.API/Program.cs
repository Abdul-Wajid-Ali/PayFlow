using PayFlow.API.Extensions;
using PayFlow.Application;
using PayFlow.Infrastructure;
using Serilog;

// 1: Create the application builder and load configuration, logging, and DI container
var builder = WebApplication.CreateBuilder(args);

// 2: Replace default logging with Serilog, reading configuration from appsettings
builder.Host.UseSerilog((context, loggerConfiguration)
    => loggerConfiguration.ReadFrom.Configuration(context.Configuration));

// 3: Register services in the DI container for API, Application, and Infrastructure layers
builder.Services
    .AddApiServices(builder.Configuration)
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration);

// 4: Build the application and finalize the service container
var app = builder.Build();

// 5: Configure the HTTP request processing pipeline (middleware + endpoints)
app.UseApiPipeline();

// 6: Start the web server and begin handling incoming HTTP requests
await app.RunAsync();
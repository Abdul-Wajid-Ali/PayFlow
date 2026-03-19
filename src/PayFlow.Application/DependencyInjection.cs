using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Common.Behaviors;
using PayFlow.Application.Features.Auth.Commands;
using PayFlow.Application.Features.Auth.Validators;
using PayFlow.Application.Features.Transfers.Commands;
using PayFlow.Application.Features.Transfers.Queries;
using PayFlow.Application.Features.Transfers.Validators;

namespace PayFlow.Application;

public static class DependencyInjection
{
    // Registers application layer services and use-case handlers
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 1: Register MediatR handlers from the application assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // 2: Register FluentValidation validators for commands
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();
        services.AddScoped<IValidator<TransferCommand>, TransferCommandValidator>();
        services.AddScoped<IValidator<RefreshTokenCommand>, RefreshTokenCommandValidator>();
        services.AddScoped<IValidator<RevokeTokenCommand>, RevokeTokenCommandValidator>();
        services.AddScoped<IValidator<GetTransactionsQuery>, GetTransactionsQueryValidator>();

        // 3: Register pipeline behaviors for cross-cutting concerns (logging, validation)
        services.AddScoped(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Common.Behaviors;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Features.Auth.Commands;
using PayFlow.Application.Features.Auth.DTOs;
using PayFlow.Application.Features.Auth.Validators;

namespace PayFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Command Handlers
        services.AddScoped<ICommandHandler<RegisterCommand, RegisterResponse>, RegisterCommandHandler>();

        // Validators
        services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();

        // Pipeline Behaviors
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}

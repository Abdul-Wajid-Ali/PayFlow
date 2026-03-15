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
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();

        // Validators
        services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();

        // Pipeline Behaviors
        services.AddScoped<IPipelineBehavior<RegisterCommand, RegisterResponse>, ValidationBehavior<RegisterCommand, RegisterResponse>>();
        services.AddScoped<IPipelineBehavior<LoginCommand, LoginResponse>, ValidationBehavior<LoginCommand, LoginResponse>>();

        return services;
    }
}
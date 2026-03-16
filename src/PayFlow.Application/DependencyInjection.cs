using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PayFlow.Application.Common.Behaviors;
using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Features.Auth.Commands;
using PayFlow.Application.Features.Auth.DTOs;
using PayFlow.Application.Features.Auth.Validators;
using PayFlow.Application.Features.Transfers.Commands;
using PayFlow.Application.Features.Transfers.DTOs;
using PayFlow.Application.Features.Transfers.Validators;
using PayFlow.Application.Features.Wallet.DTOs;
using PayFlow.Application.Features.Wallet.Queries;

namespace PayFlow.Application;

public static class DependencyInjection
{
    // Registers application layer services and use-case handlers
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 1: Register command handlers for application use cases
        services.AddScoped<ICommandHandler<RegisterCommand, RegisterResponse>, RegisterCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<TransferCommand, TransferResponse>, TransferCommandHandler>();

        // 2: Register query handlers for application use cases
        services.AddScoped<IQueryHandler<GetBalanceQuery, WalletBalanceResponse>, GetBalanceQueryHandler>();

        // 3: Register FluentValidation validators for commands
        services.AddScoped<IValidator<RegisterCommand>, RegisterCommandValidator>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();
        services.AddScoped<IValidator<TransferCommand>, TransferCommandValidator>();

        // 4: Register pipeline behaviors for cross-cutting concerns (validation)
        services.AddScoped<IPipelineBehavior<LoginCommand, LoginResponse>, ValidationBehavior<LoginCommand, LoginResponse>>();
        services.AddScoped<IPipelineBehavior<RegisterCommand, RegisterResponse>, ValidationBehavior<RegisterCommand, RegisterResponse>>();
        services.AddScoped<IPipelineBehavior<TransferCommand, TransferResponse>, ValidationBehavior<TransferCommand, TransferResponse>>();

        return services;
    }
}
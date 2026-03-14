using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Features.Auth.DTOs;

namespace PayFlow.Application.Features.Auth.Commands
{
    public record RegisterCommand(string Email, string Password)
        : ICommand<RegisterResponse>;
}
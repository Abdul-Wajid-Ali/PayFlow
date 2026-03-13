using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Common.Features.Auth.DTOs;

namespace PayFlow.Application.Common.Features.Auth.Commands
{
    public record RegisterCommand(string Email, string Password)
        : ICommand<RegisterResponse>;
}
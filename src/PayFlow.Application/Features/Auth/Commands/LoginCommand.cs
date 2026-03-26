using PayFlow.Application.Common.CQRS;
using PayFlow.Application.Features.Auth.DTOs;

namespace PayFlow.Application.Features.Auth.Commands
{
    public record LoginCommand(string Email, string Password)
        : ICommand<LoginResponse>;
}
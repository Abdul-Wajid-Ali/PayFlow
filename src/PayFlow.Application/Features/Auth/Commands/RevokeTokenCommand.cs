using PayFlow.Application.Common.CQRS;
using System.Windows.Input;

namespace PayFlow.Application.Features.Auth.Commands
{
    public record RevokeTokenCommand(string RefreshToken) : ICommand<bool>;
}
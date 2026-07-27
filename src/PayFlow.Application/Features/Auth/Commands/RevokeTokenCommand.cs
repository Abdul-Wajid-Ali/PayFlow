namespace PayFlow.Application.Features.Auth.Commands
{
    public record RevokeTokenCommand(string RefreshToken) : ICommand<bool>;
}

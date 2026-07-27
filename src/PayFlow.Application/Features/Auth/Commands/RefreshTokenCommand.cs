namespace PayFlow.Application.Features.Auth.Commands
{
    public record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;
}

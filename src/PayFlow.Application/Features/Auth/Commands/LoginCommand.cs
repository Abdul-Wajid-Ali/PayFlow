namespace PayFlow.Application.Features.Auth.Commands
{
    public record LoginCommand(string Email, string Password)
        : ICommand<LoginResponse>;
}

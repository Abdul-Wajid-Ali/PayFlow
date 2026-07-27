namespace PayFlow.Application.Features.Auth.Commands
{
    public record RegisterCommand(string Email, string Password)
        : ICommand<RegisterResponse>;
}

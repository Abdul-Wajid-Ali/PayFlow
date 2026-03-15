namespace PayFlow.Application.Features.Auth.DTOs
{
    // DTO for receiving Register Request
    public record RegisterRequest(string Email, string Password);

    // DTO for returning Register Response
    public record RegisterResponse(Guid UserId, string Email, Guid WalletId);
}
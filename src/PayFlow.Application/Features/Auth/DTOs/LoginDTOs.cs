namespace PayFlow.Application.Features.Auth.DTOs
{
    // DTO for receiving Login Request
    public record LoginRequest(string Email, string Password);

    // DTO for returning Login Response
    public record LoginResponse(Guid UserId, string Email, string Token, DateTime ExpiresAt);
}
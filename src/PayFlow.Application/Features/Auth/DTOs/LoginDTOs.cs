namespace PayFlow.Application.Features.Auth.DTOs
{
    // DTO for receiving Login Request
    public record LoginRequest(string Email, string Password);

    // DTO for returning Login Response
    public record LoginResponse(Guid UserId, string Email, string AccessToken, string RefreshToken, DateTime ExpiresAt);

    // DTO for receiving Refresh Token Request
    public record RefreshTokenRequest(string RefreshToken);

    // DTO for returning Refresh Token Response
    public record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

    // DTO for receiving Revoke Token Request
    public record RevokeTokenRequest(string RefreshToken);
}
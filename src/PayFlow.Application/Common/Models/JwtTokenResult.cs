namespace PayFlow.Application.Common.Models
{
    // Wrapper for JWT token generation result, including the token value and its expiration time.
    public record JwtTokenResult(string Value, DateTime ExpiresAt);
}

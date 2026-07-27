namespace PayFlow.Application.Common.Interfaces
{
    public interface IJwtService
    {
        JwtTokenResult GenerateToken(User user);

        string GenerateRefreshToken();

        string HashRefreshToken(string refreshToken);

        int GetRefreshTokenExpiryInDays();
    }
}

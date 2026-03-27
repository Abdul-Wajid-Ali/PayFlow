namespace PayFlow.Infrastructure.Configuration
{
    // Represents configuration settings for JWT token generation, validation, and lifetime management.
    public sealed class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        public string SecretKey { get; init; } = string.Empty;

        public string Issuer { get; init; } = string.Empty;

        public string Audience { get; init; } = string.Empty;

        public int ExpiryInMinutes { get; init; } = 60;

        public int RefreshTokenExpiryInDays { get; init; } = 14;
    }
}
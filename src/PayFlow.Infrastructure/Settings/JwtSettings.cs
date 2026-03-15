namespace PayFlow.Infrastructure.Settings
{
    // This class represents the settings required for JWT token generation and validation.
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        public string SecretKey { get; init; } = string.Empty;

        public string Issuer { get; init; } = string.Empty;

        public string Audience { get; init; } = string.Empty;

        public int ExpiryInMinutes { get; init; } = 60;
    }
}
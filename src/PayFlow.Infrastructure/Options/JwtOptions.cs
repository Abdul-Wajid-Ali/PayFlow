using System.ComponentModel.DataAnnotations;

namespace PayFlow.Infrastructure.Options
{
    // Represents configuration settings for JWT token generation, validation, and lifetime management.
    public sealed class JwtOptions
    {
        public const string SectionName = "JwtSettings";

        [Required]
        [MinLength(32, ErrorMessage = "SecretKey must be at least 32 characters long.")]
        public string SecretKey { get; init; } = string.Empty;

        [Required]
        public string Issuer { get; init; } = string.Empty;

        [Required]
        public string Audience { get; init; } = string.Empty;

        [Range(1, 1440, ErrorMessage = "ExpiryInMinutes must be between 1 and 1440.")]
        public int ExpiryInMinutes { get; init; } = 60;

        [Range(1, 365, ErrorMessage = "RefreshTokenExpiryInDays must be between 1 and 365.")]
        public int RefreshTokenExpiryInDays { get; init; } = 14;
    }
}
namespace PayFlow.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public string TokenHash { get; private set; } = string.Empty;

        public DateTime CreatedAt { get; private set; }

        public DateTime ExpiresAt { get; private set; }

        public bool IsRevoked { get; private set; }

        public DateTime? RevokedAt { get; private set; }

        public string? RevokedReason { get; private set; }

        public string? ReplacedByTokenHash { get; private set; }

        // Navigation property to the owning user
        public User? User { get; private set; }

        // Private constructor to enforce use of factory method
        private RefreshToken()
        { }

        // Factory method to create a new refresh token for a user
        public static RefreshToken Create(
            Guid userId,
            string tokenHash,
            DateTime createdAt,
            DateTime expiresAt)
            => new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                CreatedAt = createdAt,
                ExpiresAt = expiresAt,
                IsRevoked = false
            };

        // Domain method to check if the refresh token is expired
        public bool IsExpired(DateTime dateTime)
            => dateTime >= ExpiresAt;

        // Domain method to rotate the refresh token
        public void Rotate(string replacementTokenHash, DateTime revokedAt)
        {
            IsRevoked = true;
            RevokedAt = revokedAt;
            RevokedReason = "Rotated";
            ReplacedByTokenHash = replacementTokenHash;
        }

        // Domain method to revoke the refresh token without replacement
        public void Revoke(string reason, DateTime revokedAt)
        {
            IsRevoked = true;
            RevokedAt = revokedAt;
            RevokedReason = reason;
        }
    }
}
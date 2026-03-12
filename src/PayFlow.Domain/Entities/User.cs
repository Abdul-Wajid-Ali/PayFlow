using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }

        public string Email { get; private set; } = string.Empty;

        public string PasswordHash { get; private set; } = string.Empty;

        public UserStatus Status { get; private set; }

        public DateTime CreatedAt { get; private set; }

        // Navigation property
        public Wallet? Wallet { get; private set; }

        // Private constructor to enforce use of factory method
        private User()
        { }

        public static User Create(string email, string passwordHash)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Email = email.ToLowerInvariant().Trim(),
                PasswordHash = passwordHash,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
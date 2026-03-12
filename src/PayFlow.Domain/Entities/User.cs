using PayFlow.Domain.Enums;

namespace PayFlow.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public string PasswordSalt { get; set; }

        public UserStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
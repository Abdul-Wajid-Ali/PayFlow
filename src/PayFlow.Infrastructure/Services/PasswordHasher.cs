using PayFlow.Application.Common.Interfaces;
using System.Security.Cryptography;

namespace PayFlow.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        // These constants define the parameters for the hashing algorithm.
        private const int SaltSize = 32;

        private const int HashSize = 32;
        private const int Iterations = 100_000;

        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        // Hashes the password using PBKDF2 with a random salt.
        public (string Hash, string Salt) Hash(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);

            var hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                Algorithm,
                HashSize);

            return (
                Hash: Convert.ToBase64String(hash),
                Salt: Convert.ToBase64String(salt)
            );
        }

        // Verifies the password by hashing it with the provided salt and comparing it to the stored hash.
        public bool Verify(string password, string hash, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);

            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                Algorithm,
                HashSize);

            // Constant time comparison — prevents timing attacks
            return CryptographicOperations.FixedTimeEquals(
                hashToCompare,
                Convert.FromBase64String(hash));
        }
    }
}
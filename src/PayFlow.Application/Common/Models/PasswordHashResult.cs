namespace PayFlow.Application.Common.Models
{
    //Wrapper for the result of hashing a password, containing both the hash and the salt.
    public record PasswordHashResult(string Hash, string Salt);
}
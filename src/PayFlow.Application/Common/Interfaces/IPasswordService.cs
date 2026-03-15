using PayFlow.Application.Common.Models;

namespace PayFlow.Application.Common.Interfaces
{
    public interface IPasswordService
    {
        PasswordHashResult Hash(string password);

        bool Verify(string password, string hash, string salt);
    }
}
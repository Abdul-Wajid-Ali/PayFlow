using PayFlow.Domain.Entities;

namespace PayFlow.Application.Common.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
    }
}
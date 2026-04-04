using PayFlow.Domain.Entities;

namespace PayFlow.Domain.Interfaces
{
    public interface IWalletRepository
    {
        Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken = default);

        Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

        Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);
    }
}
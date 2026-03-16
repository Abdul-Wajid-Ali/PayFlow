using PayFlow.Domain.Entities;

namespace PayFlow.Application.Common.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Transaction>> GetByWalletIdAsync(Guid walletId, CancellationToken cancellationToken = default);

        Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    }
}
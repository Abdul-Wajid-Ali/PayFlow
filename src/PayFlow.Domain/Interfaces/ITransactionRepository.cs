using PayFlow.Domain.Entities;

namespace PayFlow.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

        Task<Transaction?> GetByIdempotencyKeyAndWalletIdAsync(string idempotencyKey, Guid walletId, CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
            Guid walletId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default
        );

        Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    }
}
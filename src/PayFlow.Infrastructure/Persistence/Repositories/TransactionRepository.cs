using Microsoft.EntityFrameworkCore;
using PayFlow.Application.Common.Interfaces;
using PayFlow.Domain.Entities;

namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly PayFlowDbContext _dbContext;

        public TransactionRepository(PayFlowDbContext dbContext) => _dbContext = dbContext;

        public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
            => await _dbContext.Transactions.AddAsync(transaction, cancellationToken);

        public async Task<Transaction?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
            => await _dbContext.Transactions.FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey, cancellationToken);

        public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => await _dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
            Guid walletId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default
        )
        {
            var query = _dbContext.Transactions
                .Where(t => t.FromWalletId == walletId || t.ToWalletId == walletId)
                .OrderByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
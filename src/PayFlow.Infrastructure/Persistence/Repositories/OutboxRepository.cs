using Microsoft.EntityFrameworkCore;
using PayFlow.Domain.Entities;
using PayFlow.Domain.Interfaces;

namespace PayFlow.Infrastructure.Persistence.Repositories
{
    public class OutboxRepository : IOutboxRepository
    {
        private readonly PayFlowDbContext _dbContext;

        public OutboxRepository(PayFlowDbContext dbContext)
            => _dbContext = dbContext;

        public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
         => await _dbContext.OutboxMessages.AddAsync(message, cancellationToken);

        public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, DateTime dateTimeNow, CancellationToken cancellationToken = default)
         => await _dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.DeadLetteredAt == null && (m.NextRetryAt == null || m.NextRetryAt >= dateTimeNow))
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}